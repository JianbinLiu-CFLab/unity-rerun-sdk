// SPDX-License-Identifier: Apache-2.0
//
// Managed gRPC client for Rerun Viewer live transport.
// Uses Grpc.Net.Client + System.Threading.Channels for bounded queue.

#nullable enable

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Unity.RerunSDK.Encoding;
using RerunSdkComms = Rerun.SdkComms.V1Alpha1;

namespace Unity.RerunSDK.Transport.Grpc
{
    internal class RerunGrpcClient : IDisposable
    {
#if UNITY_5_3_OR_NEWER
        private static void LogInfo(string msg) => UnityEngine.Debug.Log(msg);
        private static void LogWarning(string msg) => UnityEngine.Debug.LogWarning(msg);
#else
        private static void LogInfo(string msg) => System.Diagnostics.Debug.WriteLine(msg);
        private static void LogWarning(string msg) => System.Diagnostics.Debug.WriteLine(msg);
#endif

        private readonly RerunGrpcEndpoint _endpoint;
        private readonly int _connectTimeoutMs;
        private readonly int _reconnectDelayMs;

        private readonly Channel<EncodedRerunMessage> _sendQueue;
        private readonly CancellationTokenSource _shutdownCts = new();
        private readonly SemaphoreSlim _messageSignal = new(0);
        private readonly object _queueGate = new();

        private GrpcChannel? _channel;
        private RerunSdkComms.MessageProxyService.MessageProxyServiceClient? _client;
        private AsyncClientStreamingCall<RerunSdkComms.WriteMessagesRequest, RerunSdkComms.WriteMessagesResponse>? _call;

        private EncodedRerunMessage _lastStoreInfo;
        private EncodedRerunMessage _pendingStoreInfo;
        private bool _hasStoreInfo;
        private bool _hasPendingStoreInfo;

        private Task? _backgroundTask;
        private int _droppedCount;
        private int _sentStoreInfoCount;
        private int _sentDataCount;
        private volatile bool _stopRequested;
        private volatile bool _disposed;

        public int DroppedCount => Volatile.Read(ref _droppedCount);

        public RerunGrpcClient(RerunGrpcEndpoint endpoint,
            int connectTimeoutMs = 3000, int reconnectDelayMs = 1000, int maxQueueMessages = 2048)
        {
            _endpoint = endpoint;
            _connectTimeoutMs = connectTimeoutMs;
            _reconnectDelayMs = reconnectDelayMs;

            _sendQueue = Channel.CreateBounded<EncodedRerunMessage>(
                new BoundedChannelOptions(Math.Max(1, maxQueueMessages))
                {
                    FullMode = BoundedChannelFullMode.Wait,
                    SingleWriter = false,
                    SingleReader = true
                });
        }

        /// Fire-and-forget write. Drops on full queue; never blocks the caller.
        public void Write(EncodedRerunMessage message)
        {
            if (_disposed) return;

            lock (_queueGate)
            {
                if (_disposed) return;

                // Cache StoreInfo for reconnect replay.
                if (message.IsStoreInfo)
                {
                    _lastStoreInfo = message;
                    _hasStoreInfo = true;
                    var shouldSignal = !_hasPendingStoreInfo;
                    _pendingStoreInfo = message;
                    _hasPendingStoreInfo = true;
                    if (shouldSignal)
                        _messageSignal.Release();
                    return;
                }

                if (!_sendQueue.Writer.TryWrite(message))
                {
                    Interlocked.Increment(ref _droppedCount);
                    return;
                }

                _messageSignal.Release();
            }
        }

        private bool TryTakeNextMessage(out EncodedRerunMessage message)
        {
            lock (_queueGate)
            {
                if (_hasPendingStoreInfo)
                {
                    message = _pendingStoreInfo;
                    _hasPendingStoreInfo = false;
                    return true;
                }
            }

            return _sendQueue.Reader.TryRead(out message);
        }

        /// Start connecting in the background.
        public void Start()
        {
            if (_backgroundTask != null) return;
            LogInfo($"[RerunGrpcClient] Starting live stream loop to {_endpoint.GrpcAddress}");
            _backgroundTask = Task.Run(() => RunLoop(_shutdownCts.Token));
        }

        private async Task RunLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await ConnectAndStream(ct);
                    if (_stopRequested)
                        break;
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    if (_stopRequested)
                    {
                        if (IsExpectedShutdownException(ex))
                        {
                            LogInfo($"[RerunGrpcClient] Live stream closed during shutdown ({ex.GetType().Name})");
                            break;
                        }

                        var stopExtra = ex is RpcException stopRpc
                            ? $", StatusCode={stopRpc.StatusCode}, Detail={stopRpc.Status.Detail}"
                            : "";
                        LogWarning($"[RerunGrpcClient] Stream ended during shutdown: {ex.GetType().Name}: {ex.Message}{stopExtra}");
                        break;
                    }

                    var extra = ex is RpcException rpc
                        ? $", StatusCode={rpc.StatusCode}, Detail={rpc.Status.Detail}"
                        : "";
                    LogWarning($"[RerunGrpcClient] Stream ended: {ex.GetType().Name}: {ex.Message}{extra}, reconnecting in {_reconnectDelayMs}ms");
                }

                try { await Task.Delay(_reconnectDelayMs, ct); }
                catch (OperationCanceledException) { break; }
            }
        }

        private async Task ConnectAndStream(CancellationToken ct)
        {
            GrpcChannel? channel = null;
            AsyncClientStreamingCall<RerunSdkComms.WriteMessagesRequest, RerunSdkComms.WriteMessagesResponse>? call = null;

            try
            {
                channel = GrpcChannel.ForAddress(_endpoint.GrpcAddress, CreateChannelOptions());
                _channel = channel;

                _client = new RerunSdkComms.MessageProxyService.MessageProxyServiceClient(channel);

                using var connectCts = new CancellationTokenSource(_connectTimeoutMs);

                call = _client.WriteMessages(cancellationToken: ct);
                _call = call;
                LogInfo($"[RerunGrpcClient] WriteMessages stream opened to {_endpoint.GrpcAddress}");

                EncodedRerunMessage storeInfo = default;
                bool hasStoreInfo;
                lock (_queueGate)
                {
                    hasStoreInfo = _hasStoreInfo;
                    storeInfo = _lastStoreInfo;
                    if (_hasPendingStoreInfo)
                        _hasPendingStoreInfo = false;
                }

                // Replay cached StoreInfo on new stream (with connect timeout guard).
                if (hasStoreInfo)
                {
                    if (!await WaitForTask(
                            call.RequestStream.WriteAsync(ToWriteRequest(storeInfo)),
                            connectCts.Token).ConfigureAwait(false))
                    {
                        LogWarning("[RerunGrpcClient] StoreInfo replay timed out");
                        return;
                    }
                    LogSent(storeInfo);
                }

                // Drain prioritized StoreInfo control messages and bounded data messages.
                while (!ct.IsCancellationRequested)
                {
                    await _messageSignal.WaitAsync(ct).ConfigureAwait(false);

                    while (TryTakeNextMessage(out var msg))
                    {
                        await call.RequestStream.WriteAsync(ToWriteRequest(msg));
                        LogSent(msg);
                    }

                    if (_stopRequested)
                        break;
                }

                LogInfo("[RerunGrpcClient] Completing WriteMessages request stream");
                await call.RequestStream.CompleteAsync();
                LogInfo("[RerunGrpcClient] WriteMessages request stream completed");

                using var responseCts = new CancellationTokenSource(1000);
                if (await WaitForTask(call.ResponseAsync, responseCts.Token).ConfigureAwait(false))
                    LogInfo("[RerunGrpcClient] WriteMessages stream completed");
                else
                    LogWarning("[RerunGrpcClient] WriteMessages response timed out after request stream completion");
            }
            finally
            {
                try { call?.Dispose(); }
                catch { /* best-effort cleanup */ }

                if (ReferenceEquals(_call, call))
                    _call = null;
                if (ReferenceEquals(_channel, channel))
                    _channel = null;

                channel?.Dispose();
            }
        }

        private static RerunSdkComms.WriteMessagesRequest ToWriteRequest(EncodedRerunMessage msg)
        {
            return new RerunSdkComms.WriteMessagesRequest
            {
                LogMsg = Rerun.LogMsg.V1Alpha1.LogMsg.Parser.ParseFrom(msg.GrpcLogMsgBytes)
            };
        }

        private static bool IsExpectedShutdownException(Exception ex)
        {
            if (ex is OperationCanceledException)
                return true;

            if (ex is not RpcException rpc)
                return false;

            if (rpc.StatusCode == StatusCode.Cancelled)
                return true;

            return false;
        }

        private void LogSent(EncodedRerunMessage msg)
        {
            if (msg.IsStoreInfo)
            {
                var count = Interlocked.Increment(ref _sentStoreInfoCount);
                if (count <= 2)
                    LogInfo("[RerunGrpcClient] StoreInfo sent to live stream");
                return;
            }

            var dataCount = Interlocked.Increment(ref _sentDataCount);
            if (dataCount <= 5)
                LogInfo($"[RerunGrpcClient] Data message sent to live stream (kind={msg.RrdKind}, total={dataCount})");
        }

        private static GrpcChannelOptions CreateChannelOptions()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2Support", true);
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            return new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.Insecure,
                HttpHandler = CreateHttpHandler()
            };
        }

        private static HttpMessageHandler CreateHttpHandler()
        {
#if UNITY_5_3_OR_NEWER
            var cysharpHandler = TryCreateCysharpHttp2Handler();
            if (cysharpHandler != null)
                return cysharpHandler;

            LogWarning("[RerunGrpcClient] Cysharp YetAnotherHttpHandler was not found; falling back to HttpClientHandler. Unity live gRPC may downgrade to HTTP/1.1.");
            return new HttpClientHandler();
#else
            return new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            };
#endif
        }

        private static HttpMessageHandler? TryCreateCysharpHttp2Handler()
        {
            var handlerType = FindLoadedType("Cysharp.Net.Http.YetAnotherHttpHandler");
            if (handlerType == null || !typeof(HttpMessageHandler).IsAssignableFrom(handlerType))
                return null;

            try
            {
                var handler = (HttpMessageHandler?)Activator.CreateInstance(handlerType);
                if (handler == null)
                    return null;

                var http2Only = handlerType.GetProperty("Http2Only");
                if (http2Only != null && http2Only.CanWrite &&
                    (http2Only.PropertyType == typeof(bool) || http2Only.PropertyType == typeof(bool?)))
                {
                    http2Only.SetValue(handler, true);
                }
                else
                {
                    LogWarning("[RerunGrpcClient] Cysharp YetAnotherHttpHandler loaded, but Http2Only property was not found.");
                }

                LogInfo("[RerunGrpcClient] Using Cysharp YetAnotherHttpHandler for HTTP/2 live gRPC");
                return handler;
            }
            catch (Exception ex)
            {
                LogWarning($"[RerunGrpcClient] Failed to create Cysharp YetAnotherHttpHandler: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private static Type? FindLoadedType(string fullName)
        {
            var type = Type.GetType(fullName);
            if (type != null)
                return type;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, throwOnError: false);
                if (type != null)
                    return type;
            }

            return null;
        }

        private static async Task<bool> WaitForTask(Task task, CancellationToken timeoutToken)
        {
            if (task.IsCompleted)
            {
                await task.ConfigureAwait(false);
                return true;
            }

            var timeout = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = timeoutToken.Register(
                state => ((TaskCompletionSource<bool>)state!).TrySetResult(false), timeout);

            var completed = await Task.WhenAny(task, timeout.Task).ConfigureAwait(false);
            if (completed != task)
                return false;

            await task.ConfigureAwait(false);
            return true;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _stopRequested = true;

            LogInfo("[RerunGrpcClient] Stopping live stream loop");
            _sendQueue.Writer.TryComplete();
            _messageSignal.Release();

            try
            {
                if (_backgroundTask != null && !_backgroundTask.Wait(TimeSpan.FromSeconds(3)))
                {
                    LogWarning("[RerunGrpcClient] Live stream did not stop within 3s; cancelling");
                    _shutdownCts.Cancel();
                    _backgroundTask.Wait(TimeSpan.FromSeconds(1));
                }
            }
            catch { /* swallow shutdown race */ }

            _channel?.Dispose();
            _shutdownCts.Dispose();
            _messageSignal.Dispose();
        }
    }
}

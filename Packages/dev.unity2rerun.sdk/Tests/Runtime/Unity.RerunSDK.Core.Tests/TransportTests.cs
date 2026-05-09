// Transport layer unit tests — endpoint parsing, message identity, composite isolation
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Grpc.Core;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.Transport;
using Unity.RerunSDK.Transport.Grpc;
using Xunit;

public class TransportTests
{
    // ── Endpoint parsing ──

    [Fact]
    public void Default_endpoint_parses_correctly()
    {
        var ep = RerunGrpcEndpoint.Parse("rerun+http://127.0.0.1:9876/proxy");
        Assert.Equal("127.0.0.1", ep.Host);
        Assert.Equal(9876, ep.Port);
        Assert.Equal("http://127.0.0.1:9876/proxy", ep.GrpcAddress);
    }

    [Fact]
    public void Https_endpoint_parses_correctly()
    {
        var ep = RerunGrpcEndpoint.Parse("rerun+https://my.server.com:443/proxy");
        Assert.Equal("my.server.com", ep.Host);
        Assert.Equal(443, ep.Port);
        Assert.Equal("https://my.server.com:443/proxy", ep.GrpcAddress);
    }

    [Fact]
    public void Missing_scheme_throws()
    {
        Assert.Throws<ArgumentException>(() => RerunGrpcEndpoint.Parse("http://host:9876/proxy"));
    }

    [Fact]
    public void Missing_port_throws()
    {
        Assert.Throws<ArgumentException>(() => RerunGrpcEndpoint.Parse("rerun+http://host/proxy"));
    }

    // ── EncodedRerunMessage identity ──

    [Fact]
    public void StoreInfo_message_has_Istrue_fields()
    {
        var msg = new EncodedRerunMessage(1, new byte[] { 1 }, new byte[] { 1, 2 },
            isStoreInfo: true, isStatic: false);
        Assert.True(msg.IsStoreInfo);
        Assert.False(msg.IsStatic);
    }

    [Fact]
    public void Data_write_increments_drop_count_when_queue_is_full()
    {
        using var client = new RerunGrpcClient(RerunGrpcEndpoint.Default, maxQueueMessages: 1);

        client.Write(DataMessage());
        client.Write(DataMessage());

        Assert.Equal(1, client.DroppedCount);
        var reader = GetSendQueueReader(client);
        Assert.True(reader.TryRead(out var queued));
        Assert.False(queued.IsStoreInfo);
        Assert.False(reader.TryRead(out _));
    }

    [Fact]
    public void StoreInfo_write_has_priority_without_reading_data_queue()
    {
        using var client = new RerunGrpcClient(RerunGrpcEndpoint.Default, maxQueueMessages: 1);

        client.Write(DataMessage());
        client.Write(StoreInfoMessage());

        Assert.Equal(0, client.DroppedCount);
        Assert.True(TryTakeNextMessage(client, out var first));
        Assert.True(first.IsStoreInfo);
        Assert.True(TryTakeNextMessage(client, out var second));
        Assert.False(second.IsStoreInfo);
        Assert.False(TryTakeNextMessage(client, out _));
    }

    [Fact]
    public void Grpc_client_channel_options_include_http_handler()
    {
        var method = typeof(RerunGrpcClient).GetMethod("CreateChannelOptions",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var options = Assert.IsType<Grpc.Net.Client.GrpcChannelOptions>(method.Invoke(null, null));
        Assert.NotNull(options.HttpHandler);
    }

    [Fact]
    public void Cysharp_http2_handler_is_used_when_loaded()
    {
        var method = typeof(RerunGrpcClient).GetMethod("TryCreateCysharpHttp2Handler",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        using var handler = Assert.IsType<Cysharp.Net.Http.YetAnotherHttpHandler>(
            method.Invoke(null, null));
        Assert.True(handler.Http2Only);
    }

    [Fact]
    public void Cancelled_without_grpc_status_is_expected_during_shutdown()
    {
        var method = typeof(RerunGrpcClient).GetMethod("IsExpectedShutdownException",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var ex = new RpcException(new Status(StatusCode.Cancelled,
            "No grpc-status found on response."));

        Assert.True((bool)method.Invoke(null, new object[] { ex })!);
    }

    [Fact]
    public void Internal_rpc_error_is_not_expected_during_shutdown()
    {
        var method = typeof(RerunGrpcClient).GetMethod("IsExpectedShutdownException",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);

        var ex = new RpcException(new Status(StatusCode.Internal,
            "Bad gRPC response. Response protocol downgraded to HTTP/1.1."));

        Assert.False((bool)method.Invoke(null, new object[] { ex })!);
    }

    [Fact]
    public void Viewer_probe_accepts_tcp_listener()
    {
        // Probe is now TCP-only: any open port is considered "listening".
        // The actual gRPC handshake is deferred to RerunGrpcClient background reconnect.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Assert.True(RerunGrpcViewerProbe.IsViewerListening($"http://127.0.0.1:{port}", timeoutMs: 200));
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public void CompositeBackend_isolates_live_write_failure()
    {
        var file = new CountingBackend();
        var live = new ThrowingBackend();
        var composite = new CompositeRerunBackend(file, live);

        composite.Write(new EncodedRerunMessage(2, new byte[] { 0 }, new byte[] { 0 },
            isStoreInfo: false, isStatic: false));
        Assert.Equal(1, file.WriteCount); // file always succeeds
    }

    [Fact]
    public void CompositeBackend_file_error_is_not_swallowed()
    {
        var file = new ThrowingBackend();
        var live = new CountingBackend();
        var composite = new CompositeRerunBackend(file, live);

        Assert.Throws<InvalidOperationException>(() =>
            composite.Write(new EncodedRerunMessage(2, new byte[] { 0 }, new byte[] { 0 },
                isStoreInfo: false, isStatic: false)));
    }

    // ── test doubles ──

    private class CountingBackend : IRerunBackend
    {
        public int WriteCount;
        public void Initialize(RerunRuntime rt) { }
        public void Write(EncodedRerunMessage m) { WriteCount++; }
        public void Flush() { }
        public void Shutdown() { }
    }

    private class ThrowingBackend : IRerunBackend
    {
        public void Initialize(RerunRuntime rt) => throw new InvalidOperationException("boom");
        public void Write(EncodedRerunMessage m) => throw new InvalidOperationException("boom");
        public void Flush() => throw new InvalidOperationException("boom");
        public void Shutdown() => throw new InvalidOperationException("boom");
    }

    private static EncodedRerunMessage DataMessage()
    {
        return new EncodedRerunMessage(2, new byte[] { 1 }, new byte[] { 1 },
            isStoreInfo: false, isStatic: false);
    }

    private static EncodedRerunMessage StoreInfoMessage()
    {
        return new EncodedRerunMessage(1, new byte[] { 2 }, new byte[] { 2 },
            isStoreInfo: true, isStatic: false);
    }

    private static ChannelReader<EncodedRerunMessage> GetSendQueueReader(RerunGrpcClient client)
    {
        var field = typeof(RerunGrpcClient).GetField("_sendQueue",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        var channel = Assert.IsAssignableFrom<Channel<EncodedRerunMessage>>(field.GetValue(client));
        return channel.Reader;
    }

    private static bool TryTakeNextMessage(RerunGrpcClient client, out EncodedRerunMessage message)
    {
        var method = typeof(RerunGrpcClient).GetMethod("TryTakeNextMessage",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);

        object[] args = { default(EncodedRerunMessage) };
        var result = (bool)method.Invoke(client, args)!;
        message = (EncodedRerunMessage)args[0];
        return result;
    }
}

namespace Cysharp.Net.Http
{
    public sealed class YetAnotherHttpHandler : HttpMessageHandler
    {
        public bool? Http2Only { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}

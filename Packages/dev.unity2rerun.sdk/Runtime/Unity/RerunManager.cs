// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;
using Unity.RerunSDK.Transport;
using Unity.RerunSDK.Transport.Grpc;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    [AddComponentMenu("Rerun/Rerun Manager")]
    public class RerunManager : MonoBehaviour
    {
        private const string DefaultOutputPath = "../build/RRD/unity_recording_{TIMESTAMP}.rrd";
        private const string LegacyPersistentOutputPath = "{PERSISTENT}/unity_recording.rrd";

        [SerializeField, Tooltip("Application name shown in Rerun Viewer.")]
        private string _applicationId = "unity_app";

        [SerializeField, Tooltip("Output mode: file, live, or both.")]
        private RerunOutputMode _outputMode = RerunOutputMode.FileOnly;

        [SerializeField, Tooltip(".rrd output file path. Relative paths resolve from the Unity project root. Use {PERSISTENT} and/or {TIMESTAMP}.")]
        private string _outputPath = DefaultOutputPath;

        [SerializeField, Tooltip("Automatically start recording on Start.")]
        private bool _recordOnStart = true;

        [SerializeField, Tooltip("Write ViewCoordinates on world entity at recording start.")]
        private bool _writeViewCoordinates = true;

        // ── Live settings ──

        [SerializeField, Tooltip("Rerun gRPC endpoint for live transport.")]
        private string _liveEndpoint = "rerun+http://127.0.0.1:9876/proxy";

        [SerializeField, Tooltip("Automatically launch Rerun Viewer if needed.")]
        private bool _autoLaunchViewer = true;

        [SerializeField, Tooltip("Full path to rerun executable. Empty = auto-detect from PATH.")]
        private string _viewerExecutablePath = "";

        [SerializeField, Tooltip("Connection timeout in milliseconds.")]
        private int _connectTimeoutMs = 3000;

        [SerializeField, Tooltip("Reconnect delay in milliseconds.")]
        private int _reconnectDelayMs = 1000;

        [SerializeField, Tooltip("Maximum live message queue size before dropping.")]
        private int _maxLiveQueueMessages = 2048;

        private RerunRuntime _runtime;
        private IRerunBackend _backend;
        private ManagedRerunEncoder _encoder;

        // File-only resources
        private FileStream _fileStream;
        private RrdWriter _rrdWriter;

        // Live resources
        private RerunGrpcClient _grpcClient;
        private RerunViewerLauncher _launcher;

        private string _resolvedPath;

        public bool IsRecording { get; private set; }

        public RerunLiveState LiveState { get; private set; } = RerunLiveState.Disabled;

        private void Awake()
        {
            MigrateLegacyOutputPath();
            _encoder = new ManagedRerunEncoder();
        }

        private void OnValidate()
        {
            MigrateLegacyOutputPath();
        }

        private void Start()
        {
            if (_recordOnStart)
                StartRecording();
        }

        public void StartRecording()
        {
            if (IsRecording) return;

            try
            {
                BuildBackend();
                _runtime = new RerunRuntime(_applicationId, _backend);
                _runtime.Start();

                // Send StoreInfo to all backends after stream header is written
                _backend.Write(_encoder.EncodeSetStoreInfoMessage(
                    _runtime.RecordingId, _applicationId));

                if (_writeViewCoordinates)
                    WriteViewCoordinates();

                IsRecording = true;
                Debug.Log($"[Rerun] Recording started mode={_outputMode}" +
                    (_outputMode != RerunOutputMode.LiveOnly ? $" → {_resolvedPath}" : ""));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Rerun] Failed to start recording: {ex.Message}");
                CleanupResources();
            }
        }

        private void BuildBackend()
        {
            IRerunBackend fileBackend = null;
            IRerunBackend liveBackend = null;

            if (_outputMode == RerunOutputMode.FileOnly || _outputMode == RerunOutputMode.FileAndLive)
            {
                _resolvedPath = ResolvePath();
                var dir = Path.GetDirectoryName(_resolvedPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                _fileStream = new FileStream(_resolvedPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                _rrdWriter = new RrdWriter(_fileStream);
                fileBackend = new RrdRerunBackend(_rrdWriter);
            }

            if (_outputMode == RerunOutputMode.LiveOnly || _outputMode == RerunOutputMode.FileAndLive)
            {
                var ep = RerunGrpcEndpoint.Parse(_liveEndpoint);
                _launcher = new RerunViewerLauncher();

                LiveState = RerunLiveState.LaunchingViewer;
                bool viewerReady = _launcher.EnsureViewerRunning(
                    ep, _viewerExecutablePath, _autoLaunchViewer, Math.Max(_connectTimeoutMs, 10000));

                if (!viewerReady)
                {
                    LiveState = RerunLiveState.Failed;
                    Debug.LogWarning("[Rerun] Viewer not available; live transport disabled for this session." +
                        (_outputMode != RerunOutputMode.LiveOnly ? " .rrd path continues." : ""));
                    if (_outputMode == RerunOutputMode.LiveOnly)
                        throw new InvalidOperationException("Live-only mode requires a running Rerun Viewer");
                    // FileAndLive: fall through to file-only
                }
                else
                {
                    LiveState = RerunLiveState.Connecting;
                    _grpcClient = new RerunGrpcClient(ep, _connectTimeoutMs, _reconnectDelayMs, _maxLiveQueueMessages);
                    liveBackend = new GrpcRerunBackend(_grpcClient);
                    // Connected will be set when the first WriteMessages stream succeeds (Phase 4.5)
                }
            }

            if (fileBackend != null && liveBackend != null)
                _backend = new CompositeRerunBackend(fileBackend, liveBackend);
            else if (liveBackend != null)
                _backend = liveBackend;
            else
                _backend = fileBackend!;
        }

        public void StopRecording()
        {
            if (!IsRecording) return;
            IsRecording = false;

            if (_runtime != null)
            {
                _runtime.Stop();
                _runtime.Dispose();
                _runtime = null;
            }

            CleanupResources();
            Debug.Log($"[Rerun] Recording stopped" +
                (_outputMode != RerunOutputMode.LiveOnly ? $" → {_resolvedPath}" : ""));
        }

        private void CleanupResources()
        {
            _rrdWriter?.Dispose();
            _rrdWriter = null;
            _fileStream?.Dispose();
            _fileStream = null;

            _grpcClient?.Dispose();
            _grpcClient = null;

            _launcher?.StopOwnedProcess();
            _launcher = null;

            _backend = null;
            LiveState = RerunLiveState.Disconnected;
        }

        // ── Timeline API ──

        public void SetTimeSequence(string name, long value)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTimeline(name, value, RerunTimelineKind.Sequence);
        }

        public void SetTimeTimestampNs(string name, long unixNs)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTimeline(name, unixNs, RerunTimelineKind.TimestampNs);
        }

        public void SetTimeDurationNs(string name, long durationNs)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTimeline(name, durationNs, RerunTimelineKind.DurationNs);
        }

        public void SetTime(string timelineName, long value)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTime(new RerunTimeline(timelineName), value);
        }

        public void ResetTime(string name) => _runtime?.ResetTime(name);
        public void ResetAllTimes() => _runtime?.ResetAllTimes();

        // ── Logging API ──

        public void LogText(string entityPath, string text, string level = "INFO")
        {
            if (_runtime == null || !IsRecording) return;
            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeTextLogMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, text, level, snapshot.ToEntries()));
        }

        public void LogScalar(string entityPath, double value)
        {
            if (_runtime == null || !IsRecording) return;
            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeScalarMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, value, snapshot.ToEntries()));
        }

        public void LogTransform(string entityPath, Transform transform)
        {
            if (transform == null) return;
            LogTransform(entityPath, transform.position, transform.rotation);
        }

        public void LogTransform(string entityPath, Vector3 position, Quaternion rotation)
        {
            if (_runtime == null || !IsRecording) return;
            var pos = RerunCoordinateConverter.ToRerunPosition(position);
            var rot = RerunCoordinateConverter.ToRerunRotation(rotation);
            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeTransform3DMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w,
                snapshot.ToEntries()));
        }

        private void WriteViewCoordinates()
        {
            _backend.Write(_encoder.EncodeViewCoordinatesMessage(
                _runtime.RecordingId, _applicationId, "world", 3, 1, 6));
        }

        private string ResolvePath()
        {
            var expandedPath = _outputPath
                .Replace("{PERSISTENT}", Application.persistentDataPath)
                .Replace("{TIMESTAMP}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            if (Path.IsPathRooted(expandedPath))
                return Path.GetFullPath(expandedPath);

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, expandedPath));
        }

        private void MigrateLegacyOutputPath()
        {
            if (string.Equals(_outputPath, LegacyPersistentOutputPath, StringComparison.Ordinal))
                _outputPath = DefaultOutputPath;
        }

        private void OnDestroy()
        {
            if (IsRecording) StopRecording();
        }
    }
}

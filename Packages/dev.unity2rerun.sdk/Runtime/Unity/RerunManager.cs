// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Unity
// Purpose: Integrates managed Rerun logging with Unity runtime components.

using System;
using System.Collections.Generic;
using System.IO;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;
using Unity.RerunSDK.Transport;
using Unity.RerunSDK.Transport.Grpc;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Coordinates recording lifecycle, timelines, file output, live transport, and public log APIs.
    /// </summary>
    [AddComponentMenu("Rerun/Rerun Manager")]
    public class RerunManager : MonoBehaviour
    {
        /// <summary>Default relative RRD path used when the inspector field has not been customized.</summary>
        private const string DefaultOutputPath = "../build/RRD/unity_recording_{TIMESTAMP}.rrd";
        /// <summary>Backward-compatible persistent-data token path retained for older scenes.</summary>
        private const string LegacyPersistentOutputPath = "{PERSISTENT}/unity_recording.rrd";
        /// <summary>Polling interval for discovering generated log source components in active scenes.</summary>
        private const float GeneratedLogDiscoveryIntervalSeconds = 1f;

        [SerializeField, Tooltip("Application name shown in Rerun Viewer.")]
        private string _applicationId = "unity_app";

        [SerializeField, Tooltip("Output mode: file, live, or both.")]
        private RerunOutputMode _outputMode = RerunOutputMode.FileOnly;

        [SerializeField, Tooltip(".rrd output file path. Relative paths resolve from the Unity project root. Use {PERSISTENT} and/or {TIMESTAMP}.")]
        private string _outputPath = DefaultOutputPath;

        [SerializeField, Tooltip("Automatically start recording on Start.")]
        private bool _recordOnStart = true;

        [SerializeField, Tooltip("Keep Unity running while Rerun Viewer or sidecar browser has focus.")]
        private bool _runInBackground = true;

        [SerializeField, Tooltip("Write ViewCoordinates on world entity at recording start.")]
        private bool _writeViewCoordinates = true;

        // -- Live settings --

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
        private readonly Dictionary<IRerunGeneratedLogSource, float[]> _generatedLogTimers = new();
        private readonly List<IRerunGeneratedLogSource> _generatedLogSnapshot = new();
        private readonly List<IRerunGeneratedLogSource> _generatedLogStale = new();
        /// <summary>Shared registry populated by generated log sources as they become active.</summary>
        private static readonly List<IRerunGeneratedLogSource> GeneratedLogSources = new();
        private float _generatedLogDiscoveryTimer;
        private bool _warnedBoxes3DRotationLengthMismatch;
        private bool _warnedBoxes3DColorLengthMismatch;
        private bool _warnedPoints3DColorLengthMismatch;
        private bool _warnedPoints3DRadiusLengthMismatch;

        public bool IsRecording { get; private set; }

        public RerunLiveState LiveState { get; private set; } = RerunLiveState.Disabled;

        public string ResolvedOutputPath => _resolvedPath;
        /// <summary>
        /// Returns the current runtime value or snapshot.
        /// </summary>
        public RerunTransportStatsSnapshot GetTransportStatsSnapshot()
        {
            if (_grpcClient != null)
            {
                var snapshot = _grpcClient.GetStatsSnapshot();
                LiveState = snapshot.LiveState;
                return snapshot;
            }

            var supported = _outputMode == RerunOutputMode.LiveOnly ||
                            _outputMode == RerunOutputMode.FileAndLive;
            return new RerunTransportStatsSnapshot(
                supported: supported,
                isRunning: false,
                liveState: LiveState,
                queueDepth: 0,
                droppedCount: 0,
                reconnectCount: 0,
                sentStoreInfoCount: 0,
                sentDataCount: 0,
                lastError: "");
        }

        private void Awake()
        {
            if (_runInBackground)
                Application.runInBackground = true;

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
        /// <summary>
        /// Updates the generated logging source registry.
        /// </summary>
        public static void RegisterGeneratedLogSource(IRerunGeneratedLogSource source)
        {
            if (source == null) return;
            if (!GeneratedLogSources.Contains(source))
                GeneratedLogSources.Add(source);
        }
        /// <summary>
        /// Updates the generated logging source registry.
        /// </summary>
        public static void UnregisterGeneratedLogSource(IRerunGeneratedLogSource source)
        {
            if (source == null) return;
            GeneratedLogSources.Remove(source);
        }
        /// <summary>
        /// Handles the StartRecording workflow for this component.
        /// </summary>
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
                _generatedLogDiscoveryTimer = 0f;
                DiscoverGeneratedLogSources();
                Debug.Log($"[Rerun] Recording started mode={_outputMode}" +
                    (_outputMode != RerunOutputMode.LiveOnly ? $" -> {_resolvedPath}" : ""));
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
        /// <summary>
        /// Handles the StopRecording workflow for this component.
        /// </summary>
        public void StopRecording()
        {
            if (!IsRecording) return;
            IsRecording = false;
            _generatedLogTimers.Clear();

            if (_runtime != null)
            {
                _runtime.Stop();
                _runtime.Dispose();
                _runtime = null;
            }

            CleanupResources();
            Debug.Log($"[Rerun] Recording stopped" +
                (_outputMode != RerunOutputMode.LiveOnly ? $" -> {_resolvedPath}" : ""));
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

        // -- Timeline API --

        private void Update()
        {
            DiscoverGeneratedLogSourcesIfDue();
            DriveGeneratedLogSources();
        }

        private void DiscoverGeneratedLogSourcesIfDue()
        {
            if (!IsRecording)
                return;

            _generatedLogDiscoveryTimer -= Time.deltaTime;
            if (_generatedLogDiscoveryTimer > 0f)
                return;

            _generatedLogDiscoveryTimer = GeneratedLogDiscoveryIntervalSeconds;
            DiscoverGeneratedLogSources();
        }

        private static void DiscoverGeneratedLogSources()
        {
#if UNITY_2023_1_OR_NEWER
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var behaviours = FindObjectsOfType<MonoBehaviour>();
#endif
            foreach (var behaviour in behaviours)
            {
                if (behaviour is IRerunGeneratedLogSource source)
                    RegisterGeneratedLogSource(source);
            }
        }

        private void DriveGeneratedLogSources()
        {
            if (!IsRecording || _runtime == null || GeneratedLogSources.Count == 0)
                return;

            _generatedLogSnapshot.Clear();
            _generatedLogSnapshot.AddRange(GeneratedLogSources);
            _generatedLogStale.Clear();

            var dt = Time.deltaTime;
            foreach (var source in _generatedLogSnapshot)
            {
                if (source == null)
                    continue;

                if (source is MonoBehaviour mb)
                {
                    if (mb == null)
                    {
                        _generatedLogStale.Add(source);
                        continue;
                    }

                    if (!mb.isActiveAndEnabled)
                        continue;
                }

                int count;
                try
                {
                    count = source.RerunLog_EntryCount;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[RerunLog] Failed to read generated entry count: {ex.Message}");
                    continue;
                }

                if (count <= 0)
                    continue;

                if (!_generatedLogTimers.TryGetValue(source, out var timers) || timers.Length != count)
                {
                    timers = new float[count];
                    _generatedLogTimers[source] = timers;
                }

                for (var i = 0; i < count; i++)
                {
                    timers[i] -= dt;
                    if (timers[i] > 0f)
                        continue;

                    RerunGeneratedLogEntry entry;
                    try
                    {
                        entry = source.RerunLog_GetEntry(i);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RerunLog] Failed to read generated entry {i}: {ex.Message}");
                        timers[i] = 1f;
                        continue;
                    }

                    timers[i] = entry.RateHz > 0f ? 1f / entry.RateHz : 0f;

                    try
                    {
                        source.RerunLog_Publish(i, this);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[RerunLog] Generated publish failed for {entry.EntityPath}: {ex.Message}");
                    }
                }
            }

            foreach (var stale in _generatedLogStale)
            {
                GeneratedLogSources.Remove(stale);
                _generatedLogTimers.Remove(stale);
            }
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void SetTimeSequence(string name, long value)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTimeline(name, value, RerunTimelineKind.Sequence);
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void SetTimeTimestampNs(string name, long unixNs)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTimeline(name, unixNs, RerunTimelineKind.TimestampNs);
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void SetTimeDurationNs(string name, long durationNs)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTimeline(name, durationNs, RerunTimelineKind.DurationNs);
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void SetTime(string timelineName, long value)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTime(new RerunTimeline(timelineName), value);
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void ResetTime(string name) => _runtime?.ResetTime(name);
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void ResetAllTimes() => _runtime?.ResetAllTimes();

        // -- Logging API --
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogText(string entityPath, string text, string level = "INFO")
        {
            if (_runtime == null || !IsRecording) return;
            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeTextLogMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, text, level, snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogScalar(string entityPath, double value)
        {
            if (_runtime == null || !IsRecording) return;
            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeScalarMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, value, snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogTransform(string entityPath, Transform transform)
        {
            if (transform == null) return;
            LogTransform(entityPath, transform.position, transform.rotation);
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
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
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogEncodedImage(string entityPath, byte[] encodedBytes, string mediaType)
        {
            if (_runtime == null || !IsRecording) return;
            if (encodedBytes == null || encodedBytes.Length == 0) return;

            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeEncodedImageMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, encodedBytes, mediaType, snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogPinhole(string entityPath, RerunPinhole pinhole)
        {
            if (_runtime == null || !IsRecording) return;

            _backend.Write(_encoder.EncodePinholeMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, pinhole));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogBox3D(string entityPath, Transform target, Color color)
        {
            if (target == null) return;
            var halfSize = new Vector3(
                Mathf.Abs(target.lossyScale.x) * 0.5f,
                Mathf.Abs(target.lossyScale.y) * 0.5f,
                Mathf.Abs(target.lossyScale.z) * 0.5f);
            LogBox3D(entityPath, target.position, halfSize, target.rotation, color);
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogBox3D(string entityPath, Vector3 center, Vector3 halfSize, Quaternion rotation, Color color)
        {
            LogBoxes3D(
                entityPath,
                new[] { center },
                new[] { halfSize },
                new[] { rotation },
                new[] { color });
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogBoxes3D(
            string entityPath,
            IReadOnlyList<Vector3> centers,
            IReadOnlyList<Vector3> halfSizes,
            IReadOnlyList<Quaternion> rotations = null,
            IReadOnlyList<Color> colors = null)
        {
            if (_runtime == null || !IsRecording) return;
            if (centers == null || halfSizes == null) return;

            var count = Math.Min(centers.Count, halfSizes.Count);
            if (count <= 0) return;

            if (rotations != null && rotations.Count != count && !_warnedBoxes3DRotationLengthMismatch)
            {
                Debug.LogWarning($"[Rerun] LogBoxes3D('{entityPath}') rotations count {rotations.Count} does not match box count {count}; missing rotations use identity.");
                _warnedBoxes3DRotationLengthMismatch = true;
            }

            if (colors != null && colors.Count != count && !_warnedBoxes3DColorLengthMismatch)
            {
                Debug.LogWarning($"[Rerun] LogBoxes3D('{entityPath}') colors count {colors.Count} does not match box count {count}; missing colors use green.");
                _warnedBoxes3DColorLengthMismatch = true;
            }

            var boxes = new List<RerunBox3D>(count);
            for (var i = 0; i < count; i++)
            {
                var center = RerunCoordinateConverter.ToRerunPosition(centers[i]);
                var halfSize = AbsVector(halfSizes[i]);
                var rotation = rotations != null && i < rotations.Count
                    ? RerunCoordinateConverter.ToRerunRotation(rotations[i])
                    : Quaternion.identity;
                var color = colors != null && i < colors.Count ? colors[i] : Color.green;

                boxes.Add(new RerunBox3D(
                    ToRerunVec3(center),
                    ToRerunVec3(halfSize),
                    new RerunQuat(rotation.x, rotation.y, rotation.z, rotation.w),
                    ToRgba32(color)));
            }

            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeBoxes3DMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, boxes, snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogLineStrip3D(string entityPath, IReadOnlyList<Vector3> points, Color color)
        {
            LogLineStrips3D(entityPath, points, color);
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogLineStrips3D(string entityPath, IReadOnlyList<Vector3> points, Color color)
        {
            if (_runtime == null || !IsRecording) return;
            if (points == null || points.Count == 0) return;

            var convertedPoints = new List<RerunVec3>(points.Count);
            for (var i = 0; i < points.Count; i++)
                convertedPoints.Add(ToRerunVec3(RerunCoordinateConverter.ToRerunPosition(points[i])));

            var strip = new RerunLineStrip3D(convertedPoints, ToRgba32(color));
            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeLineStrips3DMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, new[] { strip }, snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogPoints3D(string entityPath, IReadOnlyList<Vector3> positions, Color color, float radius = 0.03f)
        {
            if (_runtime == null || !IsRecording) return;
            if (positions == null || positions.Count == 0) return;

            var colors = new Color[positions.Count];
            var radii = new float[positions.Count];
            for (var i = 0; i < positions.Count; i++)
            {
                colors[i] = color;
                radii[i] = radius;
            }

            LogPoints3D(entityPath, positions, colors, radii);
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogPoints3D(
            string entityPath,
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<Color> colors = null,
            IReadOnlyList<float> radii = null)
        {
            if (_runtime == null || !IsRecording) return;
            if (positions == null || positions.Count == 0) return;

            if (colors != null && colors.Count != positions.Count && !_warnedPoints3DColorLengthMismatch)
            {
                Debug.LogWarning($"[Rerun] LogPoints3D('{entityPath}') colors count {colors.Count} does not match point count {positions.Count}; missing colors use cyan.");
                _warnedPoints3DColorLengthMismatch = true;
            }

            if (radii != null && radii.Count != positions.Count && !_warnedPoints3DRadiusLengthMismatch)
            {
                Debug.LogWarning($"[Rerun] LogPoints3D('{entityPath}') radii count {radii.Count} does not match point count {positions.Count}; missing radii use 0.03.");
                _warnedPoints3DRadiusLengthMismatch = true;
            }

            var points = new List<RerunPoint3D>(positions.Count);
            for (var i = 0; i < positions.Count; i++)
            {
                var position = RerunCoordinateConverter.ToRerunPosition(positions[i]);
                var color = colors != null && i < colors.Count ? colors[i] : Color.cyan;
                var radius = radii != null && i < radii.Count ? Mathf.Max(0f, radii[i]) : 0.03f;
                points.Add(new RerunPoint3D(ToRerunVec3(position), ToRgba32(color), radius));
            }

            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodePoints3DMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, points, snapshot.ToEntries()));
        }

        private void WriteViewCoordinates()
        {
            _backend.Write(_encoder.EncodeViewCoordinatesMessage(
                _runtime.RecordingId, _applicationId, "world", 3, 1, 6));
        }

        private static Vector3 AbsVector(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static RerunVec3 ToRerunVec3(Vector3 value)
        {
            return new RerunVec3(value.x, value.y, value.z);
        }

        private static uint ToRgba32(Color color)
        {
            var r = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f);
            var g = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f);
            var b = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f);
            var a = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.a) * 255f);
            return (r << 24) | (g << 16) | (b << 8) | a;
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

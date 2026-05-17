// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components
// Purpose: Integrates managed Rerun logging with Unity runtime components.

using System.Collections.Generic;
using System.IO;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;
using Unity.RerunSDK.Transport.Grpc;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Coordinates recording lifecycle, timelines, file output, live transport, and public log APIs.
    /// </summary>
    [AddComponentMenu("Rerun/Rerun Manager")]
    public partial class RerunManager : MonoBehaviour
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
    }
}

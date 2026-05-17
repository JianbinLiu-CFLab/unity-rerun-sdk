// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components/Manager
// Purpose: Builds, starts, stops, and cleans up RRD and live transport backends.

using System;
using System.IO;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.IO.Rrd;
using Unity.RerunSDK.Transport;
using Unity.RerunSDK.Transport.Grpc;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    public partial class RerunManager
    {
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
    }
}

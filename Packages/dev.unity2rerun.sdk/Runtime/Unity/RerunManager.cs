// SPDX-License-Identifier: Apache-2.0

using System;
using System.IO;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    /// MonoBehaviour entry point for the Rerun SDK.
    [AddComponentMenu("Rerun/Rerun Manager")]
    public class RerunManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Application name shown in Rerun Viewer.")]
        private string _applicationId = "unity_app";

        [SerializeField, Tooltip(".rrd output file path. Use {PERSISTENT} and/or {TIMESTAMP}.")]
        private string _outputPath = "{PERSISTENT}/unity_recording.rrd";

        [SerializeField, Tooltip("Automatically start recording on Start.")]
        private bool _recordOnStart = true;

        [SerializeField, Tooltip("Write ViewCoordinates on world entity at recording start.")]
        private bool _writeViewCoordinates = true;

        private RerunRuntime _runtime;
        private RrdWriter _rrdWriter;
        private ManagedRerunEncoder _encoder;
        private RrdRerunBackend _backend;
        private string _resolvedPath;

        public bool IsRecording { get; private set; }

        private void Awake()
        {
            _encoder = new ManagedRerunEncoder();
        }

        private void Start()
        {
            if (_recordOnStart)
                StartRecording();
        }

        public void StartRecording()
        {
            if (IsRecording) return;

            _resolvedPath = ResolvePath();
            var dir = Path.GetDirectoryName(_resolvedPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            FileStream stream = null;
            RrdWriter writer = null;
            try
            {
                stream = new FileStream(_resolvedPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                writer = new RrdWriter(stream);

                _backend = new RrdRerunBackend(writer, _encoder, _applicationId);
                _runtime = new RerunRuntime(_applicationId, _backend);
                _runtime.Start();

                if (_writeViewCoordinates)
                {
                    WriteViewCoordinates();
                }

                _rrdWriter = writer;
                IsRecording = true;
                Debug.Log($"[Rerun] Recording started → {_resolvedPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Rerun] Failed to start recording: {ex.Message}");
                writer?.Dispose();
                stream?.Dispose();
                _rrdWriter = null;
                _backend = null;
                _runtime = null;
            }
        }

        public void StopRecording()
        {
            if (!IsRecording) return;
            _runtime.Stop();
            _runtime.Dispose();
            _rrdWriter.Dispose();
            _rrdWriter = null;
            _backend = null;
            _runtime = null;
            IsRecording = false;
            Debug.Log($"[Rerun] Recording stopped → {_resolvedPath}");
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

        public void ResetTime(string name)
        {
            _runtime?.ResetTime(name);
        }

        public void ResetAllTimes()
        {
            _runtime?.ResetAllTimes();
        }

        // ── Logging API ──

        public void LogText(string entityPath, string text, string level = "INFO")
        {
            if (_runtime == null || !IsRecording) return;
            var snapshot = _runtime.CaptureTimelineSnapshot();
            var payload = _encoder.EncodeTextLogMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, text, level, snapshot.ToEntries());
            _backend.Write(payload);
        }

        public void LogScalar(string entityPath, double value)
        {
            if (_runtime == null || !IsRecording) return;
            var snapshot = _runtime.CaptureTimelineSnapshot();
            var payload = _encoder.EncodeScalarMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, value, snapshot.ToEntries());
            _backend.Write(payload);
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
            var payload = _encoder.EncodeTransform3DMessage(
                _runtime.RecordingId, _applicationId,
                entityPath,
                pos.x, pos.y, pos.z,
                rot.x, rot.y, rot.z, rot.w,
                snapshot.ToEntries());
            _backend.Write(payload);
        }

        // ── Internal ──

        private void WriteViewCoordinates()
        {
            var payload = _encoder.EncodeViewCoordinatesMessage(
                _runtime.RecordingId, _applicationId,
                "world", 3, 1, 6); // Right=3, Up=1, Back=6 = RIGHT_HAND_Y_UP
            _backend.Write(payload);
        }

        private string ResolvePath()
        {
            return _outputPath
                .Replace("{PERSISTENT}", Application.persistentDataPath)
                .Replace("{TIMESTAMP}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        }

        private void OnDestroy()
        {
            if (IsRecording) StopRecording();
        }
    }
}

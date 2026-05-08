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
    /// Configure via Inspector: application name, output path, record on start.
    [AddComponentMenu("Rerun/Rerun Manager")]
    public class RerunManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Application name shown in Rerun Viewer.")]
        private string _applicationId = "unity_app";

        [SerializeField, Tooltip(".rrd output file path. Use {TIMESTAMP} for auto-naming. Defaults to persistentDataPath.")]
        private string _outputPath = "{PERSISTENT}/unity_recording.rrd";

        [SerializeField, Tooltip("Automatically start recording when the component is enabled.")]
        private bool _recordOnStart = true;

        private RerunRuntime _runtime;
        private RrdWriter _rrdWriter;
        private ManagedRerunEncoder _encoder;
        private RrdBackend _backend;

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

            var path = ResolvePath();
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            _rrdWriter = new RrdWriter(stream);

            _backend = new RrdBackend(_rrdWriter, _encoder, _applicationId);
            _runtime = new RerunRuntime(_applicationId, _backend);
            _runtime.Start();
            IsRecording = true;
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
        }

        /// Set the current timeline value (e.g. frame counter).
        public void SetTime(string timelineName, long value)
        {
            _runtime.SetTime(new RerunTimeline(timelineName), value);
        }

        /// Log a text message to the given entity path.
        public void LogText(string entityPath, string text, string level = "INFO")
        {
            if (!IsRecording) return;

            var logTick = _runtime.GetTime(RerunTimeline.LogTick) + 1;
            _runtime.SetTime(RerunTimeline.LogTick, logTick);

            var payload = _encoder.EncodeTextLogChunk(
                _runtime.RecordingId, _applicationId,
                entityPath, text, level, logTick);

            _backend.WritePayloadUnchecked(payload);
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

    /// Backend that writes directly to an RRD file.
    internal class RrdBackend : IRerunBackend
    {
        private readonly RrdWriter _writer;
        private readonly ManagedRerunEncoder _encoder;
        private readonly string _applicationId;
        private RerunRuntime _runtime;
        private bool _initialized;

        public RrdBackend(RrdWriter writer, ManagedRerunEncoder encoder, string applicationId)
        {
            _writer = writer;
            _encoder = encoder;
            _applicationId = applicationId;
        }

        public void Initialize(RerunRuntime runtime)
        {
            _runtime = runtime;
            _writer.WriteStreamHeader();

            var setStoreInfo = _encoder.EncodeSetStoreInfo(runtime.RecordingId, _applicationId);
            _writer.WriteMessage(RrdConstants.MsgKindSetStoreInfo, setStoreInfo);

            _initialized = true;
        }

        public void WriteMessage(byte[] payload)
        {
            _writer.WriteMessage(RrdConstants.MsgKindArrowMsg, payload);
        }

        /// Direct write for TextLog payloads from the encoder.
        public void WritePayloadUnchecked(byte[] payload)
        {
            _writer.WriteMessage(RrdConstants.MsgKindArrowMsg, payload);
        }

        public void Flush()
        {
            _writer.FinishNoFooter();
        }

        public void Shutdown()
        {
            _writer.FinishNoFooter();
        }
    }
}

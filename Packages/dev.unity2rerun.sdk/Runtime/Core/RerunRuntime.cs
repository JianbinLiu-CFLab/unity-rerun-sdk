// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace Unity.RerunSDK.Core
{
    /// Top-level entry point. Owns the recording state, backend, and entity/timeline state.
    public class RerunRuntime : IDisposable
    {
        public string ApplicationId { get; }
        public string RecordingId { get; }

        private readonly IRerunBackend _backend;
        private readonly Dictionary<RerunTimeline, long> _timelines = new();

        public bool IsRunning { get; private set; }

        public RerunRuntime(string applicationId, IRerunBackend backend)
        {
            ApplicationId = applicationId;
            RecordingId = Guid.NewGuid().ToString();
            _backend = backend;
        }

        public void Start()
        {
            if (IsRunning) return;
            _backend.Initialize(this);
            IsRunning = true;
        }

        public void SetTime(RerunTimeline timeline, long value)
        {
            _timelines[timeline] = value;
        }

        public long GetTime(RerunTimeline timeline)
        {
            return _timelines.TryGetValue(timeline, out var v) ? v : 0;
        }

        /// Set all active timelines, then reset them after the batch.
        internal void FlushTimelines()
        {
            _timelines.Clear();
        }

        public void Stop()
        {
            if (!IsRunning) return;
            _backend.Flush();
            _backend.Shutdown();
            IsRunning = false;
        }

        public void Dispose()
        {
            if (IsRunning) Stop();
        }
    }
}

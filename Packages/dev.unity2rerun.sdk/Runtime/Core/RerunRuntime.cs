// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Core
// Purpose: Defines core Rerun runtime concepts shared by encoding, transport, and Unity layers.

using System;
using System.Collections.Generic;

namespace Unity.RerunSDK.Core
{
    /// <summary>
    /// Provides Rerun Runtime support for Unity2Rerun.
    /// </summary>
    internal class RerunRuntime : IDisposable
    {
        public string ApplicationId { get; }
        public string RecordingId { get; }

        private readonly IRerunBackend _backend;
        private readonly Dictionary<string, (long Value, RerunTimelineKind Kind)> _timelines = new();
        private long _logTickCounter;
        private long _logTimeCounter;

        public bool IsRunning { get; private set; }

        public RerunRuntime(string applicationId, IRerunBackend backend)
        {
            ApplicationId = applicationId;
            RecordingId = Guid.NewGuid().ToString();
            _backend = backend;
        }
        /// <summary>
        /// Starts the component or service and prepares its runtime resources.
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;
            _backend.Initialize(this);
            IsRunning = true;
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void SetTimeline(string name, long value, RerunTimelineKind kind)
        {
            if (!IsRunning) return;
            _timelines[name] = (value, kind);
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void SetTime(RerunTimeline timeline, long value)
        {
            if (!IsRunning) return;
            _timelines[timeline.Name] = (value, RerunTimelineKind.Sequence);
        }
        /// <summary>
        /// Returns the current runtime value or snapshot.
        /// </summary>
        public long GetTime(RerunTimeline timeline)
        {
            return _timelines.TryGetValue(timeline.Name, out var v) ? v.Value : 0;
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void ResetTime(string name)
        {
            _timelines.Remove(name);
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void ResetAllTimes()
        {
            _timelines.Clear();
            _logTickCounter = 0;
            _logTimeCounter = 0;
        }

        /// Snapshot all active timelines plus auto-maintained log_tick / log_time.
        public RerunTimelineSnapshot CaptureTimelineSnapshot()
        {
            var snap = new RerunTimelineSnapshot();

            // Auto timelines
            _logTickCounter++;
            snap.SetLogTick(_logTickCounter);

            _logTimeCounter = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000; // Unix ns
            snap.Set("log_time", _logTimeCounter, RerunTimelineKind.TimestampNs);

            // User timelines
            foreach (var kv in _timelines)
                snap.Set(kv.Key, kv.Value.Value, kv.Value.Kind);

            return snap;
        }
        /// <summary>
        /// Stops the component or service and releases owned runtime resources.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning) return;
            _backend.Flush();
            _backend.Shutdown();
            IsRunning = false;
        }
        /// <summary>
        /// Stops the component or service and releases owned runtime resources.
        /// </summary>
        public void Dispose()
        {
            if (IsRunning) Stop();
        }
    }
}

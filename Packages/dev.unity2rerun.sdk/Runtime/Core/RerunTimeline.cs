// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Core
// Purpose: Defines core Rerun runtime concepts shared by encoding, transport, and Unity layers.

using System.Collections.Generic;

namespace Unity.RerunSDK.Core
{
    /// <summary>
    /// Carries Rerun Timeline data across Unity2Rerun runtime boundaries.
    /// </summary>
    public readonly struct RerunTimeline
    {
        public string Name { get; }

        public static readonly RerunTimeline LogTick = new("log_tick");
        public static readonly RerunTimeline LogTime = new("log_time");

        public RerunTimeline(string name)
        {
            Name = string.IsNullOrEmpty(name) ? "log_tick" : name;
        }

        public static implicit operator RerunTimeline(string name) => new(name);
        /// <summary>
        /// Handles the ToString workflow for this component.
        /// </summary>
        public override string ToString() => Name;
    }
    /// <summary>
    /// Enumerates supported Rerun Timeline Kind values.
    /// </summary>
    public enum RerunTimelineKind
    {
        Sequence,
        TimestampNs,
        DurationNs
    }

    /// Snapshot of active timelines at a log call moment.
    public class RerunTimelineSnapshot
    {
        private readonly Dictionary<string, (long Value, RerunTimelineKind Kind)> _timelines
            = new();

        private long _logTick;
        /// <summary>
        /// Sets runtime input used by subsequent publishing.
        /// </summary>
        internal void Set(string name, long value, RerunTimelineKind kind)
        {
            _timelines[name] = (value, kind);
        }
        /// <summary>
        /// Sets runtime input used by subsequent publishing.
        /// </summary>
        internal void SetLogTick(long tick)
        {
            _logTick = tick;
        }
        /// <summary>
        /// Returns the current runtime value or snapshot.
        /// </summary>
        public long GetLogTick() => _logTick;

        public IEnumerable<(string Name, long Value, RerunTimelineKind Kind)> All()
        {
            foreach (var kv in _timelines)
                yield return (kv.Key, kv.Value.Value, kv.Value.Kind);
        }
    }
}

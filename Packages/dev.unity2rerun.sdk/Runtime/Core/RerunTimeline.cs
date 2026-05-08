// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace Unity.RerunSDK.Core
{
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
        public override string ToString() => Name;
    }

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

        internal void Set(string name, long value, RerunTimelineKind kind)
        {
            _timelines[name] = (value, kind);
        }

        internal void SetLogTick(long tick)
        {
            _logTick = tick;
        }

        public long GetLogTick() => _logTick;

        public IEnumerable<(string Name, long Value, RerunTimelineKind Kind)> All()
        {
            foreach (var kv in _timelines)
                yield return (kv.Key, kv.Value.Value, kv.Value.Kind);
        }
    }
}

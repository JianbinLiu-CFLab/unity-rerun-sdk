// SPDX-License-Identifier: Apache-2.0

namespace Unity.RerunSDK.Core
{
    /// Represents a Rerun timeline — a named time domain.
    /// Common timelines: "log_tick" (frame counter), "log_time" (wall clock).
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
}

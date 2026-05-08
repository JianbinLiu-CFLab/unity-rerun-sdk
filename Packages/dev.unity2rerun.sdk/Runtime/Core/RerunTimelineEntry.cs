// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace Unity.RerunSDK.Core
{
    /// A single timeline value with kind, passed from Runtime through Encoder to Arrow writer.
    public readonly struct RerunTimelineEntry
    {
        public string Name { get; }
        public long Value { get; }
        public RerunTimelineKind Kind { get; }

        public RerunTimelineEntry(string name, long value, RerunTimelineKind kind)
        {
            Name = name;
            Value = value;
            Kind = kind;
        }
    }

    public static class RerunTimelineSnapshotExtensions
    {
        public static IReadOnlyList<RerunTimelineEntry> ToEntries(
            this RerunTimelineSnapshot snapshot)
        {
            var entries = new List<RerunTimelineEntry>();
            foreach (var (name, value, kind) in snapshot.All())
                entries.Add(new RerunTimelineEntry(name, value, kind));
            return entries;
        }
    }
}

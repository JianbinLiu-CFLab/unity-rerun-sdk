// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Core
// Purpose: Defines core Rerun runtime concepts shared by encoding, transport, and Unity layers.

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
    /// <summary>
    /// Provides Rerun Timeline Snapshot Extensions support for Unity2Rerun.
    /// </summary>
    public static class RerunTimelineSnapshotExtensions
    {
        /// <summary>
        /// Handles the ToEntries workflow for this component.
        /// </summary>
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

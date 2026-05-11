// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System.Collections.Generic;
using Apache.Arrow;
using Apache.Arrow.Types;
using Unity.RerunSDK.Core;

namespace Unity.RerunSDK.Encoding
{
    internal sealed class RrdManifestChunkInfo
    {
        public string RecordingId { get; }
        public string ApplicationId { get; }
        public string EntityPath { get; }
        public RerunTuid ChunkId { get; }
        public bool IsStatic { get; }
        public ulong RowCount { get; }
        public ulong UncompressedSize { get; }
        public Schema SorbetSchema { get; }
        public IReadOnlyList<RrdManifestTimelineInfo> Timelines { get; }
        public IReadOnlyList<RrdManifestComponentInfo> Components { get; }

        private RrdManifestChunkInfo(
            string recordingId,
            string applicationId,
            string entityPath,
            RerunTuid chunkId,
            bool isStatic,
            ulong rowCount,
            ulong uncompressedSize,
            Schema sorbetSchema,
            IReadOnlyList<RrdManifestTimelineInfo> timelines,
            IReadOnlyList<RrdManifestComponentInfo> components)
        {
            RecordingId = recordingId;
            ApplicationId = applicationId;
            EntityPath = entityPath;
            ChunkId = chunkId;
            IsStatic = isStatic;
            RowCount = rowCount;
            UncompressedSize = uncompressedSize;
            SorbetSchema = sorbetSchema;
            Timelines = timelines;
            Components = components;
        }

        public static RrdManifestChunkInfo FromSchema(
            string recordingId,
            string applicationId,
            string entityPath,
            RerunTuid chunkId,
            bool isStatic,
            ulong rowCount,
            ulong uncompressedSize,
            Schema schema,
            IReadOnlyList<RerunTimelineEntry> timelineEntries)
        {
            var timelineValues = new Dictionary<string, RerunTimelineEntry>();
            foreach (var entry in timelineEntries)
                timelineValues[entry.Name] = entry;

            var timelines = new List<RrdManifestTimelineInfo>();
            var components = new List<RrdManifestComponentInfo>();

            foreach (var field in schema.FieldsList)
            {
                var metadata = field.Metadata;
                if (metadata == null || !metadata.TryGetValue("rerun:kind", out var rerunKind))
                    continue;

                if (rerunKind == "index")
                {
                    var name = metadata.TryGetValue("rerun:index_name", out var indexName)
                        ? indexName
                        : field.Name;
                    if (timelineValues.TryGetValue(name, out var entry))
                    {
                        timelines.Add(new RrdManifestTimelineInfo(
                            name, entry.Value, entry.Kind, field.DataType));
                    }
                }
                else if (rerunKind == "data")
                {
                    metadata.TryGetValue("rerun:component", out var component);
                    metadata.TryGetValue("rerun:archetype", out var archetype);
                    metadata.TryGetValue("rerun:component_type", out var componentType);
                    var componentIsStatic =
                        isStatic ||
                        (metadata.TryGetValue("rerun:is_static", out var staticValue) &&
                         staticValue == "true");

                    components.Add(new RrdManifestComponentInfo(
                        archetype ?? "",
                        component ?? field.Name,
                        componentType ?? "",
                        componentIsStatic,
                        rowCount));
                }
            }

            return new RrdManifestChunkInfo(
                recordingId,
                applicationId,
                entityPath,
                chunkId,
                isStatic,
                rowCount,
                uncompressedSize,
                schema,
                timelines,
                components);
        }
    }

    internal readonly struct RrdManifestTimelineInfo
    {
        public string Name { get; }
        public long Value { get; }
        public RerunTimelineKind Kind { get; }
        public IArrowType DataType { get; }

        public RrdManifestTimelineInfo(string name, long value, RerunTimelineKind kind, IArrowType dataType)
        {
            Name = name;
            Value = value;
            Kind = kind;
            DataType = dataType;
        }
    }

    internal readonly struct RrdManifestComponentInfo
    {
        public string Archetype { get; }
        public string Component { get; }
        public string ComponentType { get; }
        public bool IsStatic { get; }
        public ulong NumRows { get; }

        public RrdManifestComponentInfo(
            string archetype,
            string component,
            string componentType,
            bool isStatic,
            ulong numRows)
        {
            Archetype = archetype;
            Component = component;
            ComponentType = componentType;
            IsStatic = isStatic;
            NumRows = numRows;
        }
    }
}

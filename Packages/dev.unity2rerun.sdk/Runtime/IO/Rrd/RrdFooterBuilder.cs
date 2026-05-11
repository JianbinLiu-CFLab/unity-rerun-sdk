// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Apache.Arrow;
using Apache.Arrow.Arrays;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using Google.Protobuf;
using Rerun.Common.V1Alpha1;
using Rerun.LogMsg.V1Alpha1;
using Unity.RerunSDK.Encoding;

using ArrowSchema = Apache.Arrow.Schema;
using ProtoSchema = Rerun.Common.V1Alpha1.Schema;

namespace Unity.RerunSDK.IO.Rrd
{
    internal sealed class RrdFooterBuilder
    {
        private readonly List<ManifestChunkEntry> _chunks = new();

        public void AddChunk(RrdManifestChunkInfo info, RrdPayloadSpan payloadSpan)
        {
            _chunks.Add(new ManifestChunkEntry(info, payloadSpan));
        }

        public RrdFooter Build()
        {
            var footer = new RrdFooter();
            foreach (var group in _chunks.GroupBy(c => c.StoreKey).OrderBy(g => g.Key))
                footer.Manifests.Add(BuildManifest(group.ToList()));
            return footer;
        }

        private static RrdManifest BuildManifest(IReadOnlyList<ManifestChunkEntry> chunks)
        {
            var first = chunks[0].Info;
            var sorbetSchema = BuildSorbetSchema(chunks.Select(c => c.Info.SorbetSchema));
            var sorbetSchemaBytes = SerializeSchemaIpc(sorbetSchema);
            var sortedSorbetSchemaBytes = SerializeSchemaIpc(SortSchemaFields(sorbetSchema));
            var dataPayload = SerializeManifestRecordBatch(chunks, sorbetSchema);

            using var sha = SHA256.Create();
            return new RrdManifest
            {
                StoreId = new StoreId
                {
                    Kind = StoreKind.Recording,
                    RecordingId = first.RecordingId,
                    ApplicationId = new global::Rerun.Common.V1Alpha1.ApplicationId { Id = first.ApplicationId },
                },
                SorbetSchema = new ProtoSchema
                {
                    ArrowSchema = ByteString.CopyFrom(sorbetSchemaBytes),
                },
                SorbetSchemaSha256 = ByteString.CopyFrom(sha.ComputeHash(sortedSorbetSchemaBytes)),
                Data = new DataframePart
                {
                    EncoderVersion = EncoderVersion.V0,
                    Compression = Compression.None,
                    Payload = ByteString.CopyFrom(dataPayload),
                    UncompressedSize = (ulong)dataPayload.Length,
                },
            };
        }

        private static ArrowSchema BuildSorbetSchema(IEnumerable<ArrowSchema> schemas)
        {
            var fields = new Dictionary<string, Field>(StringComparer.Ordinal);
            foreach (var schema in schemas)
            {
                foreach (var field in schema.FieldsList)
                {
                    if (!fields.ContainsKey(field.Name))
                        fields[field.Name] = field;
                }
            }

            return new ArrowSchema(
                fields.Values.OrderBy(f => f.Name, StringComparer.Ordinal),
                System.Array.Empty<KeyValuePair<string, string>>());
        }

        private static ArrowSchema SortSchemaFields(ArrowSchema schema)
        {
            return new ArrowSchema(
                schema.FieldsList.OrderBy(f => f.Name, StringComparer.Ordinal),
                System.Array.Empty<KeyValuePair<string, string>>());
        }

        private static byte[] SerializeManifestRecordBatch(
            IReadOnlyList<ManifestChunkEntry> chunks,
            ArrowSchema sorbetSchema)
        {
            var rowCount = chunks.Count;
            var columns = new List<ManifestColumn>
            {
                new("chunk_id", new FixedSizeBinaryType(16), false,
                    CreateFixedSizeBinaryArray(16, chunks.Select(c => RerunTuidGenerator.ToBytes(c.Info.ChunkId)))),
                new("chunk_is_static", BooleanType.Default, false,
                    CreateBooleanArray(chunks.Select(c => c.Info.IsStatic))),
                new("chunk_num_rows", new UInt64Type(), false,
                    CreateUInt64Array(chunks.Select(c => c.Info.RowCount))),
                new("chunk_byte_offset", new UInt64Type(), false,
                    CreateUInt64Array(chunks.Select(c => c.PayloadSpan.Offset))),
                new("chunk_byte_size", new UInt64Type(), false,
                    CreateUInt64Array(chunks.Select(c => c.PayloadSpan.Length))),
                new("chunk_byte_size_uncompressed", new UInt64Type(), false,
                    CreateUInt64Array(chunks.Select(c => c.Info.UncompressedSize))),
                new("chunk_entity_path", StringType.Default, false,
                    CreateStringArray(chunks.Select(c => c.Info.EntityPath))),
            };

            var allComponents = CollectComponents(sorbetSchema);
            var allTimelines = CollectTimelines(sorbetSchema, chunks);
            var anyStaticChunks = chunks.Any(c => c.Info.IsStatic);

            if (anyStaticChunks)
            {
                foreach (var component in allComponents.Values.OrderBy(c => ComponentColumnName(c, null, "has_static_data"), StringComparer.Ordinal))
                {
                    var name = ComponentColumnName(component, null, "has_static_data");
                    columns.Add(new ManifestColumn(
                        CreateComponentField(name, BooleanType.Default, false, component, "rerun:static"),
                        CreateBooleanArray(chunks.Select(c =>
                            c.Info.IsStatic && c.Info.Components.Any(component.Matches)))));
                }
            }

            foreach (var timeline in allTimelines.Values.OrderBy(t => t.Name, StringComparer.Ordinal))
            {
                columns.Add(new ManifestColumn(
                    CreateTimelineField(TimelineColumnName(timeline.Name, "start"), timeline.DataType, true, timeline.Name, null),
                    CreateNullableInt64Array(chunks.Select(c => TimelineValueOrNull(c.Info, timeline.Name)))));
                columns.Add(new ManifestColumn(
                    CreateTimelineField(TimelineColumnName(timeline.Name, "end"), timeline.DataType, true, timeline.Name, null),
                    CreateNullableInt64Array(chunks.Select(c => TimelineValueOrNull(c.Info, timeline.Name)))));

                foreach (var component in allComponents.Values.OrderBy(c => ComponentColumnName(c, timeline.Name, "start"), StringComparer.Ordinal))
                {
                    var relevant = chunks.Any(c =>
                        !c.Info.IsStatic &&
                        HasTimeline(c.Info, timeline.Name) &&
                        c.Info.Components.Any(component.Matches));
                    if (!relevant)
                        continue;

                    columns.Add(new ManifestColumn(
                        CreateTimelineField(ComponentColumnName(component, timeline.Name, "start"), timeline.DataType, true, timeline.Name, component),
                        CreateNullableInt64Array(chunks.Select(c => ComponentTimelineValueOrNull(c.Info, component, timeline.Name)))));
                    columns.Add(new ManifestColumn(
                        CreateTimelineField(ComponentColumnName(component, timeline.Name, "end"), timeline.DataType, true, timeline.Name, component),
                        CreateNullableInt64Array(chunks.Select(c => ComponentTimelineValueOrNull(c.Info, component, timeline.Name)))));
                    columns.Add(new ManifestColumn(
                        CreateTimelineField(ComponentColumnName(component, timeline.Name, "num_rows"), new UInt64Type(), false, timeline.Name, component),
                        CreateUInt64Array(chunks.Select(c => ComponentNumRows(c.Info, component, timeline.Name)))));
                }
            }

            var schema = new ArrowSchema(
                columns.Select(c => c.Field),
                System.Array.Empty<KeyValuePair<string, string>>());
            var batch = new RecordBatch(schema, columns.Select(c => c.Array), rowCount);
            return SerializeRecordBatchIpc(schema, batch);
        }

        private static Dictionary<string, ComponentKey> CollectComponents(ArrowSchema schema)
        {
            var result = new Dictionary<string, ComponentKey>(StringComparer.Ordinal);
            foreach (var field in schema.FieldsList)
            {
                var metadata = field.Metadata;
                if (metadata == null ||
                    !metadata.TryGetValue("rerun:kind", out var kind) ||
                    kind != "data")
                    continue;

                metadata.TryGetValue("rerun:archetype", out var archetype);
                metadata.TryGetValue("rerun:component", out var component);
                metadata.TryGetValue("rerun:component_type", out var componentType);
                var key = new ComponentKey(
                    archetype ?? "",
                    component ?? field.Name,
                    componentType ?? "");
                result[key.Key] = key;
            }

            return result;
        }

        private static Dictionary<string, RrdManifestTimelineInfo> CollectTimelines(
            ArrowSchema schema,
            IReadOnlyList<ManifestChunkEntry> chunks)
        {
            var result = new Dictionary<string, RrdManifestTimelineInfo>(StringComparer.Ordinal);
            foreach (var chunk in chunks)
            {
                foreach (var timeline in chunk.Info.Timelines)
                    result[timeline.Name] = timeline;
            }

            foreach (var field in schema.FieldsList)
            {
                var metadata = field.Metadata;
                if (metadata == null ||
                    !metadata.TryGetValue("rerun:kind", out var kind) ||
                    kind != "index")
                    continue;

                var name = metadata.TryGetValue("rerun:index_name", out var indexName)
                    ? indexName
                    : field.Name;
                if (result.TryGetValue(name, out var existing))
                {
                    result[name] = new RrdManifestTimelineInfo(
                        name, existing.Value, existing.Kind, field.DataType);
                }
            }

            return result;
        }

        private static long? TimelineValueOrNull(RrdManifestChunkInfo info, string timelineName)
        {
            if (info.IsStatic)
                return null;
            foreach (var timeline in info.Timelines)
            {
                if (timeline.Name == timelineName)
                    return timeline.Value;
            }
            return null;
        }

        private static long? ComponentTimelineValueOrNull(
            RrdManifestChunkInfo info,
            ComponentKey component,
            string timelineName)
        {
            return !info.IsStatic &&
                   HasTimeline(info, timelineName) &&
                   info.Components.Any(component.Matches)
                ? TimelineValueOrNull(info, timelineName)
                : null;
        }

        private static ulong ComponentNumRows(
            RrdManifestChunkInfo info,
            ComponentKey component,
            string timelineName)
        {
            if (info.IsStatic || !HasTimeline(info, timelineName))
                return 0;

            foreach (var candidate in info.Components)
            {
                if (component.Matches(candidate))
                    return candidate.NumRows;
            }
            return 0;
        }

        private static bool HasTimeline(RrdManifestChunkInfo info, string timelineName)
        {
            return info.Timelines.Any(t => t.Name == timelineName);
        }

        private static string TimelineColumnName(string timelineName, string suffix)
        {
            return SanitizeColumnName(timelineName) + ":" + suffix;
        }

        private static string ComponentColumnName(ComponentKey component, string? timelineName, string suffix)
        {
            var pieces = new List<string>();
            if (!string.IsNullOrEmpty(timelineName))
                pieces.Add(timelineName);
            if (!string.IsNullOrEmpty(component.ArchetypeShortName))
                pieces.Add(component.ArchetypeShortName);
            pieces.Add(component.ArchetypeFieldName);
            pieces.Add(suffix);
            return SanitizeColumnName(string.Join(":", pieces));
        }

        private static string SanitizeColumnName(string name)
        {
            return name
                .Replace(',', '_')
                .Replace(' ', '_')
                .Replace('-', '_')
                .Replace('.', '_')
                .Replace('\\', '_')
                .TrimStart('_');
        }

        private static Field CreateComponentField(
            string name,
            IArrowType type,
            bool nullable,
            ComponentKey component,
            string index)
        {
            var metadata = new Dictionary<string, string>
            {
                { "rerun:index", index },
                { "rerun:component", component.Component },
            };
            if (!string.IsNullOrEmpty(component.Archetype))
                metadata["rerun:archetype"] = component.Archetype;
            if (!string.IsNullOrEmpty(component.ComponentType))
                metadata["rerun:component_type"] = component.ComponentType;
            return new Field(name, type, nullable, metadata);
        }

        private static Field CreateTimelineField(
            string name,
            IArrowType type,
            bool nullable,
            string index,
            ComponentKey? component)
        {
            var metadata = new Dictionary<string, string>
            {
                { "rerun:index", index },
            };
            if (component.HasValue)
            {
                var value = component.Value;
                metadata["rerun:component"] = value.Component;
                if (!string.IsNullOrEmpty(value.Archetype))
                    metadata["rerun:archetype"] = value.Archetype;
                if (!string.IsNullOrEmpty(value.ComponentType))
                    metadata["rerun:component_type"] = value.ComponentType;
            }
            return new Field(name, type, nullable, metadata);
        }

        private static byte[] SerializeSchemaIpc(ArrowSchema schema)
        {
            var emptyArrays = schema.FieldsList.Select(f => CreateEmptyArray(f.DataType)).ToList();
            var emptyBatch = new RecordBatch(schema, emptyArrays, 0);
            return SerializeRecordBatchIpc(schema, emptyBatch);
        }

        private static byte[] SerializeRecordBatchIpc(ArrowSchema schema, RecordBatch batch)
        {
            using var ms = new MemoryStream();
            using (var writer = new ArrowStreamWriter(ms, schema, leaveOpen: true))
            {
                writer.WriteRecordBatch(batch);
            }
            return ms.ToArray();
        }

        private static FixedSizeBinaryArray CreateFixedSizeBinaryArray(int width, IEnumerable<byte[]> values)
        {
            var rows = values.ToList();
            var data = new byte[rows.Count * width];
            for (var i = 0; i < rows.Count; i++)
                Buffer.BlockCopy(rows[i], 0, data, i * width, width);

            return new FixedSizeBinaryArray(new ArrayData(
                new FixedSizeBinaryType(width),
                rows.Count,
                0,
                0,
                new[] { ValidityForRows(rows.Count), new ArrowBuffer(new ReadOnlyMemory<byte>(data)) },
                null));
        }

        private static BooleanArray CreateBooleanArray(IEnumerable<bool> values)
        {
            var b = new BooleanArray.Builder();
            foreach (var value in values)
                b.Append(value);
            return b.Build(default);
        }

        private static UInt64Array CreateUInt64Array(IEnumerable<ulong> values)
        {
            var b = new UInt64Array.Builder();
            foreach (var value in values)
                b.Append(value);
            return b.Build(default);
        }

        private static Int64Array CreateNullableInt64Array(IEnumerable<long?> values)
        {
            var b = new Int64Array.Builder();
            foreach (var value in values)
            {
                if (value.HasValue)
                    b.Append(value.Value);
                else
                    b.AppendNull();
            }
            return b.Build(default);
        }

        private static StringArray CreateStringArray(IEnumerable<string> values)
        {
            var b = new StringArray.Builder();
            foreach (var value in values)
                b.Append(value ?? "", System.Text.Encoding.UTF8);
            return b.Build(default);
        }

        private static IArrowArray CreateEmptyArray(IArrowType type)
        {
            var emptyBuffer = new ArrowBuffer(new ReadOnlyMemory<byte>(System.Array.Empty<byte>()));

            if (type is StringType)
            {
                return ArrowArrayFactory.BuildArray(new ArrayData(
                    type,
                    0,
                    0,
                    0,
                    new[] { ValidityForRows(0), CreateOffsetsBuffer(0), emptyBuffer },
                    null));
            }

            if (type is ListType listType)
            {
                var child = CreateEmptyArray(listType.ValueDataType);
                return ArrowArrayFactory.BuildArray(new ArrayData(
                    type,
                    0,
                    0,
                    0,
                    new[] { ValidityForRows(0), CreateOffsetsBuffer(0) },
                    new[] { child.Data }));
            }

            if (type is FixedSizeListType fixedSizeListType)
            {
                var child = CreateEmptyArray(fixedSizeListType.ValueDataType);
                return ArrowArrayFactory.BuildArray(new ArrayData(
                    type,
                    0,
                    0,
                    0,
                    new[] { ValidityForRows(0) },
                    new[] { child.Data }));
            }

            return ArrowArrayFactory.BuildArray(new ArrayData(
                type,
                0,
                0,
                0,
                new[] { ValidityForRows(0), emptyBuffer },
                null));
        }

        private static ArrowBuffer CreateOffsetsBuffer(params int[] offsets)
        {
            var bytes = new byte[offsets.Length * 4];
            for (var i = 0; i < offsets.Length; i++)
                BitConverter.GetBytes(offsets[i]).CopyTo(bytes, i * 4);
            return new ArrowBuffer(new ReadOnlyMemory<byte>(bytes));
        }

        private static ArrowBuffer ValidityForRows(int rowCount)
        {
            var byteCount = (rowCount + 7) / 8;
            var bytes = new byte[byteCount];
            for (var i = 0; i < byteCount; i++)
                bytes[i] = 0xFF;
            if (rowCount % 8 != 0)
                bytes[byteCount - 1] &= (byte)((1 << (rowCount % 8)) - 1);
            return new ArrowBuffer(new ReadOnlyMemory<byte>(bytes));
        }

        private readonly struct ManifestColumn
        {
            public Field Field { get; }
            public IArrowArray Array { get; }

            public ManifestColumn(string name, IArrowType type, bool nullable, IArrowArray array)
                : this(new Field(name, type, nullable), array)
            {
            }

            public ManifestColumn(Field field, IArrowArray array)
            {
                Field = field;
                Array = array;
            }
        }

        private readonly struct ManifestChunkEntry
        {
            public RrdManifestChunkInfo Info { get; }
            public RrdPayloadSpan PayloadSpan { get; }
            public string StoreKey => Info.RecordingId + "\n" + Info.ApplicationId;

            public ManifestChunkEntry(RrdManifestChunkInfo info, RrdPayloadSpan payloadSpan)
            {
                Info = info;
                PayloadSpan = payloadSpan;
            }
        }

        private readonly struct ComponentKey
        {
            public string Archetype { get; }
            public string Component { get; }
            public string ComponentType { get; }
            public string Key => Archetype + "\n" + Component + "\n" + ComponentType;

            public ComponentKey(string archetype, string component, string componentType)
            {
                Archetype = archetype;
                Component = component;
                ComponentType = componentType;
            }

            public bool Matches(RrdManifestComponentInfo component)
            {
                return Archetype == component.Archetype &&
                       Component == component.Component &&
                       ComponentType == component.ComponentType;
            }

            public string ArchetypeShortName
            {
                get
                {
                    if (string.IsNullOrEmpty(Archetype))
                        return "";
                    var index = Archetype.LastIndexOf('.');
                    return index >= 0 ? Archetype.Substring(index + 1) : Archetype;
                }
            }

            public string ArchetypeFieldName
            {
                get
                {
                    var shortName = ArchetypeShortName;
                    var prefix = shortName + ":";
                    if (!string.IsNullOrEmpty(shortName) && Component.StartsWith(prefix, StringComparison.Ordinal))
                        return Component.Substring(prefix.Length);
                    return Component;
                }
            }
        }
    }
}

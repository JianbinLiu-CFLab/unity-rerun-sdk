// SPDX-License-Identifier: Apache-2.0

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using Apache.Arrow;
using Apache.Arrow.Arrays;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using Unity.RerunSDK.Core;

namespace Unity.RerunSDK.Encoding
{
    /// Produces Arrow IPC stream bytes for Rerun archetype chunks.
    internal static class RerunArrowIpcEncoder
    {
        private const int RowCount = 1;

        public static byte[] EncodeTextLogArrowIpc(
            string entityPath, string text, string level,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var fields = new List<Field> { CreateRowIdField() };
            foreach (var t in timelines)
                fields.Add(CreateTimelineFieldFor(t.Kind, t.Name));
            var textType = new ListType(new Field("item", StringType.Default, true));
            fields.Add(CreateComponentField("TextLog:text", "rerun.archetypes.TextLog", "rerun.components.Text", textType));
            var levelType = new ListType(new Field("item", StringType.Default, true));
            fields.Add(CreateComponentField("TextLog:level", "rerun.archetypes.TextLog", "rerun.components.TextLogLevel", levelType));
            var schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            var arrays = new List<IArrowArray> { CreateTuidColumn(rowId) };
            AppendTimelineValues(arrays, timelines);
            arrays.Add(CreateStringComponentColumn(text ?? ""));
            arrays.Add(CreateStringComponentColumn(level ?? "INFO"));
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }

        public static byte[] EncodeScalarArrowIpc(
            string entityPath, double value,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var fields = new List<Field> { CreateRowIdField() };
            foreach (var t in timelines)
                fields.Add(CreateTimelineFieldFor(t.Kind, t.Name));
            fields.Add(CreateComponentField("Scalars:scalars", "rerun.archetypes.Scalars", "rerun.components.Scalar",
                new ListType(new Field("item", DoubleType.Default, true))));
            var schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            var arrays = new List<IArrowArray> { CreateTuidColumn(rowId) };
            AppendTimelineValues(arrays, timelines);
            arrays.Add(CreateDoubleComponentColumn(value));
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }

        public static byte[] EncodeTransform3DArrowIpc(
            string entityPath,
            float tx, float ty, float tz,
            float qx, float qy, float qz, float qw,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var fields = new List<Field> { CreateRowIdField() };
            foreach (var t in timelines)
                fields.Add(CreateTimelineFieldFor(t.Kind, t.Name));
            var transInner = new FixedSizeListType(new Field("item", FloatType.Default, false), 3);
            fields.Add(CreateComponentField("Transform3D:translation", "rerun.archetypes.Transform3D", "rerun.components.Translation3D",
                new ListType(new Field("item", transInner, true))));
            var quatInner = new FixedSizeListType(new Field("item", FloatType.Default, false), 4);
            fields.Add(CreateComponentField("Transform3D:quaternion", "rerun.archetypes.Transform3D", "rerun.components.RotationQuat",
                new ListType(new Field("item", quatInner, true))));
            var schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            var arrays = new List<IArrowArray> { CreateTuidColumn(rowId) };
            AppendTimelineValues(arrays, timelines);
            arrays.Add(CreateFloatVectorComponentColumn(tx, ty, tz));
            arrays.Add(CreateFloatVectorComponentColumn(qx, qy, qz, qw));
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }

        public static byte[] EncodeViewCoordinatesArrowIpc(
            string entityPath, byte x, byte y, byte z,
            RerunTuid rowId, RerunTuid chunkId)
        {
            var innerFsl = new FixedSizeListType(new Field("item", new UInt8Type(), false), 3);
            var fields = new Field[] {
                CreateRowIdField(),
                CreateComponentField("ViewCoordinates:xyz", "rerun.archetypes.ViewCoordinates", "rerun.components.ViewCoordinates",
                    new ListType(new Field("item", innerFsl, true)), isStatic: true),
            };
            var schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            IArrowArray[] arrays = {
                CreateTuidColumn(rowId),
                CreateByteVectorComponentColumn(x, y, z),
            };
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }

        // ── field builders ──

        internal static Field CreateRowIdField()
        {
            var meta = new Dictionary<string, string> {
                { "rerun:kind", "control" },
                { "ARROW:extension:name", "rerun.datatypes.TUID" },
                { "ARROW:extension:metadata", @"{""namespace"":""row""}" }
            };
            return new Field("row_id", new FixedSizeBinaryType(16), false, meta);
        }

        internal static Field CreateTimelineFieldFor(RerunTimelineKind kind, string name)
        {
            var meta = new Dictionary<string, string> {
                { "rerun:kind", "index" },
                { "rerun:index_name", name }
            };
            IArrowType type = kind switch
            {
                RerunTimelineKind.TimestampNs => new TimestampType(TimeUnit.Nanosecond, (string?)null),
                RerunTimelineKind.DurationNs => DurationType.Nanosecond,
                _ => Int64Type.Default,
            };
            return new Field(name, type, false, meta);
        }

        internal static Field CreateComponentField(string name, string archetype, string compType,
            IArrowType fieldType, bool isStatic = false)
        {
            var meta = new Dictionary<string, string> {
                { "rerun:kind", "data" },
                { "rerun:component", name },
                { "rerun:archetype", archetype },
                { "rerun:component_type", compType }
            };
            if (isStatic) meta["rerun:is_static"] = "true";
            return new Field(name, fieldType, true, meta);
        }

        internal static Dictionary<string, string> MakeBatchMetadata(string entityPath, RerunTuid chunkId)
        {
            return new() {
                { "sorbet:version", "0.1.3" },
                { "rerun:id", RerunTuidGenerator.ToHexString(chunkId) },
                { "rerun:entity_path", entityPath }
            };
        }

        // ── column builders ──

        internal static FixedSizeBinaryArray CreateTuidColumn(RerunTuid tuid)
        {
            return CreateFixedSizeBinaryColumn(16, RerunTuidGenerator.ToBytes(tuid));
        }

        private static FixedSizeBinaryArray CreateFixedSizeBinaryColumn(int width, byte[] data)
        {
            return new FixedSizeBinaryArray(new ArrayData(
                new FixedSizeBinaryType(width), 1, 0, 0,
                new[] { ValidityForRows(1), new ArrowBuffer(new ReadOnlyMemory<byte>(data)) },
                null));
        }

        private static void AppendTimelineValues(List<IArrowArray> arrays,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            foreach (var t in timelines)
            {
                var b = new Int64Array.Builder();
                b.Append(t.Value);
                arrays.Add(b.Build(default));
            }
        }

        private static ListArray CreateStringComponentColumn(string value)
        {
            var b = new StringArray.Builder();
            b.Append(value, System.Text.Encoding.UTF8);
            return WrapComponentInstancesInList(1, b.Build(default));
        }

        private static ListArray CreateDoubleComponentColumn(double value)
        {
            var b = new DoubleArray.Builder();
            b.Append(value);
            return WrapComponentInstancesInList(1, b.Build(default));
        }

        private static ListArray CreateFloatVectorComponentColumn(params float[] values)
        {
            var b = new FloatArray.Builder();
            foreach (var v in values) b.Append(v);
            var arr = b.Build(default);
            var fsl = WrapInFixedSizeList(arr, values.Length);
            return WrapComponentInstancesInList(1, fsl);
        }

        private static ListArray CreateByteVectorComponentColumn(params byte[] values)
        {
            var b = new UInt8Array.Builder();
            foreach (var v in values) b.Append(v);
            var arr = b.Build(default);
            var fsl = WrapInFixedSizeList(arr, values.Length);
            return WrapComponentInstancesInList(1, fsl);
        }

        private static IArrowArray WrapInFixedSizeList(IArrowArray inner, int size)
        {
            var fslType = new FixedSizeListType(
                new Field("item", inner.Data.DataType, false), size);
            return ArrowArrayFactory.BuildArray(
                new ArrayData(fslType, 1, 0, 0,
                    new[] { ValidityForRows(1) },
                    new[] { inner.Data }));
        }

        private static ListArray WrapComponentInstancesInList(int componentCount, IArrowArray innerArray)
        {
            // Two int32 offsets in LE: [0, componentCount]
            var offsets = new byte[8];
            offsets[4] = (byte)(componentCount & 0xFF);
            offsets[5] = (byte)((componentCount >> 8) & 0xFF);
            offsets[6] = (byte)((componentCount >> 16) & 0xFF);
            offsets[7] = (byte)((componentCount >> 24) & 0xFF);

            var offBuf = new ArrowBuffer(new ReadOnlyMemory<byte>(offsets));
            var listType = new ListType(new Field("item", innerArray.Data.DataType, true));
            return new ListArray(new ArrayData(listType, RowCount, 0, 0,
                new[] { ValidityForRows(RowCount), offBuf },
                new[] { innerArray.Data }));
        }

        private static ArrowBuffer ValidityForRows(int rowCount)
        {
            var byteCount = (rowCount + 7) / 8;
            var bytes = new byte[byteCount];
            for (int i = 0; i < byteCount; i++) bytes[i] = 0xFF;
            if (rowCount % 8 != 0)
                bytes[byteCount - 1] &= (byte)((1 << (rowCount % 8)) - 1);
            return new ArrowBuffer(new ReadOnlyMemory<byte>(bytes));
        }

        private static byte[] SerializeArrowIpc(Schema schema, RecordBatch batch)
        {
            using var ms = new MemoryStream();
            using (var writer = new ArrowStreamWriter(ms, schema, leaveOpen: true))
            {
                writer.WriteRecordBatch(batch);
            }
            return ms.ToArray();
        }
    }
}

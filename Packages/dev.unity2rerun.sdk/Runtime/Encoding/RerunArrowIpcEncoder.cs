// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Encoding
// Purpose: Defines managed Rerun encoding primitives used by RRD files and live transport.

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
        /// <summary>Unity2Rerun currently writes each encoded component batch as a single Rerun row.</summary>
        private const int RowCount = 1;
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public static byte[] EncodeTextLogArrowIpc(
            string entityPath, string text, string level,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            return EncodeTextLogArrowIpc(entityPath, text, level, rowId, chunkId, timelines, out _);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        internal static byte[] EncodeTextLogArrowIpc(
            string entityPath, string text, string level,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines,
            out Schema schema)
        {
            var fields = new List<Field> { CreateRowIdField() };
            foreach (var t in timelines)
                fields.Add(CreateTimelineFieldFor(t.Kind, t.Name));
            var textType = new ListType(new Field("item", StringType.Default, true));
            fields.Add(CreateComponentField("TextLog:text", "rerun.archetypes.TextLog", "rerun.components.Text", textType));
            var levelType = new ListType(new Field("item", StringType.Default, true));
            fields.Add(CreateComponentField("TextLog:level", "rerun.archetypes.TextLog", "rerun.components.TextLogLevel", levelType));
            schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            var arrays = new List<IArrowArray> { CreateTuidColumn(rowId) };
            AppendTimelineValues(arrays, timelines);
            arrays.Add(CreateStringComponentColumn(text ?? ""));
            arrays.Add(CreateStringComponentColumn(level ?? "INFO"));
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public static byte[] EncodeScalarArrowIpc(
            string entityPath, double value,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            return EncodeScalarArrowIpc(entityPath, value, rowId, chunkId, timelines, out _);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        internal static byte[] EncodeScalarArrowIpc(
            string entityPath, double value,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines,
            out Schema schema)
        {
            var fields = new List<Field> { CreateRowIdField() };
            foreach (var t in timelines)
                fields.Add(CreateTimelineFieldFor(t.Kind, t.Name));
            fields.Add(CreateComponentField("Scalars:scalars", "rerun.archetypes.Scalars", "rerun.components.Scalar",
                new ListType(new Field("item", DoubleType.Default, true))));
            schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            var arrays = new List<IArrowArray> { CreateTuidColumn(rowId) };
            AppendTimelineValues(arrays, timelines);
            arrays.Add(CreateDoubleComponentColumn(value));
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public static byte[] EncodeTransform3DArrowIpc(
            string entityPath,
            float tx, float ty, float tz,
            float qx, float qy, float qz, float qw,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            return EncodeTransform3DArrowIpc(entityPath, tx, ty, tz, qx, qy, qz, qw, rowId, chunkId, timelines, out _);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        internal static byte[] EncodeTransform3DArrowIpc(
            string entityPath,
            float tx, float ty, float tz,
            float qx, float qy, float qz, float qw,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines,
            out Schema schema)
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
            schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            var arrays = new List<IArrowArray> { CreateTuidColumn(rowId) };
            AppendTimelineValues(arrays, timelines);
            arrays.Add(CreateFloatVectorComponentColumn(tx, ty, tz));
            arrays.Add(CreateFloatVectorComponentColumn(qx, qy, qz, qw));
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public static byte[] EncodeViewCoordinatesArrowIpc(
            string entityPath, byte x, byte y, byte z,
            RerunTuid rowId, RerunTuid chunkId)
        {
            return EncodeViewCoordinatesArrowIpc(entityPath, x, y, z, rowId, chunkId, out _);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        internal static byte[] EncodeViewCoordinatesArrowIpc(
            string entityPath, byte x, byte y, byte z,
            RerunTuid rowId, RerunTuid chunkId,
            out Schema schema)
        {
            var innerFsl = new FixedSizeListType(new Field("item", new UInt8Type(), false), 3);
            var fields = new Field[] {
                CreateRowIdField(),
                CreateComponentField("ViewCoordinates:xyz", "rerun.archetypes.ViewCoordinates", "rerun.components.ViewCoordinates",
                    new ListType(new Field("item", innerFsl, true)), isStatic: true),
            };
            schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            IArrowArray[] arrays = {
                CreateTuidColumn(rowId),
                CreateByteVectorComponentColumn(x, y, z),
            };
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public static byte[] EncodePinholeArrowIpc(
            string entityPath, RerunPinhole pinhole,
            RerunTuid rowId, RerunTuid chunkId)
        {
            return EncodePinholeArrowIpc(entityPath, pinhole, rowId, chunkId, out _);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        internal static byte[] EncodePinholeArrowIpc(
            string entityPath, RerunPinhole pinhole,
            RerunTuid rowId, RerunTuid chunkId,
            out Schema schema)
        {
            var matrixInner = new FixedSizeListType(new Field("item", FloatType.Default, false), 9);
            var resolutionInner = new FixedSizeListType(new Field("item", FloatType.Default, false), 2);
            var cameraXyzInner = new FixedSizeListType(new Field("item", new UInt8Type(), false), 3);

            var fields = new Field[] {
                CreateRowIdField(),
                CreateComponentField("Pinhole:image_from_camera", "rerun.archetypes.Pinhole", "rerun.components.PinholeProjection",
                    new ListType(new Field("item", matrixInner, true)), isStatic: true),
                CreateComponentField("Pinhole:resolution", "rerun.archetypes.Pinhole", "rerun.components.Resolution",
                    new ListType(new Field("item", resolutionInner, true)), isStatic: true),
                CreateComponentField("Pinhole:camera_xyz", "rerun.archetypes.Pinhole", "rerun.components.ViewCoordinates",
                    new ListType(new Field("item", cameraXyzInner, true)), isStatic: true),
                CreateComponentField("Pinhole:image_plane_distance", "rerun.archetypes.Pinhole", "rerun.components.ImagePlaneDistance",
                    new ListType(new Field("item", FloatType.Default, true)), isStatic: true),
                CreateComponentField("Pinhole:color", "rerun.archetypes.Pinhole", "rerun.components.Color",
                    new ListType(new Field("item", new UInt32Type(), true)), isStatic: true),
                CreateComponentField("Pinhole:line_width", "rerun.archetypes.Pinhole", "rerun.components.Radius",
                    new ListType(new Field("item", FloatType.Default, true)), isStatic: true),
            };
            schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            IArrowArray[] arrays = {
                CreateTuidColumn(rowId),
                CreateFloatVectorComponentColumn(
                    pinhole.Fx, 0f, 0f,
                    0f, pinhole.Fy, 0f,
                    pinhole.Cx, pinhole.Cy, 1f),
                CreateFloatVectorComponentColumn(pinhole.Width, pinhole.Height),
                CreateByteVectorComponentColumn(
                    RerunPinhole.CameraXyzRight,
                    RerunPinhole.CameraXyzDown,
                    RerunPinhole.CameraXyzForward),
                CreateFloatComponentColumn(pinhole.ImagePlaneDistance),
                CreateUInt32ComponentColumn(pinhole.ColorRgba),
                CreateFloatComponentColumn(pinhole.LineWidth),
            };
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public static byte[] EncodeEncodedImageArrowIpc(
            string entityPath, byte[] encodedBytes, string mediaType,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            return EncodeEncodedImageArrowIpc(entityPath, encodedBytes, mediaType, rowId, chunkId, timelines, out _);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        internal static byte[] EncodeEncodedImageArrowIpc(
            string entityPath, byte[] encodedBytes, string mediaType,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines,
            out Schema schema)
        {
            var fields = new List<Field> { CreateRowIdField() };
            foreach (var t in timelines)
                fields.Add(CreateTimelineFieldFor(t.Kind, t.Name));
            var blobValueType = new ListType(new Field("item", new UInt8Type(), false));
            fields.Add(CreateComponentField("EncodedImage:blob", "rerun.archetypes.EncodedImage", "rerun.components.Blob",
                new ListType(new Field("item", blobValueType, true))));
            fields.Add(CreateComponentField("EncodedImage:media_type", "rerun.archetypes.EncodedImage", "rerun.components.MediaType",
                new ListType(new Field("item", StringType.Default, true))));
            schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            var arrays = new List<IArrowArray> { CreateTuidColumn(rowId) };
            AppendTimelineValues(arrays, timelines);
            arrays.Add(CreateBlobComponentColumn(encodedBytes ?? System.Array.Empty<byte>()));
            arrays.Add(CreateStringComponentColumn(mediaType ?? ""));
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public static byte[] EncodeBoxes3DArrowIpc(
            string entityPath, IReadOnlyList<RerunBox3D> boxes,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            return EncodeBoxes3DArrowIpc(entityPath, boxes, rowId, chunkId, timelines, out _);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        internal static byte[] EncodeBoxes3DArrowIpc(
            string entityPath, IReadOnlyList<RerunBox3D> boxes,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines,
            out Schema schema)
        {
            var fields = new List<Field> { CreateRowIdField() };
            foreach (var t in timelines)
                fields.Add(CreateTimelineFieldFor(t.Kind, t.Name));

            var vec3Inner = new FixedSizeListType(new Field("item", FloatType.Default, false), 3);
            var quatInner = new FixedSizeListType(new Field("item", FloatType.Default, false), 4);
            fields.Add(CreateComponentField("Boxes3D:half_sizes", "rerun.archetypes.Boxes3D", "rerun.components.HalfSize3D",
                new ListType(new Field("item", vec3Inner, true))));
            fields.Add(CreateComponentField("Boxes3D:centers", "rerun.archetypes.Boxes3D", "rerun.components.Translation3D",
                new ListType(new Field("item", vec3Inner, true))));
            fields.Add(CreateComponentField("Boxes3D:quaternions", "rerun.archetypes.Boxes3D", "rerun.components.RotationQuat",
                new ListType(new Field("item", quatInner, true))));
            fields.Add(CreateComponentField("Boxes3D:colors", "rerun.archetypes.Boxes3D", "rerun.components.Color",
                new ListType(new Field("item", new UInt32Type(), true))));
            schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            var arrays = new List<IArrowArray> { CreateTuidColumn(rowId) };
            AppendTimelineValues(arrays, timelines);
            arrays.Add(CreateVec3ComponentColumn(boxes, b => b.HalfSize));
            arrays.Add(CreateVec3ComponentColumn(boxes, b => b.Center));
            arrays.Add(CreateQuatComponentColumn(boxes));
            arrays.Add(CreateUInt32ComponentColumn(boxes, b => b.ColorRgba));
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public static byte[] EncodeLineStrips3DArrowIpc(
            string entityPath, IReadOnlyList<RerunLineStrip3D> strips,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            return EncodeLineStrips3DArrowIpc(entityPath, strips, rowId, chunkId, timelines, out _);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        internal static byte[] EncodeLineStrips3DArrowIpc(
            string entityPath, IReadOnlyList<RerunLineStrip3D> strips,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines,
            out Schema schema)
        {
            var fields = new List<Field> { CreateRowIdField() };
            foreach (var t in timelines)
                fields.Add(CreateTimelineFieldFor(t.Kind, t.Name));

            var vec3Inner = new FixedSizeListType(new Field("item", FloatType.Default, false), 3);
            var lineStripValueType = new ListType(new Field("item", vec3Inner, false));
            fields.Add(CreateComponentField("LineStrips3D:strips", "rerun.archetypes.LineStrips3D", "rerun.components.LineStrip3D",
                new ListType(new Field("item", lineStripValueType, true))));
            fields.Add(CreateComponentField("LineStrips3D:colors", "rerun.archetypes.LineStrips3D", "rerun.components.Color",
                new ListType(new Field("item", new UInt32Type(), true))));
            schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            var arrays = new List<IArrowArray> { CreateTuidColumn(rowId) };
            AppendTimelineValues(arrays, timelines);
            arrays.Add(CreateLineStripPointsColumn(strips));
            arrays.Add(CreateUInt32ComponentColumn(strips, s => s.ColorRgba));
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public static byte[] EncodePoints3DArrowIpc(
            string entityPath, IReadOnlyList<RerunPoint3D> points,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            return EncodePoints3DArrowIpc(entityPath, points, rowId, chunkId, timelines, out _);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        internal static byte[] EncodePoints3DArrowIpc(
            string entityPath, IReadOnlyList<RerunPoint3D> points,
            RerunTuid rowId, RerunTuid chunkId,
            IReadOnlyList<RerunTimelineEntry> timelines,
            out Schema schema)
        {
            var fields = new List<Field> { CreateRowIdField() };
            foreach (var t in timelines)
                fields.Add(CreateTimelineFieldFor(t.Kind, t.Name));

            var vec3Inner = new FixedSizeListType(new Field("item", FloatType.Default, false), 3);
            fields.Add(CreateComponentField("Points3D:positions", "rerun.archetypes.Points3D", "rerun.components.Position3D",
                new ListType(new Field("item", vec3Inner, true))));
            fields.Add(CreateComponentField("Points3D:colors", "rerun.archetypes.Points3D", "rerun.components.Color",
                new ListType(new Field("item", new UInt32Type(), true))));
            fields.Add(CreateComponentField("Points3D:radii", "rerun.archetypes.Points3D", "rerun.components.Radius",
                new ListType(new Field("item", FloatType.Default, true))));
            schema = new Schema(fields, MakeBatchMetadata(entityPath, chunkId));

            var arrays = new List<IArrowArray> { CreateTuidColumn(rowId) };
            AppendTimelineValues(arrays, timelines);
            arrays.Add(CreateVec3ComponentColumn(points, p => p.Position));
            arrays.Add(CreateUInt32ComponentColumn(points, p => p.ColorRgba));
            arrays.Add(CreateFloatComponentColumn(points, p => p.Radius));
            return SerializeArrowIpc(schema, new RecordBatch(schema, arrays, RowCount));
        }

        // -- field builders --
        /// <summary>
        /// Creates the requested SDK helper object.
        /// </summary>
        internal static Field CreateRowIdField()
        {
            var meta = new Dictionary<string, string> {
                { "rerun:kind", "control" },
                { "ARROW:extension:name", "rerun.datatypes.TUID" },
                { "ARROW:extension:metadata", @"{""namespace"":""row""}" }
            };
            return new Field("row_id", new FixedSizeBinaryType(16), false, meta);
        }
        /// <summary>
        /// Creates the requested SDK helper object.
        /// </summary>
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
        /// <summary>
        /// Creates the requested SDK helper object.
        /// </summary>
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

        // -- column builders --
        /// <summary>
        /// Creates the requested SDK helper object.
        /// </summary>
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

        private static ListArray CreateBlobComponentColumn(IReadOnlyList<byte> values)
        {
            var b = new UInt8Array.Builder();
            foreach (var v in values)
                b.Append(v);
            var blob = WrapInList(new[] { 0, values.Count }, b.Build(default), false);
            return WrapComponentInstancesInList(1, blob);
        }

        private static ListArray CreateDoubleComponentColumn(double value)
        {
            var b = new DoubleArray.Builder();
            b.Append(value);
            return WrapComponentInstancesInList(1, b.Build(default));
        }

        private static ListArray CreateFloatComponentColumn(float value)
        {
            var b = new FloatArray.Builder();
            b.Append(value);
            return WrapComponentInstancesInList(1, b.Build(default));
        }

        private static ListArray CreateUInt32ComponentColumn(uint value)
        {
            var b = new UInt32Array.Builder();
            b.Append(value);
            return WrapComponentInstancesInList(1, b.Build(default));
        }

        private static ListArray CreateFloatVectorComponentColumn(params float[] values)
        {
            var b = new FloatArray.Builder();
            foreach (var v in values) b.Append(v);
            var arr = b.Build(default);
            var fsl = WrapInFixedSizeList(arr, values.Length, 1);
            return WrapComponentInstancesInList(1, fsl);
        }

        private static ListArray CreateByteVectorComponentColumn(params byte[] values)
        {
            var b = new UInt8Array.Builder();
            foreach (var v in values) b.Append(v);
            var arr = b.Build(default);
            var fsl = WrapInFixedSizeList(arr, values.Length, 1);
            return WrapComponentInstancesInList(1, fsl);
        }

        private static ListArray CreateVec3ComponentColumn<T>(IReadOnlyList<T> values, Func<T, RerunVec3> selector)
        {
            var b = new FloatArray.Builder();
            for (var i = 0; i < values.Count; i++)
            {
                var v = selector(values[i]);
                b.Append(v.X);
                b.Append(v.Y);
                b.Append(v.Z);
            }

            var arr = b.Build(default);
            var fsl = WrapInFixedSizeList(arr, 3, values.Count);
            return WrapComponentInstancesInList(values.Count, fsl);
        }

        private static ListArray CreateQuatComponentColumn(IReadOnlyList<RerunBox3D> boxes)
        {
            var b = new FloatArray.Builder();
            for (var i = 0; i < boxes.Count; i++)
            {
                var q = boxes[i].Rotation;
                b.Append(q.X);
                b.Append(q.Y);
                b.Append(q.Z);
                b.Append(q.W);
            }

            var arr = b.Build(default);
            var fsl = WrapInFixedSizeList(arr, 4, boxes.Count);
            return WrapComponentInstancesInList(boxes.Count, fsl);
        }

        private static ListArray CreateUInt32ComponentColumn<T>(IReadOnlyList<T> values, Func<T, uint> selector)
        {
            var b = new UInt32Array.Builder();
            for (var i = 0; i < values.Count; i++)
                b.Append(selector(values[i]));
            return WrapComponentInstancesInList(values.Count, b.Build(default));
        }

        private static ListArray CreateFloatComponentColumn<T>(IReadOnlyList<T> values, Func<T, float> selector)
        {
            var b = new FloatArray.Builder();
            for (var i = 0; i < values.Count; i++)
                b.Append(selector(values[i]));
            return WrapComponentInstancesInList(values.Count, b.Build(default));
        }

        private static ListArray CreateLineStripPointsColumn(IReadOnlyList<RerunLineStrip3D> strips)
        {
            var b = new FloatArray.Builder();
            var pointCount = 0;
            for (var i = 0; i < strips.Count; i++)
            {
                var points = strips[i].Points;
                for (var j = 0; j < points.Count; j++)
                {
                    b.Append(points[j].X);
                    b.Append(points[j].Y);
                    b.Append(points[j].Z);
                    pointCount++;
                }
            }

            var arr = b.Build(default);
            var fsl = WrapInFixedSizeList(arr, 3, pointCount);
            var offsets = new int[strips.Count + 1];
            pointCount = 0;
            for (var i = 0; i < strips.Count; i++)
            {
                pointCount += strips[i].Points.Count;
                offsets[i + 1] = pointCount;
            }
            var lineStrips = WrapInList(offsets, fsl, false);
            return WrapComponentInstancesInList(strips.Count, lineStrips);
        }

        private static IArrowArray WrapInFixedSizeList(IArrowArray inner, int size, int listCount)
        {
            var fslType = new FixedSizeListType(
                new Field("item", inner.Data.DataType, false), size);
            return ArrowArrayFactory.BuildArray(
                new ArrayData(fslType, listCount, 0, 0,
                    new[] { ValidityForRows(listCount) },
                    new[] { inner.Data }));
        }

        private static ListArray WrapComponentInstancesInList(int componentCount, IArrowArray innerArray)
        {
            return WrapInList(new[] { 0, componentCount }, innerArray, true);
        }

        private static ListArray WrapInList(IReadOnlyList<int> offsets, IArrowArray innerArray, bool innerNullable)
        {
            var offsetBytes = new byte[offsets.Count * 4];
            for (var i = 0; i < offsets.Count; i++)
            {
                var value = offsets[i];
                var offset = i * 4;
                offsetBytes[offset] = (byte)(value & 0xFF);
                offsetBytes[offset + 1] = (byte)((value >> 8) & 0xFF);
                offsetBytes[offset + 2] = (byte)((value >> 16) & 0xFF);
                offsetBytes[offset + 3] = (byte)((value >> 24) & 0xFF);
            }

            var rowCount = offsets.Count - 1;
            var offBuf = new ArrowBuffer(new ReadOnlyMemory<byte>(offsetBytes));
            var listType = new ListType(new Field("item", innerArray.Data.DataType, innerNullable));
            return new ListArray(new ArrayData(listType, rowCount, 0, 0,
                new[] { ValidityForRows(rowCount), offBuf },
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

// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Encoding
// Purpose: Defines managed Rerun encoding primitives used by RRD files and live transport.

using System;
using System.Collections.Generic;
using Unity.RerunSDK.Core;
using static Unity.RerunSDK.IO.Rrd.RrdConstants;

namespace Unity.RerunSDK.Encoding
{
    /// <summary>
    /// Provides Managed Rerun Encoder support for Unity2Rerun.
    /// </summary>
    internal class ManagedRerunEncoder : IRerunEncoder
    {
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public EncodedRerunMessage EncodeSetStoreInfoMessage(string recordingId, string applicationId)
        {
            var nowNs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
            var rrdPayload = RerunProtobufEncoding.EncodeSetStoreInfo(recordingId, applicationId, nowNs, 1);
            var grpcPayload = RerunProtobufEncoding.WrapSetStoreInfoAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(
                MsgKindSetStoreInfo, rrdPayload, grpcPayload,
                isStoreInfo: true, isStatic: false);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public EncodedRerunMessage EncodeTextLogMessage(
            string recordingId, string applicationId,
            string entityPath, string text, string level,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodeTextLogArrowIpc(
                entityPath, text, level, rowId, chunkId, timelines, out var schema);
            var manifestInfo = CreateManifestInfo(
                recordingId, applicationId, entityPath, chunkId, isStatic: false,
                arrowIpc, schema, timelines);

            var rrdPayload = RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkId.TimeNs, chunkId.Inc,
                compression: 1, (ulong)arrowIpc.Length, arrowIpc);

            var grpcPayload = RerunProtobufEncoding.WrapArrowMsgAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(MsgKindArrowMsg, rrdPayload, grpcPayload,
                isStoreInfo: false, isStatic: false, manifestChunkInfo: manifestInfo);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public EncodedRerunMessage EncodeScalarMessage(
            string recordingId, string applicationId,
            string entityPath, double value,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodeScalarArrowIpc(
                entityPath, value, rowId, chunkId, timelines, out var schema);
            var manifestInfo = CreateManifestInfo(
                recordingId, applicationId, entityPath, chunkId, isStatic: false,
                arrowIpc, schema, timelines);

            var rrdPayload = RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkId.TimeNs, chunkId.Inc,
                compression: 1, (ulong)arrowIpc.Length, arrowIpc);

            var grpcPayload = RerunProtobufEncoding.WrapArrowMsgAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(MsgKindArrowMsg, rrdPayload, grpcPayload,
                isStoreInfo: false, isStatic: false, manifestChunkInfo: manifestInfo);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public EncodedRerunMessage EncodeTransform3DMessage(
            string recordingId, string applicationId,
            string entityPath,
            float tx, float ty, float tz,
            float qx, float qy, float qz, float qw,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodeTransform3DArrowIpc(
                entityPath, tx, ty, tz, qx, qy, qz, qw,
                rowId, chunkId, timelines, out var schema);
            var manifestInfo = CreateManifestInfo(
                recordingId, applicationId, entityPath, chunkId, isStatic: false,
                arrowIpc, schema, timelines);

            var rrdPayload = RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkId.TimeNs, chunkId.Inc,
                compression: 1, (ulong)arrowIpc.Length, arrowIpc);

            var grpcPayload = RerunProtobufEncoding.WrapArrowMsgAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(MsgKindArrowMsg, rrdPayload, grpcPayload,
                isStoreInfo: false, isStatic: false, manifestChunkInfo: manifestInfo);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public EncodedRerunMessage EncodeViewCoordinatesMessage(
            string recordingId, string applicationId,
            string entityPath, byte x, byte y, byte z)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodeViewCoordinatesArrowIpc(
                entityPath, x, y, z, rowId, chunkId, out var schema);
            var manifestInfo = CreateManifestInfo(
                recordingId, applicationId, entityPath, chunkId, isStatic: true,
                arrowIpc, schema, Array.Empty<RerunTimelineEntry>());

            var rrdPayload = RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkId.TimeNs, chunkId.Inc,
                compression: 1, (ulong)arrowIpc.Length, arrowIpc,
                isStatic: true);

            var grpcPayload = RerunProtobufEncoding.WrapArrowMsgAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(MsgKindArrowMsg, rrdPayload, grpcPayload,
                isStoreInfo: false, isStatic: true, manifestChunkInfo: manifestInfo);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public EncodedRerunMessage EncodePinholeMessage(
            string recordingId, string applicationId,
            string entityPath, RerunPinhole pinhole)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodePinholeArrowIpc(
                entityPath, pinhole, rowId, chunkId, out var schema);
            var manifestInfo = CreateManifestInfo(
                recordingId, applicationId, entityPath, chunkId, isStatic: true,
                arrowIpc, schema, Array.Empty<RerunTimelineEntry>());

            return EncodeArrowMessage(recordingId, applicationId, chunkId, arrowIpc,
                isStatic: true, manifestInfo: manifestInfo);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public EncodedRerunMessage EncodeEncodedImageMessage(
            string recordingId, string applicationId,
            string entityPath, byte[] encodedBytes, string mediaType,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodeEncodedImageArrowIpc(
                entityPath, encodedBytes, mediaType, rowId, chunkId, timelines, out var schema);
            var manifestInfo = CreateManifestInfo(
                recordingId, applicationId, entityPath, chunkId, isStatic: false,
                arrowIpc, schema, timelines);

            return EncodeArrowMessage(recordingId, applicationId, chunkId, arrowIpc,
                isStatic: false, manifestInfo: manifestInfo);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public EncodedRerunMessage EncodeBoxes3DMessage(
            string recordingId, string applicationId,
            string entityPath, IReadOnlyList<RerunBox3D> boxes,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodeBoxes3DArrowIpc(
                entityPath, boxes, rowId, chunkId, timelines, out var schema);
            var manifestInfo = CreateManifestInfo(
                recordingId, applicationId, entityPath, chunkId, isStatic: false,
                arrowIpc, schema, timelines);

            return EncodeArrowMessage(recordingId, applicationId, chunkId, arrowIpc,
                isStatic: false, manifestInfo: manifestInfo);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public EncodedRerunMessage EncodeLineStrips3DMessage(
            string recordingId, string applicationId,
            string entityPath, IReadOnlyList<RerunLineStrip3D> strips,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodeLineStrips3DArrowIpc(
                entityPath, strips, rowId, chunkId, timelines, out var schema);
            var manifestInfo = CreateManifestInfo(
                recordingId, applicationId, entityPath, chunkId, isStatic: false,
                arrowIpc, schema, timelines);

            return EncodeArrowMessage(recordingId, applicationId, chunkId, arrowIpc,
                isStatic: false, manifestInfo: manifestInfo);
        }
        /// <summary>
        /// Encodes the requested Rerun data into the managed transport representation.
        /// </summary>
        public EncodedRerunMessage EncodePoints3DMessage(
            string recordingId, string applicationId,
            string entityPath, IReadOnlyList<RerunPoint3D> points,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodePoints3DArrowIpc(
                entityPath, points, rowId, chunkId, timelines, out var schema);
            var manifestInfo = CreateManifestInfo(
                recordingId, applicationId, entityPath, chunkId, isStatic: false,
                arrowIpc, schema, timelines);

            return EncodeArrowMessage(recordingId, applicationId, chunkId, arrowIpc,
                isStatic: false, manifestInfo: manifestInfo);
        }

        private EncodedRerunMessage EncodeArrowMessage(
            string recordingId, string applicationId,
            RerunTuid chunkId, byte[] arrowIpc, bool isStatic,
            RrdManifestChunkInfo manifestInfo)
        {
            var rrdPayload = RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkId.TimeNs, chunkId.Inc,
                compression: 1, (ulong)arrowIpc.Length, arrowIpc,
                isStatic: isStatic);

            var grpcPayload = RerunProtobufEncoding.WrapArrowMsgAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(MsgKindArrowMsg, rrdPayload, grpcPayload,
                isStoreInfo: false, isStatic: isStatic, manifestChunkInfo: manifestInfo);
        }

        private static RrdManifestChunkInfo CreateManifestInfo(
            string recordingId, string applicationId,
            string entityPath, RerunTuid chunkId, bool isStatic,
            byte[] arrowIpc, Apache.Arrow.Schema schema,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            return RrdManifestChunkInfo.FromSchema(
                recordingId,
                applicationId,
                entityPath,
                chunkId,
                isStatic,
                rowCount: 1,
                uncompressedSize: (ulong)arrowIpc.Length,
                schema,
                timelines);
        }
    }
}

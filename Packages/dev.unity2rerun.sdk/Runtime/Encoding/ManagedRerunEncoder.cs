// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.RerunSDK.Core;
using static Unity.RerunSDK.IO.Rrd.RrdConstants;

namespace Unity.RerunSDK.Encoding
{
    internal class ManagedRerunEncoder : IRerunEncoder
    {
        public EncodedRerunMessage EncodeSetStoreInfoMessage(string recordingId, string applicationId)
        {
            var nowNs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
            var rrdPayload = RerunProtobufEncoding.EncodeSetStoreInfo(recordingId, applicationId, nowNs, 1);
            var grpcPayload = RerunProtobufEncoding.WrapSetStoreInfoAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(
                MsgKindSetStoreInfo, rrdPayload, grpcPayload,
                isStoreInfo: true, isStatic: false);
        }

        public EncodedRerunMessage EncodeTextLogMessage(
            string recordingId, string applicationId,
            string entityPath, string text, string level,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodeTextLogArrowIpc(
                entityPath, text, level, rowId, chunkId, timelines);

            var rrdPayload = RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkId.TimeNs, chunkId.Inc,
                compression: 1, (ulong)arrowIpc.Length, arrowIpc);

            var grpcPayload = RerunProtobufEncoding.WrapArrowMsgAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(MsgKindArrowMsg, rrdPayload, grpcPayload,
                isStoreInfo: false, isStatic: false);
        }

        public EncodedRerunMessage EncodeScalarMessage(
            string recordingId, string applicationId,
            string entityPath, double value,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodeScalarArrowIpc(
                entityPath, value, rowId, chunkId, timelines);

            var rrdPayload = RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkId.TimeNs, chunkId.Inc,
                compression: 1, (ulong)arrowIpc.Length, arrowIpc);

            var grpcPayload = RerunProtobufEncoding.WrapArrowMsgAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(MsgKindArrowMsg, rrdPayload, grpcPayload,
                isStoreInfo: false, isStatic: false);
        }

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
                rowId, chunkId, timelines);

            var rrdPayload = RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkId.TimeNs, chunkId.Inc,
                compression: 1, (ulong)arrowIpc.Length, arrowIpc);

            var grpcPayload = RerunProtobufEncoding.WrapArrowMsgAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(MsgKindArrowMsg, rrdPayload, grpcPayload,
                isStoreInfo: false, isStatic: false);
        }

        public EncodedRerunMessage EncodeViewCoordinatesMessage(
            string recordingId, string applicationId,
            string entityPath, byte x, byte y, byte z)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowIpc = RerunArrowIpcEncoder.EncodeViewCoordinatesArrowIpc(
                entityPath, x, y, z, rowId, chunkId);

            var rrdPayload = RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkId.TimeNs, chunkId.Inc,
                compression: 1, (ulong)arrowIpc.Length, arrowIpc,
                isStatic: true);

            var grpcPayload = RerunProtobufEncoding.WrapArrowMsgAsLogMsg(rrdPayload);

            return new EncodedRerunMessage(MsgKindArrowMsg, rrdPayload, grpcPayload,
                isStoreInfo: false, isStatic: true);
        }
    }
}

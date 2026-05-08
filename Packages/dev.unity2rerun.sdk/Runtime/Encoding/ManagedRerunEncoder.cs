// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using Unity.RerunSDK.Core;

namespace Unity.RerunSDK.Encoding
{
    internal class ManagedRerunEncoder : IRerunEncoder
    {
        public byte[] EncodeSetStoreInfo(string recordingId, string applicationId)
        {
            var nowNs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
            return RerunProtobufEncoding.EncodeSetStoreInfo(recordingId, applicationId, nowNs, 1);
        }

        public byte[] EncodeTextLogArrowMsg(
            string recordingId, string applicationId,
            string entityPath, string text, string level,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowPayload = RerunArrowIpcEncoder.EncodeTextLogArrowIpc(
                entityPath, text, level, rowId, chunkId, timelines);

            return RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkIdTimeNs: chunkId.TimeNs, chunkIdInc: chunkId.Inc,
                compression: 1,
                uncompressedSize: (ulong)arrowPayload.Length,
                arrowIpcPayload: arrowPayload);
        }

        public byte[] EncodeScalarArrowMsg(
            string recordingId, string applicationId,
            string entityPath, double value,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowPayload = RerunArrowIpcEncoder.EncodeScalarArrowIpc(
                entityPath, value, rowId, chunkId, timelines);

            return RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkIdTimeNs: chunkId.TimeNs, chunkIdInc: chunkId.Inc,
                compression: 1,
                uncompressedSize: (ulong)arrowPayload.Length,
                arrowIpcPayload: arrowPayload);
        }

        public byte[] EncodeTransform3DArrowMsg(
            string recordingId, string applicationId,
            string entityPath,
            float tx, float ty, float tz,
            float qx, float qy, float qz, float qw,
            IReadOnlyList<RerunTimelineEntry> timelines)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowPayload = RerunArrowIpcEncoder.EncodeTransform3DArrowIpc(
                entityPath, tx, ty, tz, qx, qy, qz, qw,
                rowId, chunkId, timelines);

            return RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkIdTimeNs: chunkId.TimeNs, chunkIdInc: chunkId.Inc,
                compression: 1,
                uncompressedSize: (ulong)arrowPayload.Length,
                arrowIpcPayload: arrowPayload);
        }

        public byte[] EncodeViewCoordinatesArrowMsg(
            string recordingId, string applicationId,
            string entityPath, byte x, byte y, byte z)
        {
            var rowId = RerunTuidGenerator.Next();
            var chunkId = RerunTuidGenerator.Next();

            var arrowPayload = RerunArrowIpcEncoder.EncodeViewCoordinatesArrowIpc(
                entityPath, x, y, z, rowId, chunkId);

            return RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkIdTimeNs: chunkId.TimeNs, chunkIdInc: chunkId.Inc,
                compression: 1,
                uncompressedSize: (ulong)arrowPayload.Length,
                arrowIpcPayload: arrowPayload,
                isStatic: true);
        }
    }
}

// SPDX-License-Identifier: Apache-2.0
//
// Default managed encoder. Phase 2 implements protobuf encoding for
// SetStoreInfo and TextLog chunks using the hand-written protobuf helpers.
//
// Arrow IPC payload generation is stubbed for Phase 3 — see RerunArrowIpcWriter.

using System;
using System.Threading;

namespace Unity.RerunSDK.Encoding
{
    public class ManagedRerunEncoder : IRerunEncoder
    {
        private int _messageSeq;

        public byte[] EncodeSetStoreInfo(string recordingId, string applicationId)
        {
            var nowNs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
            return RerunProtobufEncoding.EncodeSetStoreInfo(recordingId, applicationId, nowNs, 1);
        }

        public byte[] EncodeTextLogChunk(
            string recordingId, string applicationId,
            string entityPath, string text, string level,
            long logTick)
        {
            int seq = Interlocked.Increment(ref _messageSeq);

            var arrowPayload = RerunArrowIpcWriter.BuildTextLogPayload(
                entityPath, text, level, logTick, (ulong)seq, (ulong)seq);

            return RerunProtobufEncoding.EncodeArrowMsg(
                recordingId, applicationId,
                chunkIdTimeNs: (ulong)seq, chunkIdInc: (ulong)seq,
                compression: 1, // NONE
                uncompressedSize: (ulong)arrowPayload.Length,
                arrowIpcPayload: arrowPayload);
        }
    }
}

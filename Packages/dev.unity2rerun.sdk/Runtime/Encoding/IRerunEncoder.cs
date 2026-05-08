// SPDX-License-Identifier: Apache-2.0
//
// Encoder interface for producing RRD transport messages.

namespace Unity.RerunSDK.Encoding
{
    /// Produces bytes for a single RRD transport message
    /// (MessageHeader + protobuf payload).
    public interface IRerunEncoder
    {
        byte[] EncodeSetStoreInfo(string recordingId, string applicationId);
        byte[] EncodeTextLogChunk(
            string recordingId, string applicationId,
            string entityPath, string text, string level,
            long logTick);
    }
}

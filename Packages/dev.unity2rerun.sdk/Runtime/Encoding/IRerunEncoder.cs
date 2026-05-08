// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.RerunSDK.Core;

namespace Unity.RerunSDK.Encoding
{
    internal interface IRerunEncoder
    {
        byte[] EncodeSetStoreInfo(string recordingId, string applicationId);
        byte[] EncodeTextLogArrowMsg(
            string recordingId, string applicationId,
            string entityPath, string text, string level,
            IReadOnlyList<RerunTimelineEntry> timelines);
        byte[] EncodeScalarArrowMsg(
            string recordingId, string applicationId,
            string entityPath, double value,
            IReadOnlyList<RerunTimelineEntry> timelines);
        byte[] EncodeTransform3DArrowMsg(
            string recordingId, string applicationId,
            string entityPath,
            float tx, float ty, float tz,
            float qx, float qy, float qz, float qw,
            IReadOnlyList<RerunTimelineEntry> timelines);
        byte[] EncodeViewCoordinatesArrowMsg(
            string recordingId, string applicationId,
            string entityPath, byte x, byte y, byte z);
    }
}

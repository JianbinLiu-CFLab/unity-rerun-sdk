// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.RerunSDK.Core;

namespace Unity.RerunSDK.Encoding
{
    internal interface IRerunEncoder
    {
        EncodedRerunMessage EncodeSetStoreInfoMessage(string recordingId, string applicationId);
        EncodedRerunMessage EncodeTextLogMessage(
            string recordingId, string applicationId,
            string entityPath, string text, string level,
            IReadOnlyList<RerunTimelineEntry> timelines);
        EncodedRerunMessage EncodeScalarMessage(
            string recordingId, string applicationId,
            string entityPath, double value,
            IReadOnlyList<RerunTimelineEntry> timelines);
        EncodedRerunMessage EncodeTransform3DMessage(
            string recordingId, string applicationId,
            string entityPath,
            float tx, float ty, float tz,
            float qx, float qy, float qz, float qw,
            IReadOnlyList<RerunTimelineEntry> timelines);
        EncodedRerunMessage EncodeViewCoordinatesMessage(
            string recordingId, string applicationId,
            string entityPath, byte x, byte y, byte z);
        EncodedRerunMessage EncodePinholeMessage(
            string recordingId, string applicationId,
            string entityPath, RerunPinhole pinhole);
    }
}

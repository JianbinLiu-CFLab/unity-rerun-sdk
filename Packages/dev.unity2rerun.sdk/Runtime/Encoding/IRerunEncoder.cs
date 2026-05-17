// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Encoding
// Purpose: Defines managed Rerun encoding primitives used by RRD files and live transport.

using System.Collections.Generic;
using Unity.RerunSDK.Core;

namespace Unity.RerunSDK.Encoding
{
    /// <summary>
    /// Defines the contract for IRerun Encoder.
    /// </summary>
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

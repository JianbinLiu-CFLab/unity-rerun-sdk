// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/IO/Rrd
// Purpose: Writes Rerun RRD stream records, manifests, and footer metadata.

namespace Unity.RerunSDK.IO.Rrd
{
    /// <summary>
    /// Carries RRD Payload Span data across Unity2Rerun runtime boundaries.
    /// </summary>
    public readonly struct RrdPayloadSpan
    {
        public ulong Offset { get; }
        public ulong Length { get; }

        public RrdPayloadSpan(ulong offset, ulong length)
        {
            Offset = offset;
            Length = length;
        }
    }
}

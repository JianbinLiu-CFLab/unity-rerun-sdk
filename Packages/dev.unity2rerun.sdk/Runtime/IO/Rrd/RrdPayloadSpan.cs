// SPDX-License-Identifier: Apache-2.0

namespace Unity.RerunSDK.IO.Rrd
{
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

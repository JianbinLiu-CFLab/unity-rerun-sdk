// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;

namespace Unity.RerunSDK.Encoding
{
    /// Generates Rerun TUIDs (Time-based Unique Identifiers).
    /// 128-bit: [time_ns: 8 bytes BE][inc: 8 bytes BE].
    /// time_ns is UTC unix nanoseconds; inc is a monotonic per-process counter.
    internal static class RerunTuidGenerator
    {
        private static long _inc;

        /// Generate a new TUID from current UTC time + monotonic inc.
        public static RerunTuid Next()
        {
            var timeNs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000;
            var inc = (ulong)Interlocked.Increment(ref _inc);
            return new RerunTuid(timeNs, inc);
        }

        /// Encode a TUID as a 16-byte big-endian buffer for Arrow row_id / chunk_id.
        public static byte[] ToBytes(RerunTuid tuid)
        {
            var bytes = new byte[16];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(0, 8), tuid.TimeNs);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(8, 8), tuid.Inc);
            return bytes;
        }

        /// Format a TUID as a 32-character uppercase hex string for schema metadata rerun:id.
        public static string ToHexString(RerunTuid tuid)
        {
            return tuid.TimeNs.ToString("X16") + tuid.Inc.ToString("X16");
        }
    }

    internal readonly struct RerunTuid
    {
        public ulong TimeNs { get; }
        public ulong Inc { get; }

        public RerunTuid(ulong timeNs, ulong inc)
        {
            TimeNs = timeNs;
            Inc = inc;
        }
    }
}

// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Util
// Purpose: Provides low-level utility code used by managed Rerun encoding.

// Minimal xxHash32 matching Rust's xxhash_rust::xxh32.
// CRC seed 7850921 = "RERUN" in base-26 (A=0..Z=25).

using System;

namespace Unity.RerunSDK.Util
{
    /// <summary>
    /// Implements the xxHash32 checksum used by Rerun footer and manifest validation.
    /// </summary>
    public static class XxHash32
    {
        // xxHash32 prime constants from the canonical algorithm definition.
        private const uint Prime1 = 2654435761u;
        private const uint Prime2 = 2246822519u;
        private const uint Prime3 = 3266489917u;
        private const uint Prime4 = 668265263u;
        private const uint Prime5 = 374761393u;
        /// <summary>
        /// Computes the xxHash32 checksum for the provided byte buffer.
        /// </summary>
        public static uint Compute(byte[] data, uint seed)
        {
            int len = data.Length;
            uint h32;
            int pos = 0;

            if (len >= 16)
            {
                uint v1 = seed + Prime1 + Prime2;
                uint v2 = seed + Prime2;
                uint v3 = seed;
                uint v4 = seed - Prime1;

                while (pos + 16 <= len)
                {
                    v1 = Round(v1, ReadU32(data, pos)); pos += 4;
                    v2 = Round(v2, ReadU32(data, pos)); pos += 4;
                    v3 = Round(v3, ReadU32(data, pos)); pos += 4;
                    v4 = Round(v4, ReadU32(data, pos)); pos += 4;
                }

                h32 = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
            }
            else
            {
                h32 = seed + Prime5;
            }

            h32 += (uint)len;

            int remaining = len - pos;
            while (remaining >= 4) { h32 = RotateLeft(h32 + ReadU32(data, pos) * Prime3, 17) * Prime4; pos += 4; remaining -= 4; }
            while (remaining > 0) { h32 = RotateLeft(h32 + data[pos++] * Prime5, 11) * Prime1; remaining--; }

            h32 ^= h32 >> 15;
            h32 *= Prime2;
            h32 ^= h32 >> 13;
            h32 *= Prime3;
            h32 ^= h32 >> 16;

            return h32;
        }

        private static uint Round(uint acc, uint input)
        {
            acc += input * Prime2;
            acc = RotateLeft(acc, 13);
            acc *= Prime1;
            return acc;
        }

        private static uint ReadU32(byte[] data, int pos)
        {
            return (uint)(data[pos] | (data[pos + 1] << 8) | (data[pos + 2] << 16) | (data[pos + 3] << 24));
        }

        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (32 - count));
        }
    }
}

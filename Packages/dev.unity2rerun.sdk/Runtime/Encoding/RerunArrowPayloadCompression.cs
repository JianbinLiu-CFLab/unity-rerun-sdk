// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Encoding
// Purpose: Compresses Arrow IPC payloads for RRD file recording.

using System;
using K4os.Compression.LZ4;
using Unity.RerunSDK.Core;
using RerunCommon = Rerun.Common.V1Alpha1;

namespace Unity.RerunSDK.Encoding
{
    /// <summary>
    /// Applies Rerun-compatible Arrow payload compression for recorded RRD chunks.
    /// </summary>
    internal static class RerunArrowPayloadCompression
    {
        /// <summary>
        /// Returns the payload bytes and wire compression enum for a recording payload.
        /// </summary>
        public static byte[] EncodeForRecording(
            byte[] arrowIpc,
            RerunRecordingCompression compression,
            out RerunCommon.Compression wireCompression)
        {
            if (arrowIpc == null)
                throw new ArgumentNullException(nameof(arrowIpc));

            switch (compression)
            {
                case RerunRecordingCompression.None:
                    wireCompression = RerunCommon.Compression.None;
                    return arrowIpc;

                case RerunRecordingCompression.Lz4:
                    wireCompression = RerunCommon.Compression.Lz4;
                    return EncodeLz4Block(arrowIpc);

                default:
                    throw new ArgumentOutOfRangeException(nameof(compression), compression, null);
            }
        }

        private static byte[] EncodeLz4Block(byte[] arrowIpc)
        {
            var output = new byte[LZ4Codec.MaximumOutputSize(arrowIpc.Length)];
            var written = LZ4Codec.Encode(
                arrowIpc, 0, arrowIpc.Length,
                output, 0, output.Length,
                LZ4Level.L00_FAST);

            if (written <= 0)
                throw new InvalidOperationException("LZ4 encoding produced no payload.");

            if (written == output.Length)
                return output;

            var trimmed = new byte[written];
            Buffer.BlockCopy(output, 0, trimmed, 0, written);
            return trimmed;
        }
    }
}

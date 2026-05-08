// SPDX-License-Identifier: Apache-2.0
//
// Low-level RRD binary stream writer.
// Handles the custom RRD framing protocol: StreamHeader, MessageHeader,
// End message, and StreamFooter.

using System;
using System.IO;
using Unity.RerunSDK.Util;

namespace Unity.RerunSDK.IO.Rrd
{
    public static class RrdConstants
    {
        public static readonly byte[] FourCC = { (byte)'R', (byte)'R', (byte)'F', (byte)'2' };

        public const ulong MsgKindEnd = 0;
        public const ulong MsgKindSetStoreInfo = 1;
        public const ulong MsgKindArrowMsg = 2;

        public const int MessageHeaderSize = 16;
        public const int StreamFooterFixedSize = 32;
    }

    /// RRD binary stream writer that produces .rrd files.
    public class RrdWriter : IDisposable
    {
        private readonly Stream _stream;
        private long _numWritten;
        private bool _finished;

        public RrdWriter(Stream stream)
        {
            _stream = stream;
        }

        /// Write the StreamHeader: "RRF2" + version + encoding options.
        public void WriteStreamHeader()
        {
            var buf = new byte[12];
            buf[0] = (byte)'R'; buf[1] = (byte)'R'; buf[2] = (byte)'F'; buf[3] = (byte)'2';
            // CrateVersion 0.23.0: [major, minor, patch, meta]
            buf[4] = 0; buf[5] = 23; buf[6] = 0; buf[7] = 0;
            // EncodingOptions: compression=0 (Off), serializer=2 (Protobuf)
            buf[8] = 0; buf[9] = 2; buf[10] = 0; buf[11] = 0;

            _stream.Write(buf, 0, buf.Length);
            _numWritten += buf.Length;
        }

        /// Write a message: MessageHeader + payload.
        public void WriteMessage(ulong kind, byte[] payload)
        {
            var header = new byte[RrdConstants.MessageHeaderSize];
            BitConverter.GetBytes(kind).CopyTo(header, 0);
            BitConverter.GetBytes((ulong)payload.Length).CopyTo(header, 8);

            _stream.Write(header, 0, header.Length);
            _stream.Write(payload, 0, payload.Length);
            _numWritten += header.Length + payload.Length;
        }

        /// Write the End message + StreamFooter (no-footer mode).
        /// Even with an empty RrdFooter, we compute the xxHash32 CRC so that
        /// downstream tools that validate footer integrity don't reject the file.
        public void FinishNoFooter()
        {
            if (_finished) return;
            _finished = true;

            var emptyFooter = Array.Empty<byte>();
            uint crc = XxHash32.Compute(emptyFooter, seed: 7850921);

            WriteMessage(RrdConstants.MsgKindEnd, emptyFooter);
            WriteStreamFooter(_numWritten - emptyFooter.Length, emptyFooter.Length, crc);
        }

        private void WriteStreamFooter(long footerOffset, long footerLen, uint crc)
        {
            var buf = new byte[RrdConstants.StreamFooterFixedSize];
            int pos = 0;

            // Entry: offset(8 LE) + len(8 LE) + crc(4 LE)
            BitConverter.GetBytes((ulong)footerOffset).CopyTo(buf, pos); pos += 8;
            BitConverter.GetBytes((ulong)footerLen).CopyTo(buf, pos); pos += 8;
            BitConverter.GetBytes(crc).CopyTo(buf, pos); pos += 4;

            // Fixed part: "RRF2" + "FOOT" + count(4 LE)
            buf[pos++] = (byte)'R'; buf[pos++] = (byte)'R'; buf[pos++] = (byte)'F'; buf[pos++] = (byte)'2';
            buf[pos++] = (byte)'F'; buf[pos++] = (byte)'O'; buf[pos++] = (byte)'O'; buf[pos++] = (byte)'T';
            BitConverter.GetBytes(1u).CopyTo(buf, pos);

            _stream.Write(buf, 0, buf.Length);
            _numWritten += buf.Length;
        }

        public void Dispose()
        {
            if (!_finished) FinishNoFooter();
            _stream?.Dispose();
        }
    }
}

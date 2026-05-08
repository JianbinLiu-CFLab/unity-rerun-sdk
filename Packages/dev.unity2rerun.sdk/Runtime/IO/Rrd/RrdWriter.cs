// SPDX-License-Identifier: Apache-2.0
//
// Low-level RRD binary stream writer.
// Handles the custom RRD framing protocol: StreamHeader, MessageHeader,
// End message, and StreamFooter.

using System;
using System.IO;

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

        /// Mark the stream as finished. In true no-footer mode, this is a no-op
        /// — the stream simply ends after the last ArrowMsg, matching the Rust
        /// encoder's do_not_emit_footer() path.
        public void FinishNoFooter()
        {
            _finished = true;
        }

        public void Dispose()
        {
            if (!_finished) FinishNoFooter();
            _stream?.Dispose();
        }
    }
}

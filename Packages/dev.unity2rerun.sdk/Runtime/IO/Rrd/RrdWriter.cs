// SPDX-License-Identifier: Apache-2.0
//
// Low-level RRD binary stream writer.
// Handles the custom RRD framing protocol: StreamHeader, MessageHeader,
// End message, and StreamFooter.

using System;
using System.IO;
using Google.Protobuf;
using Rerun.LogMsg.V1Alpha1;
using Unity.RerunSDK.Util;

namespace Unity.RerunSDK.IO.Rrd
{
    public static class RrdConstants
    {
        public static readonly byte[] FourCC = { (byte)'R', (byte)'R', (byte)'F', (byte)'2' };

        public const ulong MsgKindEnd = 0;
        public const ulong MsgKindSetStoreInfo = 1;
        public const ulong MsgKindArrowMsg = 2;

        public const int StreamHeaderSize = 12;
        public const int MessageHeaderSize = 16;
        public const int StreamFooterFixedSize = 32;
        public const int StreamFooterStaticPartSize = 12;
        public const uint StreamFooterCrcSeed = 7850921;
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
            var buf = new byte[RrdConstants.StreamHeaderSize];
            buf[0] = (byte)'R'; buf[1] = (byte)'R'; buf[2] = (byte)'F'; buf[3] = (byte)'2';
            // CrateVersion 0.23.0: [major, minor, patch, meta]
            buf[4] = 0; buf[5] = 23; buf[6] = 0; buf[7] = 0;
            // EncodingOptions: compression=0 (Off), serializer=2 (Protobuf)
            buf[8] = 0; buf[9] = 2; buf[10] = 0; buf[11] = 0;

            _stream.Write(buf, 0, buf.Length);
            _numWritten += buf.Length;
        }

        /// Write a message: MessageHeader + payload.
        public RrdPayloadSpan WriteMessage(ulong kind, byte[] payload)
        {
            if (_finished)
                throw new InvalidOperationException("Cannot write to an RRD stream after it has been finalized.");

            var header = new byte[RrdConstants.MessageHeaderSize];
            BitConverter.GetBytes(kind).CopyTo(header, 0);
            BitConverter.GetBytes((ulong)payload.Length).CopyTo(header, 8);

            var payloadOffset = _numWritten + header.Length;
            _stream.Write(header, 0, header.Length);
            _stream.Write(payload, 0, payload.Length);
            _numWritten += header.Length + payload.Length;

            return new RrdPayloadSpan((ulong)payloadOffset, (ulong)payload.Length);
        }

        public void Flush()
        {
            _stream.Flush();
        }

        /// Mark the stream as finished without appending an End message or StreamFooter.
        public void FinishNoFooter()
        {
            _finished = true;
        }

        public void FinishWithFooter(RrdFooter footer)
        {
            if (_finished)
                return;

            var payload = footer.ToByteArray();
            var footerSpan = WriteMessage(RrdConstants.MsgKindEnd, payload);
            WriteStreamFooter(footerSpan, XxHash32.Compute(payload, RrdConstants.StreamFooterCrcSeed));
            _finished = true;
            _stream.Flush();
        }

        private void WriteStreamFooter(RrdPayloadSpan footerSpan, uint crc)
        {
            var entry = new byte[20];
            BitConverter.GetBytes(footerSpan.Offset).CopyTo(entry, 0);
            BitConverter.GetBytes(footerSpan.Length).CopyTo(entry, 8);
            BitConverter.GetBytes(crc).CopyTo(entry, 16);

            var fixedPart = new byte[RrdConstants.StreamFooterStaticPartSize];
            RrdConstants.FourCC.CopyTo(fixedPart, 0);
            fixedPart[4] = (byte)'F';
            fixedPart[5] = (byte)'O';
            fixedPart[6] = (byte)'O';
            fixedPart[7] = (byte)'T';
            BitConverter.GetBytes(1u).CopyTo(fixedPart, 8);

            _stream.Write(entry, 0, entry.Length);
            _stream.Write(fixedPart, 0, fixedPart.Length);
            _numWritten += entry.Length + fixedPart.Length;
        }

        public void Dispose()
        {
            if (!_finished) FinishNoFooter();
            _stream?.Dispose();
        }
    }
}

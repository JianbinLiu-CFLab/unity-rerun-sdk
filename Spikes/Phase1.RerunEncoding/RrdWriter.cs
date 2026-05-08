/// Low-level writer for Rerun RRD streams.
///
/// RRD stream structure:
///   StreamHeader (12 bytes)
///   Message* (MessageHeader 16 bytes + protobuf payload)
///   EndMessage: MessageHeader(kind=0) + RrdFooter + StreamFooter
///
/// Phase 1 writes WITHOUT footer (no_footer mode), matching the Rust
/// encoder's `do_not_emit_footer()` path. The End message and StreamFooter
/// are still written, but the footer payload is empty/placeholder.
///
/// For the minimal Phase 1 spike, we write:
///   StreamHeader | SetStoreInfo | ArrowMsg(TextLog) | End(empty footer) | StreamFooter

using Google.Protobuf;

namespace Phase1.RerunEncoding;

/// RRD frame constants from re_log_encoding::rrd::frames
public static class RrdConstants
{
    public const uint FourCC = 0x32465252; // "RRF2" in little-endian bytes
    public static readonly byte[] FourCCBytes = new byte[] { (byte)'R', (byte)'R', (byte)'F', (byte)'2' };

    // Message kinds
    public const ulong MsgKindEnd = 0;
    public const ulong MsgKindSetStoreInfo = 1;
    public const ulong MsgKindArrowMsg = 2;
    public const ulong MsgKindBlueprintActivation = 3;

    // Serializer: Protobuf = 2
    public const byte SerializerProtobuf = 2;

    // Message header size
    public const int MessageHeaderSize = 16;

    // Stream footer sizes
    public const int StreamFooterFixedSize = 32; // 20 (entry) + 12 (fourcc+id+count)
}

/// RRD binary stream writer that produces .rrd files.
public class RrdWriter : IDisposable
{
    private readonly Stream _stream;
    private readonly long _streamStartPos;
    private long _numWritten;
    private bool _finished;

    public RrdWriter(Stream stream)
    {
        _stream = stream;
        _streamStartPos = stream.Position;
    }

    /// Write the StreamHeader and return this writer for fluent chaining.
    /// Version bytes from the local Rerun clone (0.23+ compatible).
    /// Version 0.23.0 encoded as: major=0, minor=23, patch=0
    public void WriteStreamHeader()
    {
        var buf = new byte[12];

        // FourCC: "RRF2"
        buf[0] = (byte)'R';
        buf[1] = (byte)'R';
        buf[2] = (byte)'F';
        buf[3] = (byte)'2';

        // Version: CrateVersion 0.23.0
        // CrateVersion::to_bytes() returns [major, minor, patch, meta]
        buf[4] = 0;  // major
        buf[5] = 23; // minor
        buf[6] = 0;  // patch
        buf[7] = 0;  // meta (none)

        // EncodingOptions: compression=0 (Off), serializer=2 (Protobuf), reserved=0,0
        buf[8] = 0; // compression: Off
        buf[9] = 2; // serializer: Protobuf
        buf[10] = 0; // reserved
        buf[11] = 0; // reserved

        _stream.Write(buf);
        _numWritten += buf.Length;
    }

    /// Write a single message: MessageHeader + protobuf payload.
    /// Returns the byte span (start, length) excluding header, for footer computation.
    public (long startExcludingHeader, long lenExcludingHeader) WriteMessage(ulong kind, IMessage payload)
    {
        var payloadBytes = payload.ToByteArray();
        return WriteMessageRaw(kind, payloadBytes);
    }

    /// Write a message with pre-serialized payload bytes.
    public (long startExcludingHeader, long lenExcludingHeader) WriteMessageRaw(ulong kind, byte[] payload)
    {
        // MessageHeader: kind(8 LE) + len(8 LE) = 16 bytes
        var header = new byte[16];
        BitConverter.GetBytes(kind).CopyTo(header, 0);
        BitConverter.GetBytes((ulong)payload.Length).CopyTo(header, 8);

        var startExcludingHeader = _numWritten + RrdConstants.MessageHeaderSize;

        _stream.Write(header);
        _stream.Write(payload);
        _numWritten += header.Length + payload.Length;

        return (startExcludingHeader, payload.Length);
    }

    /// Finish the stream: write End message + StreamFooter.
    /// In no_footer mode, the End message has a minimal/empty footer placeholder.
    public void FinishNoFooter()
    {
        if (_finished) return;
        _finished = true;

        // Build a minimal empty RrdFooter (no manifests).
        var footer = new Rerun.LogMsg.V1Alpha1.RrdFooter();
        var footerBytes = footer.ToByteArray();

        // Write End message
        var endStart = WriteMessageRaw(RrdConstants.MsgKindEnd, footerBytes);

        // Compute xxhash32 CRC of the footer bytes (matching Rust StreamFooter)
        uint crc = XxHash32.Compute(footerBytes, seed: 7850921); // "RERUN" in base-26

        // Write StreamFooter
        WriteStreamFooter(endStart.startExcludingHeader, endStart.lenExcludingHeader, crc);
    }

    private void WriteStreamFooter(long footerByteOffsetExcludingHeader, long footerLenExcludingHeader, uint crc)
    {
        var buf = new byte[RrdConstants.StreamFooterFixedSize];

        // Entry (20 bytes): offset(8 LE) + len(8 LE) + crc(4 LE)
        int pos = 0;
        BitConverter.GetBytes((ulong)footerByteOffsetExcludingHeader).CopyTo(buf, pos); pos += 8;
        BitConverter.GetBytes((ulong)footerLenExcludingHeader).CopyTo(buf, pos); pos += 8;
        BitConverter.GetBytes(crc).CopyTo(buf, pos); pos += 4;

        // Fixed part (12 bytes): fourcc(4) + identifier "FOOT"(4) + num_entries(4 LE)
        buf[pos++] = (byte)'R';
        buf[pos++] = (byte)'R';
        buf[pos++] = (byte)'F';
        buf[pos++] = (byte)'2';
        buf[pos++] = (byte)'F';
        buf[pos++] = (byte)'O';
        buf[pos++] = (byte)'O';
        buf[pos++] = (byte)'T';
        BitConverter.GetBytes(1u).CopyTo(buf, pos); // num_rrd_footers = 1

        _stream.Write(buf);
        _numWritten += buf.Length;
    }

    public void Dispose()
    {
        if (!_finished)
        {
            FinishNoFooter();
        }
        _stream?.Dispose();
    }
}

/// Minimal xxHash32 implementation matching Rust's xxhash_rust::xxh32.
/// CRC seed: 7850921 = "RERUN" in base-26 (A=0, Z=25).
public static class XxHash32
{
    private const uint Prime1 = 2654435761u;
    private const uint Prime2 = 2246822519u;
    private const uint Prime3 = 3266489917u;
    private const uint Prime4 = 668265263u;
    private const uint Prime5 = 374761393u;

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

    private static uint ReadU32(byte[] data, int pos) =>
        BitConverter.ToUInt32(data, pos);

    private static uint RotateLeft(uint value, int count) =>
        (value << count) | (value >> (32 - count));
}

// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Verifies Phase 13 RRD-only LZ4 compression behavior.

using System.Collections.Generic;
using System.IO;
using Apache.Arrow.Ipc;
using Google.Protobuf;
using K4os.Compression.LZ4;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Xunit;
using RerunCommon = Rerun.Common.V1Alpha1;
using RerunLogMsg = Rerun.LogMsg.V1Alpha1;

/// <summary>
/// Regression tests for Phase 13 RRD compression behavior.
/// </summary>
public class Phase13CompressionTests
{
    private static readonly List<RerunTimelineEntry> Timeline = new()
    {
        new("frame", 13, RerunTimelineKind.Sequence)
    };

    [Fact]
    public void Default_encoder_keeps_arrow_messages_uncompressed()
    {
        var encoder = new ManagedRerunEncoder();
        var msg = encoder.EncodeScalarMessage("rec", "app", "metrics/fps", 60.0, Timeline);

        var fileArrow = RerunLogMsg.ArrowMsg.Parser.ParseFrom(msg.RrdPayload);
        var liveArrow = RerunLogMsg.LogMsg.Parser.ParseFrom(msg.GrpcLogMsgBytes).ArrowMsg;

        Assert.Equal(RerunCommon.Compression.None, fileArrow.Compression);
        Assert.Equal(RerunCommon.Compression.None, liveArrow.Compression);
        Assert.Equal((ulong)fileArrow.Payload.Length, fileArrow.UncompressedSize);
        Assert.Equal(fileArrow.Payload, liveArrow.Payload);
    }

    [Fact]
    public void Lz4_encoder_compresses_rrd_payload_and_roundtrips_arrow_ipc()
    {
        var encoder = new ManagedRerunEncoder(RerunRecordingCompression.Lz4);
        var msg = encoder.EncodeTextLogMessage(
            "rec", "app", "logs/lz4", new string('x', 2048), "INFO", Timeline);

        var fileArrow = RerunLogMsg.ArrowMsg.Parser.ParseFrom(msg.RrdPayload);

        Assert.Equal(RerunCommon.Compression.Lz4, fileArrow.Compression);
        Assert.True(fileArrow.UncompressedSize > 0);

        var arrowIpc = DecodeLz4(fileArrow.Payload.ToByteArray(), checked((int)fileArrow.UncompressedSize));
        using var ms = new MemoryStream(arrowIpc);
        using var reader = new ArrowStreamReader(ms);
        var batch = reader.ReadNextRecordBatch();

        Assert.NotNull(batch);
        Assert.Equal("logs/lz4", batch.Schema.Metadata["rerun:entity_path"]);
    }

    [Fact]
    public void Lz4_encoder_keeps_live_payload_uncompressed()
    {
        var encoder = new ManagedRerunEncoder(RerunRecordingCompression.Lz4);
        var msg = encoder.EncodeScalarMessage("rec", "app", "metrics/lz4", 13.0, Timeline);

        var fileArrow = RerunLogMsg.ArrowMsg.Parser.ParseFrom(msg.RrdPayload);
        var liveArrow = RerunLogMsg.LogMsg.Parser.ParseFrom(msg.GrpcLogMsgBytes).ArrowMsg;
        var decodedFilePayload = DecodeLz4(
            fileArrow.Payload.ToByteArray(),
            checked((int)fileArrow.UncompressedSize));

        Assert.Equal(RerunCommon.Compression.Lz4, fileArrow.Compression);
        Assert.Equal(RerunCommon.Compression.None, liveArrow.Compression);
        Assert.Equal(fileArrow.UncompressedSize, liveArrow.UncompressedSize);
        Assert.Equal((ulong)liveArrow.Payload.Length, liveArrow.UncompressedSize);
        Assert.Equal(decodedFilePayload, liveArrow.Payload.ToByteArray());
    }

    [Fact]
    public void Lz4_encoder_leaves_store_info_as_set_store_info()
    {
        var encoder = new ManagedRerunEncoder(RerunRecordingCompression.Lz4);
        var msg = encoder.EncodeSetStoreInfoMessage("rec", "app");

        var fileStoreInfo = RerunLogMsg.SetStoreInfo.Parser.ParseFrom(msg.RrdPayload);
        var liveLogMsg = RerunLogMsg.LogMsg.Parser.ParseFrom(msg.GrpcLogMsgBytes);

        Assert.Equal("rec", fileStoreInfo.Info.StoreId.RecordingId);
        Assert.Equal(RerunLogMsg.LogMsg.MsgOneofCase.SetStoreInfo, liveLogMsg.MsgCase);
        Assert.True(msg.IsStoreInfo);
    }

    [Fact]
    public void Phase13_smoke_writer_emits_lz4_arrow_messages()
    {
        var path = Path.Combine(Path.GetTempPath(), $"phase13_lz4_{Path.GetRandomFileName()}.rrd");
        try
        {
            Phase13RrdWriter.WritePhase13Lz4Rrd(path);

            var result = ReadRrdCompressionSummary(path);

            Assert.Equal(1, result.StoreInfoCount);
            Assert.True(result.ArrowCount >= 4);
            Assert.Equal(result.ArrowCount, result.Lz4ArrowCount);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private static byte[] DecodeLz4(byte[] compressed, int uncompressedSize)
    {
        var decoded = new byte[uncompressedSize];
        var decodedLength = LZ4Codec.Decode(
            compressed, 0, compressed.Length,
            decoded, 0, decoded.Length);
        Assert.Equal(uncompressedSize, decodedLength);
        return decoded;
    }

    private static RrdCompressionSummary ReadRrdCompressionSummary(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var offset = 12;
        var summary = new RrdCompressionSummary();

        while (offset + 16 <= bytes.Length)
        {
            var kind = System.BitConverter.ToUInt64(bytes, offset);
            var length = checked((int)System.BitConverter.ToUInt64(bytes, offset + 8));
            offset += 16;

            var payload = new byte[length];
            System.Buffer.BlockCopy(bytes, offset, payload, 0, length);
            offset += length;

            if (kind == Unity.RerunSDK.IO.Rrd.RrdConstants.MsgKindEnd)
                break;

            if (kind == Unity.RerunSDK.IO.Rrd.RrdConstants.MsgKindSetStoreInfo)
            {
                summary.StoreInfoCount++;
                continue;
            }

            if (kind == Unity.RerunSDK.IO.Rrd.RrdConstants.MsgKindArrowMsg)
            {
                summary.ArrowCount++;
                var arrowMsg = RerunLogMsg.ArrowMsg.Parser.ParseFrom(payload);
                if (arrowMsg.Compression == RerunCommon.Compression.Lz4)
                    summary.Lz4ArrowCount++;
            }
        }

        return summary;
    }

    private struct RrdCompressionSummary
    {
        public int StoreInfoCount;
        public int ArrowCount;
        public int Lz4ArrowCount;
    }
}

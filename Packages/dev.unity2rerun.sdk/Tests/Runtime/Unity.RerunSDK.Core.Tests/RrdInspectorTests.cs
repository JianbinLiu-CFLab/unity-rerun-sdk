// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Verifies RRD compression inspection and evidence summaries.

using System;
using System.IO;
using Google.Protobuf;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;
using Xunit;
using RerunCommon = Rerun.Common.V1Alpha1;
using RerunLogMsg = Rerun.LogMsg.V1Alpha1;

/// <summary>
/// Regression tests for RrdInspector compression evidence.
/// </summary>
public class RrdInspectorTests
{
    [Fact]
    public void Inspect_bytes_counts_none_lz4_and_unknown_arrow_compression()
    {
        var bytes = WriteRawRrd(
            MakeArrow(RerunCommon.Compression.None, storedBytes: 100, uncompressedBytes: 100),
            MakeArrow(RerunCommon.Compression.Lz4, storedBytes: 40, uncompressedBytes: 100),
            MakeArrow((RerunCommon.Compression)99, storedBytes: 12, uncompressedBytes: 100));

        var result = RrdInspector.InspectBytes(bytes, "mixed.rrd");

        Assert.Equal("mixed.rrd", result.InputPath);
        Assert.Equal(3, result.ArrowMsgCount);
        Assert.Equal(1, result.CompressionNoneCount);
        Assert.Equal(1, result.CompressionLz4Count);
        Assert.Equal(1, result.UnknownCompressionCount);
        Assert.True(result.UnknownCompressionValues.ContainsKey(99));
        Assert.Equal(152UL, result.TotalStoredPayloadBytes);
        Assert.Equal(300UL, result.TotalDeclaredUncompressedBytes);
        Assert.True(result.StoredToUncompressedRatio.HasValue);
        Assert.Equal(152.0 / 300.0, result.StoredToUncompressedRatio.Value, precision: 6);
        Assert.False(result.IsReleaseEvidenceAccepted);
    }

    [Fact]
    public void Inspect_file_handles_footer_present_rrd_from_backend()
    {
        var path = Path.Combine(Path.GetTempPath(), $"rrd_inspector_{Path.GetRandomFileName()}.rrd");
        try
        {
            WriteBackendRrd(path, RerunRecordingCompression.None);

            var result = RrdInspector.InspectFile(path);

            Assert.Equal(Path.GetFullPath(path), result.InputPath);
            Assert.Equal(1, result.CompressionNoneCount);
            Assert.Equal(0, result.CompressionLz4Count);
            Assert.Equal(0, result.UnknownCompressionCount);
            Assert.True(result.SawStreamFooter);
            Assert.True(result.IsReleaseEvidenceAccepted);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Inspect_bytes_handles_no_footer_rrd()
    {
        var bytes = WriteRawRrd(
            new[] { MakeArrow(RerunCommon.Compression.None, 8, 8) },
            finishWithNoFooter: true);

        var result = RrdInspector.InspectBytes(bytes, "no_footer.rrd");

        Assert.Equal(1, result.ArrowMsgCount);
        Assert.False(result.SawStreamFooter);
        Assert.True(result.IsReleaseEvidenceAccepted);
    }

    [Fact]
    public void Inspect_bytes_rejects_malformed_payload_length()
    {
        var bytes = new byte[RrdConstants.StreamHeaderSize + RrdConstants.MessageHeaderSize];
        RrdConstants.FourCC.CopyTo(bytes, 0);
        BitConverter.GetBytes(RrdConstants.MsgKindArrowMsg).CopyTo(bytes, RrdConstants.StreamHeaderSize);
        BitConverter.GetBytes(999UL).CopyTo(bytes, RrdConstants.StreamHeaderSize + 8);

        var ex = Assert.Throws<InvalidDataException>(() => RrdInspector.InspectBytes(bytes, "bad.rrd"));
        Assert.Contains("declared payload length", ex.Message);
    }

    [Fact]
    public void FormatSummary_includes_counts_ratio_and_unknown_values()
    {
        var bytes = WriteRawRrd(
            MakeArrow(RerunCommon.Compression.Lz4, storedBytes: 10, uncompressedBytes: 20),
            MakeArrow((RerunCommon.Compression)77, storedBytes: 5, uncompressedBytes: 10));
        var result = RrdInspector.InspectBytes(bytes, "summary.rrd");

        var summary = RrdInspector.FormatSummary(result);

        Assert.Contains("summary.rrd", summary);
        Assert.Contains("ArrowMsg: 2", summary);
        Assert.Contains("CompressionLz4: 1", summary);
        Assert.Contains("CompressionOther: 1", summary);
        Assert.Contains("UnknownValues: 77=1", summary);
        Assert.Contains("StoredToUncompressedRatio: 0.500000", summary);
        Assert.Contains("Accepted: False", summary);
    }

    private static byte[] WriteRawRrd(params RerunLogMsg.ArrowMsg[] arrowMessages)
    {
        return WriteRawRrd(arrowMessages, finishWithNoFooter: false);
    }

    private static byte[] WriteRawRrd(RerunLogMsg.ArrowMsg[] arrowMessages, bool finishWithNoFooter)
    {
        using var ms = new MemoryStream();
        using var writer = new RrdWriter(ms);
        writer.WriteStreamHeader();

        foreach (var arrowMessage in arrowMessages)
            writer.WriteMessage(RrdConstants.MsgKindArrowMsg, arrowMessage.ToByteArray());

        if (finishWithNoFooter)
            writer.FinishNoFooter();

        return ms.ToArray();
    }

    private static RerunLogMsg.ArrowMsg MakeArrow(
        RerunCommon.Compression compression,
        int storedBytes,
        int uncompressedBytes)
    {
        return new RerunLogMsg.ArrowMsg
        {
            Compression = compression,
            UncompressedSize = (ulong)uncompressedBytes,
            Encoding = RerunLogMsg.Encoding.ArrowIpc,
            Payload = ByteString.CopyFrom(new byte[storedBytes])
        };
    }

    private static void WriteBackendRrd(string path, RerunRecordingCompression compression)
    {
        var encoder = new ManagedRerunEncoder(compression);
        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new RrdWriter(fs);
        var backend = new RrdRerunBackend(writer);
        var runtime = new RerunRuntime("rrd_inspector_tests", backend);

        backend.Initialize(runtime);
        backend.Write(encoder.EncodeSetStoreInfoMessage("rrd-inspector-test", "rrd_inspector_tests"));
        backend.Write(encoder.EncodeScalarMessage(
            "rrd-inspector-test",
            "rrd_inspector_tests",
            "metrics/inspector",
            1.0,
            Array.Empty<RerunTimelineEntry>()));
        backend.Shutdown();
    }
}

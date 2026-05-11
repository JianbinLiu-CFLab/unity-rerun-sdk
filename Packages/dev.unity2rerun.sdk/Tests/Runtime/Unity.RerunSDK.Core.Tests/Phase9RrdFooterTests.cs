using System;
using System.Collections.Generic;
using System.IO;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;
using Unity.RerunSDK.Util;
using Xunit;
using RerunLogMsg = Rerun.LogMsg.V1Alpha1;

public class Phase9RrdFooterTests
{
    private const int StreamHeaderSize = 12;
    private const uint StreamFooterCrcSeed = 7850921;

    private static readonly List<RerunTimelineEntry> Timeline = new()
    {
        new("frame", 1, RerunTimelineKind.Sequence)
    };

    [Fact]
    public void Backend_shutdown_writes_end_footer_and_stream_footer()
    {
        var bytes = WriteScalarRrd(callFlushBeforeShutdown: false);

        var footer = ReadStreamFooter(bytes);
        Assert.True(footer.Start > StreamHeaderSize);
        Assert.True(footer.Length > 0);

        var footerPayload = new byte[footer.Length];
        Array.Copy(bytes, (long)footer.Start, footerPayload, 0, footerPayload.Length);
        Assert.Equal(footer.Crc, XxHash32.Compute(footerPayload, StreamFooterCrcSeed));

        var endHeaderOffset = (long)footer.Start - RrdConstants.MessageHeaderSize;
        Assert.Equal(RrdConstants.MsgKindEnd, BitConverter.ToUInt64(bytes, (int)endHeaderOffset));
        Assert.Equal(footer.Length, BitConverter.ToUInt64(bytes, (int)endHeaderOffset + 8));

        var rrdFooter = RerunLogMsg.RrdFooter.Parser.ParseFrom(footerPayload);
        Assert.Single(rrdFooter.Manifests);
        Assert.NotNull(rrdFooter.Manifests[0].StoreId);
        Assert.NotNull(rrdFooter.Manifests[0].SorbetSchema);
        Assert.NotNull(rrdFooter.Manifests[0].Data);
        Assert.True(rrdFooter.Manifests[0].HasSorbetSchemaSha256);
        Assert.Equal(32, rrdFooter.Manifests[0].SorbetSchemaSha256.Length);
    }

    [Fact]
    public void Backend_flush_does_not_finalize_rrd()
    {
        var bytes = WriteScalarRrd(callFlushBeforeShutdown: true);

        var footer = ReadStreamFooter(bytes);
        Assert.True(footer.Start > StreamHeaderSize);
        Assert.True(footer.Length > 0);
    }

    private static byte[] WriteScalarRrd(bool callFlushBeforeShutdown)
    {
        var encoder = new ManagedRerunEncoder();
        const string recordingId = "phase9-footer-test";
        const string appId = "unity2rerun_phase9";

        using var ms = new MemoryStream();
        using var writer = new RrdWriter(ms);
        var backend = new RrdRerunBackend(writer);
        var runtime = new RerunRuntime(appId, backend);

        backend.Initialize(runtime);
        backend.Write(encoder.EncodeSetStoreInfoMessage(recordingId, appId));
        backend.Write(encoder.EncodeScalarMessage(recordingId, appId, "metrics/fps", 60.0, Timeline));

        if (callFlushBeforeShutdown)
        {
            backend.Flush();
            backend.Write(encoder.EncodeScalarMessage(recordingId, appId, "metrics/fps", 61.0, Timeline));
        }

        backend.Shutdown();
        return ms.ToArray();
    }

    private static (ulong Start, ulong Length, uint Crc) ReadStreamFooter(byte[] bytes)
    {
        Assert.True(bytes.Length >= RrdConstants.StreamFooterFixedSize);

        var fixedOffset = bytes.Length - 12;
        Assert.Equal((byte)'R', bytes[fixedOffset]);
        Assert.Equal((byte)'R', bytes[fixedOffset + 1]);
        Assert.Equal((byte)'F', bytes[fixedOffset + 2]);
        Assert.Equal((byte)'2', bytes[fixedOffset + 3]);
        Assert.Equal((byte)'F', bytes[fixedOffset + 4]);
        Assert.Equal((byte)'O', bytes[fixedOffset + 5]);
        Assert.Equal((byte)'O', bytes[fixedOffset + 6]);
        Assert.Equal((byte)'T', bytes[fixedOffset + 7]);
        Assert.Equal(1u, BitConverter.ToUInt32(bytes, fixedOffset + 8));

        var dynamicOffset = bytes.Length - RrdConstants.StreamFooterFixedSize;
        var start = BitConverter.ToUInt64(bytes, dynamicOffset);
        var len = BitConverter.ToUInt64(bytes, dynamicOffset + 8);
        var crc = BitConverter.ToUInt32(bytes, dynamicOffset + 16);
        return (start, len, crc);
    }
}

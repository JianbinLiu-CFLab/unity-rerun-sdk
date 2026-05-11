// Phase 10 RRD writer - interactive parity smoke including Points3D.
using System.Collections.Generic;
using System.IO;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;

public static class Phase10RrdWriter
{
    public static void WritePhase10Rrd(string path)
    {
        var encoder = new ManagedRerunEncoder();
        var recordingId = "phase10-test";
        var appId = "unity2rerun_phase10";

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new RrdWriter(fs);
        var backend = new RrdRerunBackend(writer);
        var runtime = new RerunRuntime(appId, backend);

        backend.Initialize(runtime);

        void WriteMsg(EncodedRerunMessage m) => backend.Write(m);

        WriteMsg(encoder.EncodeSetStoreInfoMessage(recordingId, appId));
        WriteMsg(encoder.EncodeViewCoordinatesMessage(recordingId, appId, "world", 3, 1, 6));

        var timelines = new List<RerunTimelineEntry>
        {
            new("frame", 1, RerunTimelineKind.Sequence)
        };

        WriteMsg(encoder.EncodeEncodedImageMessage(
            recordingId, appId, "camera/main", OnePixelPng, "image/png", timelines));

        WriteMsg(encoder.EncodeScalarMessage(
            recordingId, appId, "metrics/interactive/command_count", 2, timelines));

        WriteMsg(encoder.EncodeScalarMessage(
            recordingId, appId, "metrics/interactive/fps", 60, timelines));

        WriteMsg(encoder.EncodeTransform3DMessage(
            recordingId, appId, "world/cube",
            0.5f, 0.35f, -0.04f,
            0f, 0f, 0f, 1f,
            timelines));

        var box = new RerunBox3D(
            new RerunVec3(0f, 0f, 0f),
            new RerunVec3(0.5f, 0.5f, 0.5f),
            new RerunQuat(0f, 0f, 0f, 1f),
            0x00FF00FF);
        WriteMsg(encoder.EncodeBoxes3DMessage(
            recordingId, appId, "world/cube", new[] { box }, timelines));

        var strip = new RerunLineStrip3D(
            new[]
            {
                new RerunVec3(0f, 0f, 0f),
                new RerunVec3(1f, 0.5f, 0f),
                new RerunVec3(1f, 1f, 0.5f)
            },
            0xFFAA00FF);
        WriteMsg(encoder.EncodeLineStrips3DMessage(
            recordingId, appId, "world/cube_trajectory", new[] { strip }, timelines));

        var points = new[]
        {
            new RerunPoint3D(new RerunVec3(-0.5f, 0.0f, -0.5f), 0x33CCFFFF, 0.05f),
            new RerunPoint3D(new RerunVec3(0.0f, 0.5f, 0.0f), 0x33CCFFFF, 0.05f),
            new RerunPoint3D(new RerunVec3(0.5f, 0.0f, 0.5f), 0x33CCFFFF, 0.05f)
        };
        WriteMsg(encoder.EncodePoints3DMessage(
            recordingId, appId, "world/points", points, timelines));

        backend.Shutdown();
    }

    private static readonly byte[] OnePixelPng =
    {
        137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82,
        0, 0, 0, 1, 0, 0, 0, 1, 8, 6, 0, 0, 0, 31, 21, 196,
        137, 0, 0, 0, 13, 73, 68, 65, 84, 120, 156, 99, 0, 1, 0,
        0, 5, 0, 1, 13, 10, 45, 180, 0, 0, 0, 0, 73, 69, 78,
        68, 174, 66, 96, 130
    };
}

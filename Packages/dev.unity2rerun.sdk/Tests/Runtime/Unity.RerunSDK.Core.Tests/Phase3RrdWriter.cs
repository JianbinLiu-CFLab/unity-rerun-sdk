// Phase 3 RRD writer — TextLog + Scalars + Transform3D + ViewCoordinates smoke.
using System;
using System.Collections.Generic;
using System.IO;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;

public static class Phase3RrdWriter
{
    public static void WritePhase3Rrd(string path)
    {
        var encoder = new ManagedRerunEncoder();
        var recordingId = "phase3-test";
        var appId = "unity2rerun_phase3";

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new RrdWriter(fs);

        writer.WriteStreamHeader();

        void WriteMsg(EncodedRerunMessage m) => writer.WriteMessage(m.RrdKind, m.RrdPayload);

        WriteMsg(encoder.EncodeSetStoreInfoMessage(recordingId, appId));
        WriteMsg(encoder.EncodeViewCoordinatesMessage(recordingId, appId, "world", 3, 1, 6));

        for (int i = 0; i < 100; i++)
        {
            var timelines = new List<RerunTimelineEntry>
            {
                new("log_tick", i, RerunTimelineKind.Sequence),
                new("frame", i, RerunTimelineKind.Sequence)
            };

            if (i % 20 == 0)
            {
                WriteMsg(encoder.EncodeTextLogMessage(recordingId, appId, "logs/unity",
                    $"Frame {i}: hello from Phase 3", "INFO", timelines));
            }

            WriteMsg(encoder.EncodeScalarMessage(recordingId, appId, "metrics/fps",
                60.0 + (i % 10), timelines));

            var t = i * 0.1;
            WriteMsg(encoder.EncodeTransform3DMessage(recordingId, appId, "world/cube",
                (float)Math.Sin(t), 2f, (float)Math.Cos(t),
                0f, 0f, 0f, 1f, timelines));
        }
    }
}

// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Writes deterministic Phase 13 LZ4-compressed RRD smoke recordings.

using System.Collections.Generic;
using System.IO;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;

/// <summary>
/// Writes the Phase 13 LZ4 RRD smoke recording.
/// </summary>
public static class Phase13RrdWriter
{
    public static void WritePhase13Lz4Rrd(string path)
    {
        var encoder = new ManagedRerunEncoder(RerunRecordingCompression.Lz4);
        const string recordingId = "phase13-lz4-test";
        const string appId = "unity2rerun_phase13";

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new RrdWriter(fs);
        var backend = new RrdRerunBackend(writer);
        var runtime = new RerunRuntime(appId, backend);

        backend.Initialize(runtime);

        void WriteMsg(EncodedRerunMessage message) => backend.Write(message);

        WriteMsg(encoder.EncodeSetStoreInfoMessage(recordingId, appId));
        WriteMsg(encoder.EncodeViewCoordinatesMessage(recordingId, appId, "world", 3, 1, 6));

        var timelines = new List<RerunTimelineEntry>
        {
            new("frame", 13, RerunTimelineKind.Sequence)
        };

        WriteMsg(encoder.EncodeTextLogMessage(
            recordingId, appId, "logs/phase13",
            "Phase 13 LZ4 smoke recording", "INFO", timelines));
        WriteMsg(encoder.EncodeScalarMessage(
            recordingId, appId, "metrics/phase13_fps", 60.0, timelines));
        WriteMsg(encoder.EncodeTransform3DMessage(
            recordingId, appId, "world/phase13_cube",
            1f, 2f, 3f,
            0f, 0f, 0f, 1f,
            timelines));

        var points = new[]
        {
            new RerunPoint3D(new RerunVec3(-1f, 0f, 0f), 0xFF3355FF, 0.05f),
            new RerunPoint3D(new RerunVec3(0f, 0.5f, 0f), 0x33CC88FF, 0.06f),
            new RerunPoint3D(new RerunVec3(1f, 0f, 0f), 0x3399FFFF, 0.05f),
        };
        WriteMsg(encoder.EncodePoints3DMessage(
            recordingId, appId, "world/phase13_points", points, timelines));

        backend.Shutdown();
    }
}

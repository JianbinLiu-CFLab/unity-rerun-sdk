// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Writes deterministic Phase 14 RRD compression comparison recordings.

using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;

/// <summary>
/// Writes Phase 14 None/LZ4 RRD comparison smoke recordings.
/// </summary>
public static class Phase14RrdWriter
{
    public static Phase14CompressionComparison WriteCompressionComparison(string outputPrefix)
    {
        var fullPrefix = Path.GetFullPath(outputPrefix);
        var directory = Path.GetDirectoryName(fullPrefix);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var nonePath = fullPrefix + "_none.rrd";
        var lz4Path = fullPrefix + "_lz4.rrd";

        WritePhase14Rrd(nonePath, RerunRecordingCompression.None);
        WritePhase14Rrd(lz4Path, RerunRecordingCompression.Lz4);

        return new Phase14CompressionComparison(
            nonePath,
            lz4Path,
            RrdInspector.InspectFile(nonePath),
            RrdInspector.InspectFile(lz4Path));
    }

    public static void WritePhase14Rrd(string path, RerunRecordingCompression compression)
    {
        var encoder = new ManagedRerunEncoder(compression);
        const string recordingId = "phase14-compression-test";
        const string appId = "unity2rerun_phase14";

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

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
            new("frame", 14, RerunTimelineKind.Sequence)
        };

        WriteMsg(encoder.EncodeTextLogMessage(
            recordingId, appId, "logs/phase14",
            "Phase 14 compression comparison smoke recording", "INFO", timelines));
        WriteMsg(encoder.EncodeScalarMessage(
            recordingId, appId, "metrics/phase14_fps", 60.0, timelines));
        WriteMsg(encoder.EncodeTransform3DMessage(
            recordingId, appId, "world/phase14_cube",
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
            recordingId, appId, "world/phase14_points", points, timelines));

        backend.Shutdown();
    }

    public static string FormatComparisonSummary(Phase14CompressionComparison comparison)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Phase14 Compression Comparison");
        builder.AppendLine();
        builder.AppendLine("None Recording");
        builder.AppendLine(RrdInspector.FormatSummary(comparison.NoneResult));
        builder.AppendLine();
        builder.AppendLine("LZ4 Recording");
        builder.Append(RrdInspector.FormatSummary(comparison.Lz4Result));
        return builder.ToString();
    }
}

/// <summary>
/// Paths and inspection results for a Phase 14 compression comparison run.
/// </summary>
public sealed record Phase14CompressionComparison(
    string NonePath,
    string Lz4Path,
    RrdInspectionResult NoneResult,
    RrdInspectionResult Lz4Result);

// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Exercises Phase11 Rrd Writer behavior for release and regression validation.

// Phase11 RRD writer - sensor typed publisher smoke with Pinhole + scan/point cloud.
using System.Collections.Generic;
using System.IO;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;
/// <summary>
/// Regression tests for Phase11 RRD Writer.
/// </summary>
public static class Phase11RrdWriter
{
    public static void WritePhase11Rrd(string path)
    {
        var encoder = new ManagedRerunEncoder();
        var recordingId = "phase11-test";
        var appId = "unity2rerun_phase11";

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

        WriteMsg(encoder.EncodePinholeMessage(
            recordingId, appId, "world/camera",
            RerunPinhole.FromVerticalFov(640, 480, 60f)));
        WriteMsg(encoder.EncodeEncodedImageMessage(
            recordingId, appId, "world/camera", OnePixelPng, "image/png", timelines));
        WriteMsg(encoder.EncodeTransform3DMessage(
            recordingId, appId, "world/camera",
            0f, 1.4f, -3f,
            0f, 0f, 0f, 1f,
            timelines));

        WriteMsg(encoder.EncodeScalarMessage(
            recordingId, appId, "metrics/sensors/frame_count", 1, timelines));

        var cloud = new[]
        {
            new RerunPoint3D(new RerunVec3(-0.75f, 0.0f, -0.75f), 0x33CCFFFF, 0.05f),
            new RerunPoint3D(new RerunVec3(-0.35f, 0.2f, -0.2f), 0x33CCFFFF, 0.05f),
            new RerunPoint3D(new RerunVec3(0.0f, 0.5f, 0.0f), 0x33CCFFFF, 0.06f),
            new RerunPoint3D(new RerunVec3(0.35f, 0.2f, 0.25f), 0x33CCFFFF, 0.05f),
            new RerunPoint3D(new RerunVec3(0.75f, 0.0f, 0.75f), 0x33CCFFFF, 0.05f),
        };
        WriteMsg(encoder.EncodePoints3DMessage(
            recordingId, appId, "world/point_cloud", cloud, timelines));

        var ranges = new[] { 2.5f, 2.1f, 1.8f, 1.6f, 1.8f, 2.1f, 2.5f };
        var scanLocal = RerunLaserScanProjection.ProjectToXz(
            ranges, -60f * MathConstants.Deg2Rad, 20f * MathConstants.Deg2Rad, 0.05f, 4f);

        var scanPoints = new List<RerunPoint3D>(scanLocal.Count);
        for (var i = 0; i < scanLocal.Count; i++)
            scanPoints.Add(new RerunPoint3D(scanLocal[i], 0x22BBFFFF, 0.04f));
        WriteMsg(encoder.EncodePoints3DMessage(
            recordingId, appId, "world/laser_scan", scanPoints, timelines));

        var scanStrip = new RerunLineStrip3D(scanLocal, 0x22BBFFCC);
        WriteMsg(encoder.EncodeLineStrips3DMessage(
            recordingId, appId, "world/laser_scan_outline", new[] { scanStrip }, timelines));

        backend.Shutdown();
    }
    /// <summary>
    /// Regression tests for Math Constants.
    /// </summary>
    private static class MathConstants
    {
        /// <summary>
        /// Conversion factor from degrees to radians for phase smoke geometry.
        /// </summary>
        public const float Deg2Rad = 0.017453292519943295f;
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

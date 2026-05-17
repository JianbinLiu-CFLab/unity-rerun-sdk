// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Exercises Phase11 Sensor Tests behavior for release and regression validation.

using System;
using System.IO;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using Unity.RerunSDK.Encoding;
using Xunit;
using RerunLogMsg = Rerun.LogMsg.V1Alpha1;
/// <summary>
/// Regression tests for Phase11 Sensor Tests.
/// </summary>
public class Phase11SensorTests
{
    [Fact]
    public void Pinhole_projection_from_vertical_fov_uses_expected_intrinsics()
    {
        var pinhole = RerunPinhole.FromVerticalFov(
            width: 640,
            height: 480,
            verticalFovDegrees: 60f,
            imagePlaneDistance: 0.1f,
            colorRgba: 0x33AAFFFF,
            lineWidth: 0.003f);

        Assert.Equal(640, pinhole.Width);
        Assert.Equal(480, pinhole.Height);
        Assert.Equal(320f, pinhole.Cx);
        Assert.Equal(240f, pinhole.Cy);
        Assert.Equal(pinhole.Fy, pinhole.Fx);
        Assert.InRange(pinhole.Fy, 415.6f, 415.8f);
    }

    [Fact]
    public void Pinhole_schema_uses_projection_resolution_camera_xyz_and_wireframe_components()
    {
        var pinhole = new RerunPinhole(
            width: 640,
            height: 480,
            fx: 415.69f,
            fy: 415.69f,
            cx: 320f,
            cy: 240f,
            imagePlaneDistance: 0.1f,
            colorRgba: 0x33AAFFFF,
            lineWidth: 0.003f);

        var schema = DecodeSchema(enc => enc.EncodePinholeMessage(
            "rec", "app", "world/camera", pinhole));

        AssertFixedSizeFloatListField(schema, "Pinhole:image_from_camera", "rerun.components.PinholeProjection", 9);
        AssertFixedSizeFloatListField(schema, "Pinhole:resolution", "rerun.components.Resolution", 2);

        var cameraXyz = schema.GetFieldByName("Pinhole:camera_xyz");
        Assert.NotNull(cameraXyz);
        Assert.Equal("rerun.archetypes.Pinhole", cameraXyz.Metadata["rerun:archetype"]);
        Assert.Equal("rerun.components.ViewCoordinates", cameraXyz.Metadata["rerun:component_type"]);
        var cameraXyzList = Assert.IsType<ListType>(cameraXyz.DataType);
        var cameraXyzFsl = Assert.IsType<FixedSizeListType>(cameraXyzList.ValueDataType);
        Assert.Equal(3, cameraXyzFsl.ListSize);
        Assert.IsType<UInt8Type>(cameraXyzFsl.ValueField.DataType);

        AssertScalarListField<FloatType>(schema, "Pinhole:image_plane_distance", "rerun.components.ImagePlaneDistance");
        AssertScalarListField<UInt32Type>(schema, "Pinhole:color", "rerun.components.Color");
        AssertScalarListField<FloatType>(schema, "Pinhole:line_width", "rerun.components.Radius");
    }

    [Fact]
    public void Pinhole_message_is_static_and_manifest_components_are_static()
    {
        var pinhole = new RerunPinhole(640, 480, 415.69f, 415.69f, 320f, 240f);
        var msg = new ManagedRerunEncoder().EncodePinholeMessage(
            "rec", "app", "world/camera", pinhole);

        Assert.True(msg.IsStatic);
        Assert.NotNull(msg.ManifestChunkInfo);
        Assert.True(msg.ManifestChunkInfo!.IsStatic);
        Assert.All(msg.ManifestChunkInfo.Components, c => Assert.True(c.IsStatic));
        Assert.Contains(msg.ManifestChunkInfo.Components, c => c.Component == "Pinhole:image_from_camera");
    }

    [Fact]
    public void Laser_scan_projection_skips_invalid_ranges_and_maps_to_xz_plane()
    {
        var points = RerunLaserScanProjection.ProjectToXz(
            new[] { 1f, 2f, float.NaN, 0.05f, 4f },
            angleMinRadians: 0f,
            angleIncrementRadians: MathF.PI * 0.5f,
            rangeMin: 0.1f,
            rangeMax: 3f);

        Assert.Equal(2, points.Count);
        Assert.InRange(points[0].X, 0.999f, 1.001f);
        Assert.Equal(0f, points[0].Y);
        Assert.InRange(points[0].Z, -0.001f, 0.001f);
        Assert.InRange(points[1].X, -0.001f, 0.001f);
        Assert.Equal(0f, points[1].Y);
        Assert.InRange(points[1].Z, 1.999f, 2.001f);
    }

    private delegate EncodedRerunMessage EncFn(ManagedRerunEncoder enc);

    private static Apache.Arrow.Schema DecodeSchema(EncFn encode)
    {
        var msg = encode(new ManagedRerunEncoder());
        var arrowMsg = RerunLogMsg.ArrowMsg.Parser.ParseFrom(msg.RrdPayload);
        using var ms = new MemoryStream(arrowMsg.Payload.ToByteArray());
        using var reader = new ArrowStreamReader(ms);
        var batch = reader.ReadNextRecordBatch();
        Assert.NotNull(batch);
        return batch.Schema;
    }

    private static void AssertFixedSizeFloatListField(
        Apache.Arrow.Schema schema,
        string name,
        string componentType,
        int listSize)
    {
        var field = schema.GetFieldByName(name);
        Assert.NotNull(field);
        Assert.Equal("rerun.archetypes.Pinhole", field.Metadata["rerun:archetype"]);
        Assert.Equal(componentType, field.Metadata["rerun:component_type"]);
        var list = Assert.IsType<ListType>(field.DataType);
        var fsl = Assert.IsType<FixedSizeListType>(list.ValueDataType);
        Assert.Equal(listSize, fsl.ListSize);
        Assert.IsType<FloatType>(fsl.ValueField.DataType);
    }

    private static void AssertScalarListField<TArrowType>(
        Apache.Arrow.Schema schema,
        string name,
        string componentType)
        where TArrowType : IArrowType
    {
        var field = schema.GetFieldByName(name);
        Assert.NotNull(field);
        Assert.Equal("rerun.archetypes.Pinhole", field.Metadata["rerun:archetype"]);
        Assert.Equal(componentType, field.Metadata["rerun:component_type"]);
        var list = Assert.IsType<ListType>(field.DataType);
        Assert.IsType<TArrowType>(list.ValueDataType);
    }
}

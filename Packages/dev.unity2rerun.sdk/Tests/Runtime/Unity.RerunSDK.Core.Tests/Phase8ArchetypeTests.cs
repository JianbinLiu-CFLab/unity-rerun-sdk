// Phase 8 schema tests for image and 3D archetypes.

using System.Collections.Generic;
using System.IO;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Xunit;
using RerunLogMsg = Rerun.LogMsg.V1Alpha1;

public class Phase8ArchetypeTests
{
    private static readonly List<RerunTimelineEntry> DefTl = new() {
        new("frame", 8, RerunTimelineKind.Sequence) };

    [Fact]
    public void EncodedImage_schema_has_blob_and_media_type_components()
    {
        var schema = DecodeSchema(enc => enc.EncodeEncodedImageMessage(
            "rec", "app", "camera/main", new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg", DefTl));

        var blob = schema.GetFieldByName("EncodedImage:blob");
        Assert.NotNull(blob);
        Assert.Equal("rerun.archetypes.EncodedImage", blob.Metadata["rerun:archetype"]);
        Assert.Equal("rerun.components.Blob", blob.Metadata["rerun:component_type"]);
        var blobList = Assert.IsType<ListType>(blob.DataType);
        var blobValue = Assert.IsType<ListType>(blobList.ValueDataType);
        Assert.IsType<UInt8Type>(blobValue.ValueDataType);

        var mediaType = schema.GetFieldByName("EncodedImage:media_type");
        Assert.NotNull(mediaType);
        Assert.Equal("rerun.components.MediaType", mediaType.Metadata["rerun:component_type"]);
        var mediaList = Assert.IsType<ListType>(mediaType.DataType);
        Assert.Equal(StringType.Default.TypeId, mediaList.ValueDataType.TypeId);
    }

    [Fact]
    public void Boxes3D_schema_uses_half_sizes_center_quaternion_and_color()
    {
        var box = new RerunBox3D(
            new RerunVec3(1f, 2f, 3f),
            new RerunVec3(0.5f, 1f, 1.5f),
            new RerunQuat(0f, 0f, 0f, 1f),
            0x00FF00FF);

        var schema = DecodeSchema(enc => enc.EncodeBoxes3DMessage(
            "rec", "app", "world/cube", new[] { box }, DefTl));

        AssertVector3ListField(schema, "Boxes3D:half_sizes", "rerun.components.HalfSize3D");
        AssertVector3ListField(schema, "Boxes3D:centers", "rerun.components.Translation3D");

        var quaternion = schema.GetFieldByName("Boxes3D:quaternions");
        Assert.NotNull(quaternion);
        Assert.Equal("rerun.components.RotationQuat", quaternion.Metadata["rerun:component_type"]);
        var quatList = Assert.IsType<ListType>(quaternion.DataType);
        var quatFsl = Assert.IsType<FixedSizeListType>(quatList.ValueDataType);
        Assert.Equal(4, quatFsl.ListSize);

        var colors = schema.GetFieldByName("Boxes3D:colors");
        Assert.NotNull(colors);
        Assert.Equal("rerun.components.Color", colors.Metadata["rerun:component_type"]);
        var colorList = Assert.IsType<ListType>(colors.DataType);
        Assert.IsType<UInt32Type>(colorList.ValueDataType);
    }

    [Fact]
    public void LineStrips3D_schema_uses_line_strip_points_and_single_color()
    {
        var strip = new RerunLineStrip3D(
            new[] { new RerunVec3(0f, 0f, 0f), new RerunVec3(1f, 2f, 3f) },
            0xFFAA00FF);

        var schema = DecodeSchema(enc => enc.EncodeLineStrips3DMessage(
            "rec", "app", "world/cube_trajectory", new[] { strip }, DefTl));

        var strips = schema.GetFieldByName("LineStrips3D:strips");
        Assert.NotNull(strips);
        Assert.Equal("rerun.archetypes.LineStrips3D", strips.Metadata["rerun:archetype"]);
        Assert.Equal("rerun.components.LineStrip3D", strips.Metadata["rerun:component_type"]);
        var stripList = Assert.IsType<ListType>(strips.DataType);
        var lineStrip = Assert.IsType<ListType>(stripList.ValueDataType);
        var points = Assert.IsType<FixedSizeListType>(lineStrip.ValueDataType);
        Assert.Equal(3, points.ListSize);

        var colors = schema.GetFieldByName("LineStrips3D:colors");
        Assert.NotNull(colors);
        Assert.Equal("rerun.components.Color", colors.Metadata["rerun:component_type"]);
        var colorList = Assert.IsType<ListType>(colors.DataType);
        Assert.IsType<UInt32Type>(colorList.ValueDataType);
    }

    [Fact]
    public void Phase8_smoke_rrd_places_box_geometry_on_cube_entity()
    {
        var path = Path.Combine(Path.GetTempPath(), $"phase8_{Guid.NewGuid():N}.rrd");
        try
        {
            Phase8RrdWriter.WritePhase8Rrd(path);

            var boxes3DEntityPaths = ReadBoxes3DEntityPaths(path);
            Assert.Contains("world/cube", boxes3DEntityPaths);
            Assert.DoesNotContain("world/cube_box", boxes3DEntityPaths);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
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

    private static void AssertVector3ListField(Apache.Arrow.Schema schema, string name, string componentType)
    {
        var field = schema.GetFieldByName(name);
        Assert.NotNull(field);
        Assert.Equal(componentType, field.Metadata["rerun:component_type"]);
        var list = Assert.IsType<ListType>(field.DataType);
        var fsl = Assert.IsType<FixedSizeListType>(list.ValueDataType);
        Assert.Equal(3, fsl.ListSize);
    }

    private static List<string> ReadBoxes3DEntityPaths(string path)
    {
        var result = new List<string>();
        using var fs = File.OpenRead(path);
        var streamHeader = new byte[12];
        fs.ReadExactly(streamHeader);

        var messageHeader = new byte[16];
        while (fs.Position < fs.Length)
        {
            fs.ReadExactly(messageHeader);
            var kind = BitConverter.ToUInt64(messageHeader, 0);
            var len = BitConverter.ToUInt64(messageHeader, 8);
            var payload = new byte[len];
            fs.ReadExactly(payload);

            if (kind == Unity.RerunSDK.IO.Rrd.RrdConstants.MsgKindEnd)
                break;

            if (kind != Unity.RerunSDK.IO.Rrd.RrdConstants.MsgKindArrowMsg)
                continue;

            var arrowMsg = RerunLogMsg.ArrowMsg.Parser.ParseFrom(payload);
            using var ms = new MemoryStream(arrowMsg.Payload.ToByteArray());
            using var reader = new ArrowStreamReader(ms);
            var batch = reader.ReadNextRecordBatch();
            Assert.NotNull(batch);

            var schema = batch.Schema;
            if (schema.GetFieldByName("Boxes3D:half_sizes") == null)
                continue;

            Assert.True(schema.Metadata.TryGetValue("rerun:entity_path", out var entityPath));
            result.Add(entityPath);
        }

        return result;
    }
}

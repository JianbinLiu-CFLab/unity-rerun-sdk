// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Exercises Rrd Smoke Tests behavior for release and regression validation.

// RRD smoke tests with Arrow schema-level validation.
using System.Collections.Generic;
using System.IO;
using Apache.Arrow;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.IO.Rrd;
using Xunit;
using RerunLogMsg = Rerun.LogMsg.V1Alpha1;
/// <summary>
/// Regression tests for RRD Smoke Tests.
/// </summary>
public class RrdSmokeTests
{
    private static readonly List<RerunTimelineEntry> DefTl = new() {
        new("log_tick", 1, RerunTimelineKind.Sequence) };

    [Fact]
    public void Write_minimal_rrd_with_textlog()
    {
        var encoder = new ManagedRerunEncoder();
        var recordingId = "test-recording";
        var appId = "test_app";

        using var ms = new MemoryStream();
        using (var writer = new RrdWriter(ms))
        {
            writer.WriteStreamHeader();
            var ssi = encoder.EncodeSetStoreInfoMessage(recordingId, appId);
            writer.WriteMessage(ssi.RrdKind, ssi.RrdPayload);

            var msg = encoder.EncodeTextLogMessage(
                recordingId, appId, "logs/unity", "hello test", "INFO", DefTl);
            writer.WriteMessage(msg.RrdKind, msg.RrdPayload);
        }

        var bytes = ms.ToArray();
        Assert.True(bytes.Length > 100);
        Assert.Equal((byte)'R', bytes[0]);
        Assert.Equal((byte)'R', bytes[1]);
        Assert.Equal((byte)'F', bytes[2]);
        Assert.Equal((byte)'2', bytes[3]);
    }

    [Fact]
    public void Scalar_schema_uses_float64()
    {
        var schema = DecodeSchema(enc => enc.EncodeScalarMessage("rec", "app", "m/fps", 60.0, DefTl));
        var sf = schema.GetFieldByName("Scalars:scalars");
        Assert.NotNull(sf);
        Assert.IsType<ListType>(sf.DataType);
        Assert.Equal(DoubleType.Default.TypeId, ((ListType)sf.DataType).ValueDataType.TypeId);
        Assert.Equal("rerun.archetypes.Scalars", sf.Metadata["rerun:archetype"]);
        Assert.Equal("rerun.components.Scalar", sf.Metadata["rerun:component_type"]);
    }

    [Fact]
    public void ViewCoordinates_component_is_xyz()
    {
        var schema = DecodeSchema(enc => enc.EncodeViewCoordinatesMessage("rec", "app", "world", 3, 1, 6));
        var vf = schema.GetFieldByName("ViewCoordinates:xyz");
        Assert.NotNull(vf);
        Assert.Equal("rerun.archetypes.ViewCoordinates", vf.Metadata["rerun:archetype"]);
        Assert.Equal("rerun.components.ViewCoordinates", vf.Metadata["rerun:component_type"]);
        Assert.Equal("true", vf.Metadata["rerun:is_static"]);
        var vcList = Assert.IsType<ListType>(vf.DataType);
        Assert.IsType<FixedSizeListType>(vcList.ValueDataType);
    }

    [Fact]
    public void Transform3D_schema_has_translation_and_quaternion()
    {
        var schema = DecodeSchema(enc => enc.EncodeTransform3DMessage("rec", "app", "w/cube",
            1f, 2f, 3f, 0f, 0f, 0f, 1f, DefTl));
        var tf = schema.GetFieldByName("Transform3D:translation");
        Assert.NotNull(tf);
        Assert.Equal("rerun.components.Translation3D", tf.Metadata["rerun:component_type"]);
        var tList = Assert.IsType<ListType>(tf.DataType);
        var tFsl = Assert.IsType<FixedSizeListType>(tList.ValueDataType);
        Assert.Equal(3, tFsl.ListSize);

        var qf = schema.GetFieldByName("Transform3D:quaternion");
        Assert.NotNull(qf);
        Assert.Equal("rerun.components.RotationQuat", qf.Metadata["rerun:component_type"]);
        var qList = Assert.IsType<ListType>(qf.DataType);
        var qFsl = Assert.IsType<FixedSizeListType>(qList.ValueDataType);
        Assert.Equal(4, qFsl.ListSize);
    }

    [Fact]
    public void Row_id_has_control_kind_and_arrow_extension()
    {
        var schema = DecodeSchema(enc => enc.EncodeTextLogMessage("rec", "app", "logs/u", "hi", "INFO", DefTl));
        var rf = schema.GetFieldByName("row_id");
        Assert.NotNull(rf);
        Assert.Equal("control", rf.Metadata["rerun:kind"]);
        Assert.Equal("rerun.datatypes.TUID", rf.Metadata["ARROW:extension:name"]);
    }

    [Fact]
    public void Batch_metadata_contains_sorbet_version_and_rerun_id()
    {
        var schema = DecodeSchema(enc => enc.EncodeTextLogMessage("rec", "app", "logs/u", "hi", "INFO", DefTl));
        Assert.Equal("0.1.3", schema.Metadata["sorbet:version"]);
        Assert.Equal("logs/u", schema.Metadata["rerun:entity_path"]);
        Assert.True(schema.Metadata.ContainsKey("rerun:id"));
        Assert.Equal(32, schema.Metadata["rerun:id"].Length);
    }

    [Fact]
    public void Timeline_timestamp_uses_nanosecond_type()
    {
        var tls = new List<RerunTimelineEntry> {
            new("log_time", 1234567890123, RerunTimelineKind.TimestampNs),
            new("log_tick", 1, RerunTimelineKind.Sequence)
        };
        var schema = DecodeSchema(enc => enc.EncodeScalarMessage("rec", "app", "m/x", 1.0, tls));

        var tf = schema.GetFieldByName("log_time");
        Assert.NotNull(tf);
        var tsType = Assert.IsType<TimestampType>(tf.DataType);
        Assert.Equal(TimeUnit.Nanosecond, tsType.Unit);

        var lf = schema.GetFieldByName("log_tick");
        Assert.NotNull(lf);
        Assert.IsType<Int64Type>(lf.DataType);
    }

    [Fact]
    public void TUID_ids_are_unique_across_calls()
    {
        var ids = new HashSet<string>();
        var encoder = new ManagedRerunEncoder();
        for (int i = 0; i < 50; i++)
        {
            var msg = encoder.EncodeTextLogMessage("rec", "app", "logs/u", $"msg{i}", "INFO", DefTl);
            var schema = DecodeSchemaFromBytes(msg);
            var id = schema.Metadata["rerun:id"];
            Assert.DoesNotContain(id, ids);
            ids.Add(id);
        }
    }

    // -- helpers --

    private delegate EncodedRerunMessage EncFn(ManagedRerunEncoder enc);

    private static Apache.Arrow.Schema DecodeSchema(EncFn encode)
    {
        return DecodeSchemaFromBytes(encode(new ManagedRerunEncoder()));
    }

    private static Apache.Arrow.Schema DecodeSchemaFromBytes(EncodedRerunMessage msg)
    {
        var arrowMsg = RerunLogMsg.ArrowMsg.Parser.ParseFrom(msg.RrdPayload);
        Assert.NotNull(arrowMsg.Payload);
        Assert.True(arrowMsg.Payload.Length > 0);

        using var ms = new MemoryStream(arrowMsg.Payload.ToByteArray());
        using var reader = new ArrowStreamReader(ms);
        var batch = reader.ReadNextRecordBatch();
        Assert.NotNull(batch);
        return batch.Schema;
    }
}

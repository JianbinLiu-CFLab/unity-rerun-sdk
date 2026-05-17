// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Exercises Grpc Wrapper Tests behavior for release and regression validation.

// Grpc LogMsg wrapper roundtrip tests
using Google.Protobuf;
using Unity.RerunSDK.Encoding;
using Xunit;
using RerunLogMsg = Rerun.LogMsg.V1Alpha1;
using RerunCommon = Rerun.Common.V1Alpha1;
using RerunSdkComms = Rerun.SdkComms.V1Alpha1;
/// <summary>
/// Regression tests for Grpc Wrapper Tests.
/// </summary>
public class GrpcWrapperTests
{
    [Fact]
    public void WrapSetStoreInfo_roundtrip_has_correct_oneof_case()
    {
        var encoder = new ManagedRerunEncoder();
        var msg = encoder.EncodeSetStoreInfoMessage("rec", "app");

        // Roundtrip via generated LogMsg parser
        var logMsg = RerunLogMsg.LogMsg.Parser.ParseFrom(msg.GrpcLogMsgBytes);
        Assert.Equal(RerunLogMsg.LogMsg.MsgOneofCase.SetStoreInfo, logMsg.MsgCase);

        // Inner payload roundtrips back
        var inner = RerunLogMsg.SetStoreInfo.Parser.ParseFrom(msg.RrdPayload);
        Assert.Equal("rec", inner.Info.StoreId.RecordingId);
    }

    [Fact]
    public void WrapArrowMsg_roundtrip_has_correct_oneof_case()
    {
        var encoder = new ManagedRerunEncoder();
        var defTl = new System.Collections.Generic.List<Unity.RerunSDK.Core.RerunTimelineEntry>
        {
            new("log_tick", 1, Unity.RerunSDK.Core.RerunTimelineKind.Sequence)
        };
        var msg = encoder.EncodeTextLogMessage("rec", "app", "logs/u", "hi", "INFO", defTl);

        var logMsg = RerunLogMsg.LogMsg.Parser.ParseFrom(msg.GrpcLogMsgBytes);
        Assert.Equal(RerunLogMsg.LogMsg.MsgOneofCase.ArrowMsg, logMsg.MsgCase);

        // Inner ArrowMsg roundtrips
        var inner = RerunLogMsg.ArrowMsg.Parser.ParseFrom(msg.RrdPayload);
        Assert.Equal("rec", inner.StoreId.RecordingId);
        Assert.Equal(RerunLogMsg.Encoding.ArrowIpc, inner.Encoding);
    }

    [Fact]
    public void EncodedRerunMessage_has_correct_fields()
    {
        var encoder = new ManagedRerunEncoder();
        var msg = encoder.EncodeSetStoreInfoMessage("rec", "app");

        Assert.Equal(Unity.RerunSDK.IO.Rrd.RrdConstants.MsgKindSetStoreInfo, msg.RrdKind);
        Assert.NotNull(msg.RrdPayload);
        Assert.True(msg.RrdPayload.Length > 0);
        Assert.NotNull(msg.GrpcLogMsgBytes);
        Assert.True(msg.GrpcLogMsgBytes.Length > msg.RrdPayload.Length); // outer > inner
        Assert.True(msg.IsStoreInfo);
        Assert.False(msg.IsStatic);
    }
}

// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Exercises Rerun Source Emitter Tests behavior for release and regression validation.

using System.Collections.Generic;
using Unity.RerunSDK.Editor;
using Xunit;
/// <summary>
/// Regression tests for Rerun Source Emitter Tests.
/// </summary>
public class RerunSourceEmitterTests
{
    [Fact]
    public void Emitter_generates_bridge_methods_without_lifecycle_hooks()
    {
        var source = RerunSourceEmitter.EmitClass(
            "Game.Debug",
            "PlayerDebug",
            new[]
            {
                new RerunSourceEmitter.LogEntry(
                    RerunSourceEmitter.LogKind.TextLog,
                    RerunSourceEmitter.MemberKind.Field,
                    "_status",
                    "string",
                    "logs/status",
                    1f,
                    "INFO"),
                new RerunSourceEmitter.LogEntry(
                    RerunSourceEmitter.LogKind.Scalar,
                    RerunSourceEmitter.MemberKind.Property,
                    "Speed",
                    "float",
                    "metrics/speed",
                    10f,
                    "INFO"),
                new RerunSourceEmitter.LogEntry(
                    RerunSourceEmitter.LogKind.Transform3D,
                    RerunSourceEmitter.MemberKind.ThisTransform,
                    "this",
                    "UnityEngine.Transform",
                    "world/player",
                    30f,
                    "INFO"),
            });

        Assert.Contains("partial class PlayerDebug : IRerunGeneratedLogSource", source);
        Assert.Contains("int RerunLog_EntryCount", source);
        Assert.Contains("RerunLog_GetEntry(int index)", source);
        Assert.Contains("RerunLog_Publish(int index, RerunManager manager)", source);
        Assert.DoesNotContain("void OnEnable()", source);
        Assert.DoesNotContain("void OnDisable()", source);
        Assert.DoesNotContain("void OnDestroy()", source);
        Assert.Contains("manager.LogText(\"logs/status\", this._status, \"INFO\")", source);
        Assert.Contains("manager.LogScalar(\"metrics/speed\", Convert.ToDouble(this.Speed", source);
        Assert.Contains("manager.LogTransform(\"world/player\", this.transform)", source);
    }

    [Fact]
    public void Emitter_rejects_invalid_entity_paths()
    {
        var entries = new List<RerunSourceEmitter.LogEntry>
        {
            new(
                RerunSourceEmitter.LogKind.TextLog,
                RerunSourceEmitter.MemberKind.Field,
                "_status",
                "string",
                "logs//bad",
                1f,
                "INFO"),
        };

        Assert.Throws<System.ArgumentException>(() =>
            RerunSourceEmitter.EmitClass("", "BadLog", entries));
    }
}

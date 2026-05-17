// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Tests/Runtime/Unity.RerunSDK.Core.Tests
// Purpose: Exercises Publisher Helper Tests behavior for release and regression validation.

// Publisher helper tests cover pure C# logic used by Unity components.

using Unity.RerunSDK.Unity;
using Xunit;
/// <summary>
/// Regression tests for Publisher Helper Tests.
/// </summary>
public class PublisherHelperTests
{
    [Fact]
    public void Rate_limiter_allows_first_publish_and_blocks_until_interval()
    {
        var limiter = new RerunPublishRateLimiter();

        Assert.True(limiter.CanPublish(10f, 0.0));
        Assert.False(limiter.CanPublish(10f, 0.05));
        Assert.True(limiter.CanPublish(10f, 0.1));
    }

    [Fact]
    public void Rate_limiter_allows_every_frame_when_rate_is_non_positive()
    {
        var limiter = new RerunPublishRateLimiter();

        Assert.True(limiter.CanPublish(0f, 0.0));
        Assert.True(limiter.CanPublish(0f, 0.0));
        Assert.True(limiter.CanPublish(-1f, 0.0));
    }

    [Fact]
    public void Scalar_source_evaluator_computes_core_sources()
    {
        Assert.Equal(60.0, RerunScalarSourceEvaluator.Evaluate(
            RerunScalarSource.Fps, 0.0, 1f / 60f, 0.02f, 12.0, 42, 0f), 3);
        Assert.Equal(16.0, RerunScalarSourceEvaluator.Evaluate(
            RerunScalarSource.DeltaTimeMs, 0.0, 0.016f, 0.02f, 12.0, 42, 0f), 3);
        Assert.Equal(20.0, RerunScalarSourceEvaluator.Evaluate(
            RerunScalarSource.UnscaledDeltaTimeMs, 0.0, 0.016f, 0.02f, 12.0, 42, 0f), 3);
        Assert.Equal(12.0, RerunScalarSourceEvaluator.Evaluate(
            RerunScalarSource.TimeSinceStartup, 0.0, 0.016f, 0.02f, 12.0, 42, 0f));
        Assert.Equal(42.0, RerunScalarSourceEvaluator.Evaluate(
            RerunScalarSource.FrameCount, 0.0, 0.016f, 0.02f, 12.0, 42, 0f));
        Assert.Equal(7.5, RerunScalarSourceEvaluator.Evaluate(
            RerunScalarSource.Constant, 7.5, 0.016f, 0.02f, 12.0, 42, 0f));
    }

    [Fact]
    public void Scalar_source_evaluator_identifies_transform_sources()
    {
        Assert.True(RerunScalarSourceEvaluator.IsTransformSource(RerunScalarSource.TransformPositionX));
        Assert.True(RerunScalarSourceEvaluator.IsTransformSource(RerunScalarSource.TransformRotationEulerZ));
        Assert.False(RerunScalarSourceEvaluator.IsTransformSource(RerunScalarSource.Fps));
    }
}

// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components/Publishing
// Purpose: Integrates managed Rerun logging with Unity runtime components.

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Enumerates supported Rerun Scalar Source values.
    /// </summary>
    public enum RerunScalarSource
    {
        Fps,
        DeltaTimeMs,
        UnscaledDeltaTimeMs,
        TimeSinceStartup,
        FrameCount,
        TransformPositionX,
        TransformPositionY,
        TransformPositionZ,
        TransformRotationEulerX,
        TransformRotationEulerY,
        TransformRotationEulerZ,
        Constant,
    }
}

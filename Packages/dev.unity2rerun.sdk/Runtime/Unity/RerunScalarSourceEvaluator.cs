// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Unity
// Purpose: Integrates managed Rerun logging with Unity runtime components.

// Pure C# scalar source evaluator, testable without UnityEngine.

using System;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Provides Rerun Scalar Source Evaluator support for Unity2Rerun.
    /// </summary>
    internal static class RerunScalarSourceEvaluator
    {
        /// Evaluate a scalar source and return the computed double value.
        /// For Transform-dependent sources, pass the evaluated position/rotation component.
        public static double Evaluate(RerunScalarSource source, double constantValue,
            float deltaTime, float unscaledDeltaTime, double realtimeSinceStartup,
            int frameCount, float transformValue)
        {
            return source switch
            {
                RerunScalarSource.Fps => deltaTime > 0f ? 1.0 / deltaTime : 0.0,
                RerunScalarSource.DeltaTimeMs => deltaTime * 1000.0,
                RerunScalarSource.UnscaledDeltaTimeMs => unscaledDeltaTime * 1000.0,
                RerunScalarSource.TimeSinceStartup => realtimeSinceStartup,
                RerunScalarSource.FrameCount => frameCount,
                RerunScalarSource.TransformPositionX => transformValue,
                RerunScalarSource.TransformPositionY => transformValue,
                RerunScalarSource.TransformPositionZ => transformValue,
                RerunScalarSource.TransformRotationEulerX => transformValue,
                RerunScalarSource.TransformRotationEulerY => transformValue,
                RerunScalarSource.TransformRotationEulerZ => transformValue,
                RerunScalarSource.Constant => constantValue,
                _ => constantValue,
            };
        }
        /// <summary>
        /// Handles the IsTransformSource workflow for this component.
        /// </summary>
        public static bool IsTransformSource(RerunScalarSource source)
        {
            switch (source)
            {
                case RerunScalarSource.TransformPositionX:
                case RerunScalarSource.TransformPositionY:
                case RerunScalarSource.TransformPositionZ:
                case RerunScalarSource.TransformRotationEulerX:
                case RerunScalarSource.TransformRotationEulerY:
                case RerunScalarSource.TransformRotationEulerZ:
                    return true;
                default:
                    return false;
            }
        }
    }
}

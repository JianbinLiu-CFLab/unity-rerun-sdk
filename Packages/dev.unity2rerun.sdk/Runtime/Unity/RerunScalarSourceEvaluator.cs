// SPDX-License-Identifier: Apache-2.0
//
// Pure C# scalar source evaluator, testable without UnityEngine.

using System;

namespace Unity.RerunSDK.Unity
{
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

// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Encoding
// Purpose: Defines managed Rerun encoding primitives used by RRD files and live transport.

using System;
using System.Collections.Generic;

namespace Unity.RerunSDK.Encoding
{
    /// <summary>
    /// Provides Rerun Laser Scan Projection support for Unity2Rerun.
    /// </summary>
    internal static class RerunLaserScanProjection
    {
        /// <summary>
        /// Handles the ProjectToXz workflow for this component.
        /// </summary>
        public static IReadOnlyList<RerunVec3> ProjectToXz(
            IReadOnlyList<float> ranges,
            float angleMinRadians,
            float angleIncrementRadians,
            float rangeMin,
            float rangeMax)
        {
            if (ranges == null || ranges.Count == 0)
                return Array.Empty<RerunVec3>();

            var points = new List<RerunVec3>(ranges.Count);
            var min = Math.Max(0f, rangeMin);
            var max = Math.Max(min, rangeMax);
            for (var i = 0; i < ranges.Count; i++)
            {
                var range = ranges[i];
                if (float.IsNaN(range) || float.IsInfinity(range) || range < min || range > max)
                    continue;

                var angle = angleMinRadians + i * angleIncrementRadians;
                points.Add(new RerunVec3(
                    (float)Math.Cos(angle) * range,
                    0f,
                    (float)Math.Sin(angle) * range));
            }

            return points;
        }
    }
}

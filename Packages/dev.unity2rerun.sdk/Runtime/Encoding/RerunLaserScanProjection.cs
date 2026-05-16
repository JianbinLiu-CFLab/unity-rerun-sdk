// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace Unity.RerunSDK.Encoding
{
    internal static class RerunLaserScanProjection
    {
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

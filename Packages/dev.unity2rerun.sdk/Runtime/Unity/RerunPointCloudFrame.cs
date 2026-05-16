// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    public readonly struct RerunPointCloudFrame
    {
        public RerunPointCloudFrame(
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<Color> colors = null,
            IReadOnlyList<float> radii = null)
        {
            Positions = positions;
            Colors = colors;
            Radii = radii;
        }

        public IReadOnlyList<Vector3> Positions { get; }
        public IReadOnlyList<Color> Colors { get; }
        public IReadOnlyList<float> Radii { get; }
    }
}

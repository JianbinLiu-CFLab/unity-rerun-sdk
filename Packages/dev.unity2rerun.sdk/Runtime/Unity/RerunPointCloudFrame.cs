// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Unity
// Purpose: Integrates managed Rerun logging with Unity runtime components.

using System.Collections.Generic;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Carries Rerun Point Cloud Frame data across Unity2Rerun runtime boundaries.
    /// </summary>
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

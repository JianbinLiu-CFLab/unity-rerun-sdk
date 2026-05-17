// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Unity2Rerun/Assets/Scripts/Validation
// Purpose: Provides a scene-level probe for validating generated RerunLog output.

using Unity.RerunSDK.Unity;
using UnityEngine;

[AddComponentMenu("Rerun/Validation/Generated RerunLog Probe")]
[RerunTransform("world/generated_cube", RateHz = 30f)]
public partial class GeneratedRerunLogProbe : MonoBehaviour
{
    [RerunLog("logs/generated", RateHz = 1f, Level = "INFO")]
    private string _status = "generated hello";

    [RerunScalar("metrics/generated_fps", RateHz = 10f)]
    private float _fps;

    private void Update()
    {
        _fps = Time.deltaTime > 0f ? 1f / Time.deltaTime : 0f;
        _status = $"generated frame {Time.frameCount}";
        transform.Rotate(0f, 45f * Time.deltaTime, 0f);
    }
}

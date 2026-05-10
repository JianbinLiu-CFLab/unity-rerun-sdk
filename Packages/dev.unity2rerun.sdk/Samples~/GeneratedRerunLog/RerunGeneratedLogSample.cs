// SPDX-License-Identifier: Apache-2.0

using Unity.RerunSDK.Unity;
using UnityEngine;

namespace Unity.RerunSDK.Samples
{
    [RerunTransform("world/generated_cube", RateHz = 30f)]
    public partial class RerunGeneratedLogSample : MonoBehaviour
    {
        [RerunLog("logs/generated", RateHz = 1f, Level = "INFO")]
        private string _status = "generated hello";

        [RerunScalar("metrics/generated_fps", RateHz = 10f)]
        private float _fps;

        private void Update()
        {
            _fps = Time.deltaTime > 0f ? 1f / Time.deltaTime : 0f;
            _status = $"generated frame {Time.frameCount}";
        }
    }
}

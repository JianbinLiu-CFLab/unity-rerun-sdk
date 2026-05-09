// SPDX-License-Identifier: Apache-2.0
//
// Pure C# rate limiter, testable without UnityEngine.

namespace Unity.RerunSDK.Unity
{
    internal class RerunPublishRateLimiter
    {
        private double _lastPublishTime = double.MinValue;

        /// Returns true if enough time has passed since the last publish.
        /// Uses unscaledTime to avoid Time.timeScale interaction.
        public bool CanPublish(float publishRateHz, double unscaledTime)
        {
            if (publishRateHz <= 0f)
                return true;

            var interval = 1.0 / publishRateHz;
            if (unscaledTime - _lastPublishTime >= interval)
            {
                _lastPublishTime = unscaledTime;
                return true;
            }
            return false;
        }

        public void Reset()
        {
            _lastPublishTime = double.MinValue;
        }
    }
}

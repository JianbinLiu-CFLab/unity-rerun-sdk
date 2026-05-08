// SPDX-License-Identifier: Apache-2.0
//
// Minimal sample: logs a TextLog entry every 2 seconds via RerunManager.
// Attach to any GameObject with a RerunManager on the same GameObject.

using UnityEngine;

namespace Unity.RerunSDK.Samples
{
    public class RerunSampleLogger : MonoBehaviour
    {
        private RerunManager _manager;

        private void Awake()
        {
            _manager = GetComponent<RerunManager>();
        }

        private void OnEnable()
        {
            InvokeRepeating(nameof(LogSample), 0.5f, 2.0f);
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(LogSample));
        }

        private void LogSample()
        {
            if (_manager != null && _manager.IsRecording)
            {
                _manager.LogText("logs/unity", $"Sample log at frame {Time.frameCount}");
            }
        }
    }
}

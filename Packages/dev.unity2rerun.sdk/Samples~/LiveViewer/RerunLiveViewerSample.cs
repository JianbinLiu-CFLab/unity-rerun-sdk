// SPDX-License-Identifier: Apache-2.0
//
// Live Viewer sample: TextLog + Transform + Scalar with FileAndLive output.
// Requires Rerun Viewer running on port 9876 or auto-launch enabled.
// Requires Cysharp YetAnotherHttpHandler for HTTP/2 gRPC in Unity.

using Unity.RerunSDK.Unity;
using UnityEngine;

namespace Unity.RerunSDK.Samples
{
    public class RerunLiveViewerSample : MonoBehaviour
    {
        private void Start()
        {
            var mgr = GetComponent<RerunManager>();
            if (mgr == null)
            {
                Debug.LogError("[Rerun Sample] RerunManager not found.");
                return;
            }

            // Ensure FileAndLive mode with auto-launch.
            // Configure these in the Inspector before entering Play Mode,
            // or set them programmatically as shown below.
            if (mgr.IsRecording) return;

            Debug.Log("[Rerun Sample] Starting FileAndLive recording. " +
                "Ensure Rerun Viewer is running or auto-launch is enabled.");
        }

        private void Update()
        {
            var mgr = GetComponent<RerunManager>();
            if (mgr == null || !mgr.IsRecording) return;

            mgr.SetTimeSequence("frame", Time.frameCount);
            mgr.LogText("logs/unity", $"Live frame {Time.frameCount}", "INFO");
            mgr.LogScalar("metrics/fps", 1.0 / Time.deltaTime);
            mgr.LogTransform("world/cube", transform);
        }
    }
}

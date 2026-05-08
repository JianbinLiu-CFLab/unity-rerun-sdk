// SPDX-License-Identifier: Apache-2.0
//
// Sample: TextLog + FPS scalar + moving transform .rrd recording.
// Attach to a GameObject that also has a RerunManager component.

using Unity.RerunSDK.Unity;
using UnityEngine;

namespace Unity.RerunSDK.Samples
{
    public class RerunSampleLogger : MonoBehaviour
    {
        private RerunManager _manager;
        private GameObject _cube;
        private float _textLogTimer;
        private float _scalarTimer;

        private void Awake()
        {
            _manager = GetComponent<RerunManager>();
        }

        private void Start()
        {
            // Create a simple cube for transform demo if none exists
            _cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _cube.name = "SampleCube";
            _cube.transform.SetParent(transform, false);
            _cube.transform.localPosition = new Vector3(0, 2, 0);
        }

        private void Update()
        {
            if (_manager == null || !_manager.IsRecording) return;

            // Timeline: frame counter
            _manager.SetTimeSequence("frame", Time.frameCount);

            // Moving transform
            var angle = Time.time * 45f;
            _cube.transform.position = new Vector3(Mathf.Sin(Time.time), 2f, Mathf.Cos(Time.time));
            _cube.transform.rotation = Quaternion.Euler(0, angle, 0);
            _manager.LogTransform("world/cube", _cube.transform);

            // Scalar every 0.25s
            _scalarTimer += Time.deltaTime;
            if (_scalarTimer >= 0.25f)
            {
                _scalarTimer = 0f;
                _manager.LogScalar("metrics/fps", 1.0 / Time.deltaTime);
            }

            // TextLog every 2s
            _textLogTimer += Time.deltaTime;
            if (_textLogTimer >= 2f)
            {
                _textLogTimer = 0f;
                _manager.LogText("logs/unity", $"Sample log at frame {Time.frameCount}");
            }
        }

        private void OnDestroy()
        {
            if (_cube != null)
                Destroy(_cube);
        }
    }
}

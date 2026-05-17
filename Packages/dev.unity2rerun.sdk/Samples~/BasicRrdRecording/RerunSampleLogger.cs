// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Samples~/BasicRrdRecording
// Purpose: Provides the Rerun Sample Logger Unity sample script for users importing the package.

// Sample: TextLog + FPS scalar + moving transform .rrd recording.
// Attach to a GameObject that also has a RerunManager component.

using Unity.RerunSDK.Unity;
using UnityEngine;

namespace Unity.RerunSDK.Samples
{
    /// <summary>
    /// Sample MonoBehaviour for Rerun Sample Logger in Unity sample scene.
    /// </summary>
    public class RerunSampleLogger : MonoBehaviour
    {
        private RerunManager _manager;
        private GameObject _cube;
        private float _textLogTimer;
        private float _scalarTimer;
        private bool _wroteStartupLog;

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

            var dt = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);

            // Timeline: frame counter
            _manager.SetTimeSequence("frame", Time.frameCount);

            // Moving transform
            var angle = Time.time * 45f;
            _cube.transform.position = new Vector3(Mathf.Sin(Time.time), 2f, Mathf.Cos(Time.time));
            _cube.transform.rotation = Quaternion.Euler(0, angle, 0);
            _manager.LogTransform("world/cube", _cube.transform);

            if (!_wroteStartupLog)
            {
                _wroteStartupLog = true;
                _manager.LogScalar("metrics/fps", 1.0 / dt);
                _manager.LogText("logs/unity", $"Unity2Rerun sample started at frame {Time.frameCount}");
            }

            // Scalar every 0.25s
            _scalarTimer += dt;
            if (_scalarTimer >= 0.25f)
            {
                _scalarTimer = 0f;
                _manager.LogScalar("metrics/fps", 1.0 / dt);
            }

            // TextLog every 1s of real time after the startup log.
            _textLogTimer += dt;
            if (_textLogTimer >= 1f)
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

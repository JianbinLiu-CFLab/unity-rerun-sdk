// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Samples~/PublisherComponents
// Purpose: Provides the Rerun Publisher Sample Driver Unity sample script for users importing the package.

// Sample driver: programmatic setup of Publisher components on an existing target.
// Attach to a GameObject with RerunManager, then assign Target Object in the Inspector.

using Unity.RerunSDK.Unity;
using UnityEngine;

namespace Unity.RerunSDK.Samples
{
    /// <summary>
    /// Sample MonoBehaviour for Rerun Publisher Sample Driver in Unity sample scene.
    /// </summary>
    public class RerunPublisherSampleDriver : MonoBehaviour
    {
        [SerializeField, Tooltip("Existing scene object that receives the Publisher components.")]
        private GameObject _targetObject;

        private void Start()
        {
            var mgr = GetComponent<RerunManager>();
            if (mgr == null)
            {
                Debug.LogError("[Rerun Sample] RerunManager not found on this GameObject.");
                return;
            }

            if (_targetObject == null)
            {
                Debug.LogError("[Rerun Sample] Assign Target Object, for example a Cube named SampleCube.");
                return;
            }

            var xform = GetOrAdd<RerunTransformPublisher>(_targetObject);
            xform.Manager = mgr;
            xform.EntityPath = "world/cube";
            xform.PublishRateHz = 30f;

            var fps = GetOrAdd<RerunScalarPublisher>(_targetObject);
            fps.Manager = mgr;
            fps.Source = RerunScalarSource.Fps;
            fps.EntityPath = "metrics/fps";
            fps.PublishRateHz = 4f;

            var log = GetOrAdd<RerunTextLogPublisher>(_targetObject);
            log.Manager = mgr;
            log.EntityPath = "logs/unity";
            log.Message = "Publisher sample started";
            log.Repeat = false;
            log.PublishRateHz = 0f; // one-shot publisher sends on the first publish tick

            Debug.Log($"[Rerun Sample] Publisher components configured on {_targetObject.name}.");
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}

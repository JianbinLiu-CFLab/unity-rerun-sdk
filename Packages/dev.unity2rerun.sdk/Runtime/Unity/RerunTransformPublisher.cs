// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Unity
// Purpose: Integrates managed Rerun logging with Unity runtime components.

using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Provides Rerun Transform Publisher support for Unity2Rerun.
    /// </summary>
    [AddComponentMenu("Rerun/Publishers/Rerun Transform Publisher")]
    public class RerunTransformPublisher : RerunPublisherBase
    {
        [SerializeField, Tooltip("Transform to publish. Leave empty to use this GameObject's transform.")]
        private Transform _target;

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        protected override void PublishNowCore(RerunManager manager, string entityPath)
        {
            var t = _target != null ? _target : transform;
            manager.LogTransform(entityPath, t);
        }

        protected override GameObject ResolveDefaultEntityPathGameObject()
        {
            var t = _target != null ? _target : transform;
            return t.gameObject;
        }
    }
}

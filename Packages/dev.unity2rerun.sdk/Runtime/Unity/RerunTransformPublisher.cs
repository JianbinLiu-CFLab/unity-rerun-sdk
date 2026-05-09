// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace Unity.RerunSDK.Unity
{
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

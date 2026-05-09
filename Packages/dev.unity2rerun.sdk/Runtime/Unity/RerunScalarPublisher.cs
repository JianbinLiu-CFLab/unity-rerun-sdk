// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    [AddComponentMenu("Rerun/Publishers/Rerun Scalar Publisher")]
    public class RerunScalarPublisher : RerunPublisherBase
    {
        [SerializeField, Tooltip("Data source for the scalar value.")]
        private RerunScalarSource _source = RerunScalarSource.Fps;

        [SerializeField, Tooltip("Constant value when source is Constant.")]
        private double _constantValue;

        [SerializeField, Tooltip("Target Transform for position/rotation sources. Leave empty to use self.")]
        private Transform _target;

        public RerunScalarSource Source
        {
            get => _source;
            set => _source = value;
        }

        public double ConstantValue
        {
            get => _constantValue;
            set => _constantValue = value;
        }

        public Transform Target
        {
            get => _target;
            set => _target = value;
        }

        protected override void PublishNowCore(RerunManager manager, string entityPath)
        {
            var transformVal = GetTransformComponent();

            var value = RerunScalarSourceEvaluator.Evaluate(
                _source, _constantValue,
                Time.deltaTime, Time.unscaledDeltaTime,
                Time.realtimeSinceStartupAsDouble, Time.frameCount, transformVal);

            manager.LogScalar(entityPath, value);
        }

        protected override GameObject ResolveDefaultEntityPathGameObject()
        {
            if (RerunScalarSourceEvaluator.IsTransformSource(_source) && _target != null)
                return _target.gameObject;

            return gameObject;
        }

        private float GetTransformComponent()
        {
            var t = _target != null ? _target : transform;
            return _source switch
            {
                RerunScalarSource.TransformPositionX => t.position.x,
                RerunScalarSource.TransformPositionY => t.position.y,
                RerunScalarSource.TransformPositionZ => t.position.z,
                RerunScalarSource.TransformRotationEulerX => t.rotation.eulerAngles.x,
                RerunScalarSource.TransformRotationEulerY => t.rotation.eulerAngles.y,
                RerunScalarSource.TransformRotationEulerZ => t.rotation.eulerAngles.z,
                _ => 0f,
            };
        }
    }
}

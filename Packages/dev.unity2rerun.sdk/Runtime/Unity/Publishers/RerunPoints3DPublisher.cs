// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.RerunSDK.Unity;
using UnityEngine;

namespace Unity.RerunSDK.Unity.Publishers
{
    [AddComponentMenu("Rerun/Publishers/Rerun Points3D Publisher")]
    public class RerunPoints3DPublisher : RerunPublisherBase
    {
        [SerializeField, Tooltip("Number of synthetic points to publish around this object.")]
        private int _pointCount = 64;

        [SerializeField, Tooltip("Radius of the synthetic point cloud around this object.")]
        private float _cloudRadius = 1.25f;

        [SerializeField, Tooltip("Visual radius of each Rerun point.")]
        private float _pointRadius = 0.025f;

        [SerializeField, Tooltip("Point color.")]
        private Color _color = new Color(0.2f, 0.8f, 1f, 1f);

        [SerializeField, Tooltip("Rotate the synthetic point cloud over time.")]
        private bool _animate = true;

        private readonly List<Vector3> _positions = new();
        private readonly List<Color> _colors = new();
        private readonly List<float> _radii = new();

        protected override void PublishNowCore(RerunManager manager, string entityPath)
        {
            BuildSyntheticCloud();
            manager.LogPoints3D(entityPath, _positions, _colors, _radii);
        }

        private void BuildSyntheticCloud()
        {
            var count = Mathf.Max(1, _pointCount);
            EnsureCapacity(count);

            _positions.Clear();
            _colors.Clear();
            _radii.Clear();

            var center = transform.position;
            var phase = _animate ? Time.time * 0.7f : 0f;
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0f : i / (float)(count - 1);
                var angle = t * Mathf.PI * 2f * 3f + phase;
                var y = Mathf.Lerp(-0.5f, 0.5f, t) * _cloudRadius;
                var ringRadius = Mathf.Sqrt(Mathf.Max(0f, 1f - (2f * t - 1f) * (2f * t - 1f))) * _cloudRadius;
                var local = new Vector3(Mathf.Cos(angle) * ringRadius, y, Mathf.Sin(angle) * ringRadius);
                _positions.Add(center + local);
                _colors.Add(_color);
                _radii.Add(Mathf.Max(0f, _pointRadius));
            }
        }

        private void EnsureCapacity(int count)
        {
            if (_positions.Capacity < count) _positions.Capacity = count;
            if (_colors.Capacity < count) _colors.Capacity = count;
            if (_radii.Capacity < count) _radii.Capacity = count;
        }
    }
}

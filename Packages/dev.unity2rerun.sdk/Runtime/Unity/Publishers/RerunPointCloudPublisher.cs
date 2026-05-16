// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.RerunSDK.Unity;
using UnityEngine;

namespace Unity.RerunSDK.Unity.Publishers
{
    [AddComponentMenu("Rerun/Publishers/Rerun Point Cloud Publisher")]
    public class RerunPointCloudPublisher : RerunPublisherBase
    {
        private const string DefaultPointCloudEntityPath = "world/point_cloud";

        [SerializeField, Tooltip("Transforms to publish as point positions.")]
        private Transform[] _sources;

        [SerializeField, Tooltip("Use child transforms when Sources is empty.")]
        private bool _useChildrenWhenSourcesEmpty = true;

        [SerializeField, Tooltip("Default point color.")]
        private Color _defaultColor = new Color(0.2f, 0.8f, 1f, 1f);

        [SerializeField, Tooltip("Default point radius.")]
        private float _defaultRadius = 0.03f;

        private readonly List<Vector3> _positions = new();
        private readonly List<Color> _colors = new();
        private readonly List<float> _radii = new();
        private bool _hasExplicitFrame;

        public void SetFrame(RerunPointCloudFrame frame)
        {
            _positions.Clear();
            _colors.Clear();
            _radii.Clear();

            if (frame.Positions == null || frame.Positions.Count == 0)
            {
                _hasExplicitFrame = false;
                return;
            }

            for (var i = 0; i < frame.Positions.Count; i++)
            {
                _positions.Add(frame.Positions[i]);
                _colors.Add(frame.Colors != null && i < frame.Colors.Count ? frame.Colors[i] : _defaultColor);
                _radii.Add(frame.Radii != null && i < frame.Radii.Count ? frame.Radii[i] : _defaultRadius);
            }
            _hasExplicitFrame = true;
        }

        public void ClearFrame()
        {
            _positions.Clear();
            _colors.Clear();
            _radii.Clear();
            _hasExplicitFrame = false;
        }

        protected override void OnEnable()
        {
            if (string.IsNullOrEmpty(_entityPath))
                _entityPath = DefaultPointCloudEntityPath;
            base.OnEnable();
        }

        protected override void PublishNowCore(RerunManager manager, string entityPath)
        {
            if (!_hasExplicitFrame)
                BuildFromTransforms();

            if (_positions.Count == 0)
                return;

            manager.LogPoints3D(entityPath, _positions, _colors, _radii);
        }

        private void BuildFromTransforms()
        {
            _positions.Clear();
            _colors.Clear();
            _radii.Clear();

            if (_sources != null && _sources.Length > 0)
            {
                for (var i = 0; i < _sources.Length; i++)
                    AddTransformPoint(_sources[i]);
                return;
            }

            if (!_useChildrenWhenSourcesEmpty)
                return;

            for (var i = 0; i < transform.childCount; i++)
                AddTransformPoint(transform.GetChild(i));
        }

        private void AddTransformPoint(Transform source)
        {
            if (source == null)
                return;

            _positions.Add(source.position);
            _colors.Add(_defaultColor);
            _radii.Add(Mathf.Max(0f, _defaultRadius));
        }
    }
}

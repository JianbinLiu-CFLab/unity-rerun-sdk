// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Unity/Publishers
// Purpose: Provides a Unity Inspector publisher component for Rerun visualization data.

using System.Collections.Generic;
using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.Unity;
using UnityEngine;

namespace Unity.RerunSDK.Unity.Publishers
{
    /// <summary>
    /// Provides Rerun Laser Scan Publisher support for Unity2Rerun.
    /// </summary>
    [AddComponentMenu("Rerun/Publishers/Rerun Laser Scan Publisher")]
    public class RerunLaserScanPublisher : RerunPublisherBase
    {
        /// <summary>Default entity path for synthetic laser scan demo data.</summary>
        private const string DefaultLaserScanEntityPath = "world/laser_scan";

        [SerializeField, Tooltip("Number of scan beams for the synthetic demo scan.")]
        private int _beamCount = 181;

        [SerializeField, Tooltip("Minimum scan angle in degrees.")]
        private float _angleMinDegrees = -90f;

        [SerializeField, Tooltip("Maximum scan angle in degrees.")]
        private float _angleMaxDegrees = 90f;

        [SerializeField, Tooltip("Minimum accepted range.")]
        private float _rangeMin = 0.05f;

        [SerializeField, Tooltip("Maximum accepted range.")]
        private float _rangeMax = 4f;

        [SerializeField, Tooltip("Publish scan points.")]
        private bool _publishPoints = true;

        [SerializeField, Tooltip("Publish a line strip through scan points.")]
        private bool _publishLineStrip = true;

        [SerializeField, Tooltip("Generate a moving synthetic scan when no external ranges are provided.")]
        private bool _animateSyntheticRanges = true;

        [SerializeField, Tooltip("Point color.")]
        private Color _pointColor = new Color(0.1f, 0.75f, 1f, 1f);

        [SerializeField, Tooltip("Line strip color.")]
        private Color _lineColor = new Color(0.1f, 0.75f, 1f, 0.8f);

        [SerializeField, Tooltip("Point radius.")]
        private float _pointRadius = 0.025f;

        private readonly List<float> _ranges = new();
        private readonly List<Vector3> _worldPoints = new();
        private readonly List<Color> _colors = new();
        private readonly List<float> _radii = new();
        private bool _hasExternalRanges;
        /// <summary>
        /// Sets runtime input used by subsequent publishing.
        /// </summary>
        public void SetRanges(IReadOnlyList<float> ranges)
        {
            _ranges.Clear();
            if (ranges == null || ranges.Count == 0)
            {
                _hasExternalRanges = false;
                return;
            }

            for (var i = 0; i < ranges.Count; i++)
                _ranges.Add(ranges[i]);
            _hasExternalRanges = true;
        }
        /// <summary>
        /// Clears runtime input so the default publishing path is used again.
        /// </summary>
        public void ClearRanges()
        {
            _ranges.Clear();
            _hasExternalRanges = false;
        }

        protected override void OnEnable()
        {
            if (string.IsNullOrEmpty(_entityPath))
                _entityPath = DefaultLaserScanEntityPath;
            base.OnEnable();
        }

        protected override void PublishNowCore(RerunManager manager, string entityPath)
        {
            if (!_hasExternalRanges)
                BuildSyntheticRanges();

            BuildWorldPoints();
            if (_worldPoints.Count == 0)
                return;

            if (_publishPoints)
                manager.LogPoints3D(entityPath, _worldPoints, _colors, _radii);
            if (_publishLineStrip && _worldPoints.Count > 1)
                manager.LogLineStrips3D(entityPath + "_outline", _worldPoints, _lineColor);
        }

        private void BuildSyntheticRanges()
        {
            var count = Mathf.Max(2, _beamCount);
            if (_ranges.Capacity < count)
                _ranges.Capacity = count;
            _ranges.Clear();

            var phase = _animateSyntheticRanges ? Time.unscaledTime * 1.2f : 0f;
            var min = Mathf.Max(0f, _rangeMin);
            var max = Mathf.Max(min + 0.001f, _rangeMax);
            for (var i = 0; i < count; i++)
            {
                var t = count == 1 ? 0f : i / (float)(count - 1);
                var wave = 0.5f + 0.5f * Mathf.Sin(phase + t * Mathf.PI * 4f);
                var envelope = 0.65f + 0.2f * Mathf.Cos(t * Mathf.PI * 2f);
                _ranges.Add(Mathf.Lerp(min, max, Mathf.Clamp01(envelope + wave * 0.12f)));
            }
        }

        private void BuildWorldPoints()
        {
            _worldPoints.Clear();
            _colors.Clear();
            _radii.Clear();

            var count = _ranges.Count;
            if (count == 0)
                return;

            var angleMin = _angleMinDegrees * Mathf.Deg2Rad;
            var angleMax = _angleMaxDegrees * Mathf.Deg2Rad;
            var angleStep = count <= 1 ? 0f : (angleMax - angleMin) / (count - 1);
            var localPoints = RerunLaserScanProjection.ProjectToXz(
                _ranges, angleMin, angleStep, _rangeMin, _rangeMax);

            for (var i = 0; i < localPoints.Count; i++)
            {
                var point = localPoints[i];
                _worldPoints.Add(transform.TransformPoint(new Vector3(point.X, point.Y, point.Z)));
                _colors.Add(_pointColor);
                _radii.Add(Mathf.Max(0f, _pointRadius));
            }
        }
    }
}

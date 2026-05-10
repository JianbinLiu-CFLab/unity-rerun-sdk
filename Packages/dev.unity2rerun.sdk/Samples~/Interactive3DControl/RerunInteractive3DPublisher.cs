// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using Unity.RerunSDK.Unity;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.RerunSDK.Samples
{
    public class RerunInteractive3DPublisher : MonoBehaviour
    {
        [SerializeField] private RerunManager _manager;
        [SerializeField] private Transform _target;
        [SerializeField] private Renderer _targetRenderer;
        [SerializeField] private RerunInteractiveControlBridge _controlBridge;
        [SerializeField] private string _entityPath = "world/cube";
        [SerializeField] private float _spatialRateHz = 30f;
        [SerializeField] private float _metricsRateHz = 10f;
        [SerializeField] private int _trajectoryPointLimit = 256;

        private readonly List<Vector3> _trajectory = new();
        private Vector3 _lastTrajectoryPoint;
        private bool _hasTrajectoryPoint;
        private double _lastSpatialPublishTime = double.MinValue;
        private double _lastMetricsPublishTime = double.MinValue;

        private void Awake()
        {
            if (_target == null)
                _target = transform;
            if (_targetRenderer == null && _target != null)
                _targetRenderer = _target.GetComponent<Renderer>();
            if (_controlBridge == null)
                _controlBridge = GetComponent<RerunInteractiveControlBridge>();
            if (_controlBridge == null && _target != null)
                _controlBridge = _target.GetComponent<RerunInteractiveControlBridge>();
        }

        private void Update()
        {
            var manager = ResolveManager();
            if (manager == null || !manager.IsRecording || _target == null)
                return;

            manager.SetTimeSequence("frame", Time.frameCount);

            var now = Time.unscaledTimeAsDouble;
            if (CanPublish(_spatialRateHz, now, ref _lastSpatialPublishTime))
                PublishSpatial(manager);

            if (CanPublish(_metricsRateHz, now, ref _lastMetricsPublishTime))
                PublishMetrics(manager);
        }

        private void PublishSpatial(RerunManager manager)
        {
            Profiler.BeginSample("RerunInteractive3DPublisher.Spatial");
            try
            {
                manager.LogTransform(_entityPath, _target);
                manager.LogBox3D(_entityPath, Vector3.zero, ResolveHalfSize(), Quaternion.identity, ResolveColor());
                AppendTrajectoryPoint(_target.position);
                manager.LogLineStrips3D(_entityPath + "_trajectory", _trajectory, Color.yellow);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private void PublishMetrics(RerunManager manager)
        {
            var fps = Time.unscaledDeltaTime > 0f ? 1.0 / Time.unscaledDeltaTime : 0.0;
            manager.LogScalar("metrics/interactive/fps", fps);
            manager.LogScalar("metrics/interactive/trajectory_points", _trajectory.Count);
            if (_controlBridge != null)
                manager.LogScalar("metrics/interactive/command_count", _controlBridge.CommandCount);
        }

        private void AppendTrajectoryPoint(Vector3 position)
        {
            if (_hasTrajectoryPoint && (position - _lastTrajectoryPoint).sqrMagnitude < 0.0001f)
                return;

            _trajectory.Add(position);
            _lastTrajectoryPoint = position;
            _hasTrajectoryPoint = true;

            while (_trajectory.Count > Mathf.Max(2, _trajectoryPointLimit))
                _trajectory.RemoveAt(0);
        }

        private Color ResolveColor()
        {
            if (_targetRenderer != null)
                return _targetRenderer.material.color;
            return Color.green;
        }

        private Vector3 ResolveHalfSize()
        {
            var scale = _target.lossyScale;
            return new Vector3(
                Mathf.Abs(scale.x) * 0.5f,
                Mathf.Abs(scale.y) * 0.5f,
                Mathf.Abs(scale.z) * 0.5f);
        }

        private RerunManager ResolveManager()
        {
            if (_manager != null)
                return _manager;

#if UNITY_2023_1_OR_NEWER
            _manager = FindFirstObjectByType<RerunManager>();
#else
            _manager = FindObjectOfType<RerunManager>();
#endif
            return _manager;
        }

        private static bool CanPublish(float publishRateHz, double unscaledTime, ref double lastPublishTime)
        {
            if (publishRateHz <= 0f)
                return true;

            var interval = 1.0 / publishRateHz;
            if (unscaledTime - lastPublishTime < interval)
                return false;

            lastPublishTime = unscaledTime;
            return true;
        }
    }
}

// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components/Publishing
// Purpose: Provides a Unity Inspector publisher component for Rerun visualization data.

using Unity.RerunSDK.Encoding;
using Unity.RerunSDK.Unity;
using UnityEngine;

namespace Unity.RerunSDK.Unity.Publishers
{
    /// <summary>
    /// Provides Rerun Pinhole Camera Publisher support for Unity2Rerun.
    /// </summary>
    [AddComponentMenu("Rerun/Publishers/Rerun Pinhole Camera Publisher")]
    public class RerunPinholeCameraPublisher : RerunPublisherBase
    {
        /// <summary>Default entity path for static pinhole camera calibration data.</summary>
        private const string DefaultCameraEntityPath = "world/camera";

        [SerializeField, Tooltip("Camera to describe. Leave empty to use Camera.main.")]
        private Camera _camera;

        [SerializeField, Tooltip("Image width used for the pinhole resolution.")]
        private int _width = 640;

        [SerializeField, Tooltip("Image height used for the pinhole resolution.")]
        private int _height = 480;

        [SerializeField, Tooltip("Distance used by Rerun Viewer to draw the camera image plane.")]
        private float _imagePlaneDistance = 0.1f;

        [SerializeField, Tooltip("Frustum wireframe color.")]
        private Color _frustumColor = new Color(0.2f, 0.8f, 1f, 0.75f);

        [SerializeField, Tooltip("Frustum wireframe line width.")]
        private float _lineWidth = 0.003f;

        [SerializeField, Tooltip("Publish the camera transform on the same entity path.")]
        private bool _publishCameraPose = true;

        private bool _pinholePublished;

        public Camera TargetCamera
        {
            get => _camera;
            set => _camera = value;
        }

        public int Width
        {
            get => _width;
            set => _width = Mathf.Max(1, value);
        }

        public int Height
        {
            get => _height;
            set => _height = Mathf.Max(1, value);
        }
        /// <summary>
        /// Handles the RepublishPinhole workflow for this component.
        /// </summary>
        public void RepublishPinhole()
        {
            _pinholePublished = false;
            PublishOnce();
        }

        protected override void OnEnable()
        {
            if (string.IsNullOrEmpty(_entityPath))
                _entityPath = DefaultCameraEntityPath;
            _pinholePublished = false;
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _pinholePublished = false;
        }

        protected override void PublishNowCore(RerunManager manager, string entityPath)
        {
            var cam = ResolveCamera();
            if (cam == null)
                return;

            if (!_pinholePublished)
            {
                var pinhole = RerunPinhole.FromUnityCamera(
                    cam,
                    Mathf.Max(1, _width),
                    Mathf.Max(1, _height),
                    Mathf.Max(0f, _imagePlaneDistance),
                    ToRgba32(_frustumColor),
                    Mathf.Max(0f, _lineWidth));
                manager.LogPinhole(entityPath, pinhole);
                _pinholePublished = true;
            }

            if (_publishCameraPose)
                manager.LogTransform(entityPath, cam.transform);
        }

        protected override GameObject ResolveDefaultEntityPathGameObject()
        {
            var cam = ResolveCamera();
            return cam != null ? cam.gameObject : gameObject;
        }

        private Camera ResolveCamera()
        {
            return _camera != null ? _camera : Camera.main;
        }

        private static uint ToRgba32(Color color)
        {
            var r = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f);
            var g = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f);
            var b = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f);
            var a = (uint)Mathf.RoundToInt(Mathf.Clamp01(color.a) * 255f);
            return (r << 24) | (g << 16) | (b << 8) | a;
        }
    }
}

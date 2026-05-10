// SPDX-License-Identifier: Apache-2.0

using UnityEngine;
using UnityEngine.Profiling;
using Unity.RerunSDK.Unity;

namespace Unity.RerunSDK.Unity.Publishers
{
    public enum RerunEncodedImageFormat
    {
        Jpeg,
        Png
    }

    [AddComponentMenu("Rerun/Publishers/Rerun Camera Image Publisher")]
    public class RerunCameraImagePublisher : RerunPublisherBase
    {
        [SerializeField, Tooltip("Camera to capture. Leave empty to use Camera.main.")]
        private Camera _camera;

        [SerializeField, Tooltip("Captured image width.")]
        private int _width = 640;

        [SerializeField, Tooltip("Captured image height.")]
        private int _height = 480;

        [SerializeField, Tooltip("Encoded output format.")]
        private RerunEncodedImageFormat _format = RerunEncodedImageFormat.Jpeg;

        [SerializeField, Range(1, 100), Tooltip("JPEG quality when format is Jpeg.")]
        private int _jpegQuality = 70;

        [SerializeField, Tooltip("Drop encoded frames larger than this size in bytes. 0 disables the limit.")]
        private int _maxEncodedBytes = 512 * 1024;

        private Texture2D _texture;
        private int _lastWidth;
        private int _lastHeight;

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

        public int MaxEncodedBytes
        {
            get => _maxEncodedBytes;
            set => _maxEncodedBytes = Mathf.Max(0, value);
        }

        protected override void PublishNowCore(RerunManager manager, string entityPath)
        {
            var cam = ResolveCamera();
            if (cam == null)
                return;

            Profiler.BeginSample("RerunCameraImagePublisher.Update");
            try
            {
                var bytes = CaptureEncoded(cam);
                if (bytes == null || bytes.Length == 0)
                    return;

                if (_maxEncodedBytes > 0 && bytes.Length > _maxEncodedBytes)
                {
                    manager.LogText("logs/rerun/image", $"Dropped encoded image frame: {bytes.Length} bytes > {_maxEncodedBytes}", "WARN");
                    return;
                }

                manager.LogEncodedImage(entityPath, bytes, MediaType);
            }
            finally
            {
                Profiler.EndSample();
            }
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

        private byte[] CaptureEncoded(Camera cam)
        {
            EnsureTexture();

            Profiler.BeginSample("RerunCameraImagePublisher.ImageEncode");
            var previousTarget = cam.targetTexture;
            var previousActive = RenderTexture.active;
            var rt = RenderTexture.GetTemporary(_width, _height, 24, RenderTextureFormat.ARGB32);
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                _texture.ReadPixels(new Rect(0, 0, _width, _height), 0, 0, false);
                _texture.Apply(false);
                return _format == RerunEncodedImageFormat.Png
                    ? _texture.EncodeToPNG()
                    : _texture.EncodeToJPG(Mathf.Clamp(_jpegQuality, 1, 100));
            }
            finally
            {
                cam.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(rt);
                Profiler.EndSample();
            }
        }

        private void EnsureTexture()
        {
            if (_texture != null && _lastWidth == _width && _lastHeight == _height)
                return;

            if (_texture != null)
                Destroy(_texture);

            _lastWidth = Mathf.Max(1, _width);
            _lastHeight = Mathf.Max(1, _height);
            _width = _lastWidth;
            _height = _lastHeight;
            _texture = new Texture2D(_width, _height, TextureFormat.RGB24, false);
        }

        private string MediaType => _format == RerunEncodedImageFormat.Png
            ? "image/png"
            : "image/jpeg";

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_texture != null)
            {
                Destroy(_texture);
                _texture = null;
            }
        }
    }
}

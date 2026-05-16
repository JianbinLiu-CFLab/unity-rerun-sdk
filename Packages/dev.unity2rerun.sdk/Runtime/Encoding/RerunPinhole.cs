// SPDX-License-Identifier: Apache-2.0

using System;

namespace Unity.RerunSDK.Encoding
{
    public readonly partial struct RerunPinhole
    {
        public const byte CameraXyzRight = 3;
        public const byte CameraXyzDown = 2;
        public const byte CameraXyzForward = 5;

        public RerunPinhole(
            int width,
            int height,
            float fx,
            float fy,
            float cx,
            float cy,
            float imagePlaneDistance = 0.1f,
            uint colorRgba = 0x33AAFFFF,
            float lineWidth = 0.003f)
        {
            Width = Math.Max(1, width);
            Height = Math.Max(1, height);
            Fx = Math.Max(0f, fx);
            Fy = Math.Max(0f, fy);
            Cx = cx;
            Cy = cy;
            ImagePlaneDistance = Math.Max(0f, imagePlaneDistance);
            ColorRgba = colorRgba;
            LineWidth = Math.Max(0f, lineWidth);
        }

        public int Width { get; }
        public int Height { get; }
        public float Fx { get; }
        public float Fy { get; }
        public float Cx { get; }
        public float Cy { get; }
        public float ImagePlaneDistance { get; }
        public uint ColorRgba { get; }
        public float LineWidth { get; }

        public static RerunPinhole FromVerticalFov(
            int width,
            int height,
            float verticalFovDegrees,
            float imagePlaneDistance = 0.1f,
            uint colorRgba = 0x33AAFFFF,
            float lineWidth = 0.003f)
        {
            var clampedHeight = Math.Max(1, height);
            var halfFovRadians = Math.Max(0.0001f, verticalFovDegrees * (float)Math.PI / 360f);
            var focalLength = clampedHeight / (2f * (float)Math.Tan(halfFovRadians));

            return new RerunPinhole(
                width,
                height,
                focalLength,
                focalLength,
                Math.Max(1, width) * 0.5f,
                clampedHeight * 0.5f,
                imagePlaneDistance,
                colorRgba,
                lineWidth);
        }
    }
}

// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace Unity.RerunSDK.Encoding
{
    public readonly partial struct RerunPinhole
    {
        /// <summary>
        /// Builds a first-pass pinhole model from Unity's vertical FOV.
        /// The generated camera space uses Rerun RDF coordinates
        /// (right, down, forward), which differs from the world
        /// RIGHT_HAND_Y_UP coordinates. This approximation assumes fx == fy,
        /// so non-square images are suitable for visualization but not camera
        /// calibration.
        /// </summary>
        public static RerunPinhole FromUnityCamera(
            Camera camera,
            int width,
            int height,
            float imagePlaneDistance = 0.1f,
            uint colorRgba = 0x33AAFFFF,
            float lineWidth = 0.003f)
        {
            var fov = camera != null ? camera.fieldOfView : 60f;
            return FromVerticalFov(width, height, fov, imagePlaneDistance, colorRgba, lineWidth);
        }
    }
}

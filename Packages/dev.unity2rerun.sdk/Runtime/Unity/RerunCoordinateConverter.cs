// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    /// Unity-to-Rerun coordinate converter.
    /// Left-hand Y-up (Unity) → RIGHT_HAND_Y_UP (Rerun): position (x, y, -z).
    public static class RerunCoordinateConverter
    {
        /// Convert Unity left-hand Y-up position to Rerun RIGHT_HAND_Y_UP.
        public static Vector3 ToRerunPosition(Vector3 unityPosition)
        {
            return new Vector3(unityPosition.x, unityPosition.y, -unityPosition.z);
        }

        /// Convert Unity rotation to Rerun RIGHT_HAND_Y_UP via S*R*S,
        /// where S = diag(1, 1, -1). Uses Matrix4x4.Rotate for pure rotation (no scale).
        public static Quaternion ToRerunRotation(Quaternion unityRotation)
        {
            var r = Matrix4x4.Rotate(unityRotation);

            // S * R * S with S = diag(1, 1, -1)
            FlipColumnSign(ref r, 2); // R * S
            FlipRowSign(ref r, 2);    // S * R

            return r.rotation;
        }

        private static void FlipColumnSign(ref Matrix4x4 m, int col)
        {
            m[0, col] = -m[0, col];
            m[1, col] = -m[1, col];
            m[2, col] = -m[2, col];
        }

        private static void FlipRowSign(ref Matrix4x4 m, int row)
        {
            m[row, 0] = -m[row, 0];
            m[row, 1] = -m[row, 1];
            m[row, 2] = -m[row, 2];
        }
    }
}

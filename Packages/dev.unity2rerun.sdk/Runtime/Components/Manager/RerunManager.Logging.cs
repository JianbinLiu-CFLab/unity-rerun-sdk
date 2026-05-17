// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components/Manager
// Purpose: Exposes public Rerun logging helpers and local Unity conversion helpers.

using System;
using System.Collections.Generic;
using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;
using UnityEngine;

namespace Unity.RerunSDK.Unity
{
    public partial class RerunManager
    {
        // -- Logging API --
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogText(string entityPath, string text, string level = "INFO")
        {
            if (_runtime == null || !IsRecording) return;
            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeTextLogMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, text, level, snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogScalar(string entityPath, double value)
        {
            if (_runtime == null || !IsRecording) return;
            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeScalarMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, value, snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogTransform(string entityPath, Transform transform)
        {
            if (transform == null) return;
            LogTransform(entityPath, transform.position, transform.rotation);
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogTransform(string entityPath, Vector3 position, Quaternion rotation)
        {
            if (_runtime == null || !IsRecording) return;
            var pos = RerunCoordinateConverter.ToRerunPosition(position);
            var rot = RerunCoordinateConverter.ToRerunRotation(rotation);
            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeTransform3DMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, pos.x, pos.y, pos.z, rot.x, rot.y, rot.z, rot.w,
                snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogEncodedImage(string entityPath, byte[] encodedBytes, string mediaType)
        {
            if (_runtime == null || !IsRecording) return;
            if (encodedBytes == null || encodedBytes.Length == 0) return;

            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeEncodedImageMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, encodedBytes, mediaType, snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogPinhole(string entityPath, RerunPinhole pinhole)
        {
            if (_runtime == null || !IsRecording) return;

            _backend.Write(_encoder.EncodePinholeMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, pinhole));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogBox3D(string entityPath, Transform target, Color color)
        {
            if (target == null) return;
            var halfSize = new Vector3(
                Mathf.Abs(target.lossyScale.x) * 0.5f,
                Mathf.Abs(target.lossyScale.y) * 0.5f,
                Mathf.Abs(target.lossyScale.z) * 0.5f);
            LogBox3D(entityPath, target.position, halfSize, target.rotation, color);
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogBox3D(string entityPath, Vector3 center, Vector3 halfSize, Quaternion rotation, Color color)
        {
            LogBoxes3D(
                entityPath,
                new[] { center },
                new[] { halfSize },
                new[] { rotation },
                new[] { color });
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogBoxes3D(
            string entityPath,
            IReadOnlyList<Vector3> centers,
            IReadOnlyList<Vector3> halfSizes,
            IReadOnlyList<Quaternion> rotations = null,
            IReadOnlyList<Color> colors = null)
        {
            if (_runtime == null || !IsRecording) return;
            if (centers == null || halfSizes == null) return;

            var count = Math.Min(centers.Count, halfSizes.Count);
            if (count <= 0) return;

            if (rotations != null && rotations.Count != count && !_warnedBoxes3DRotationLengthMismatch)
            {
                Debug.LogWarning($"[Rerun] LogBoxes3D('{entityPath}') rotations count {rotations.Count} does not match box count {count}; missing rotations use identity.");
                _warnedBoxes3DRotationLengthMismatch = true;
            }

            if (colors != null && colors.Count != count && !_warnedBoxes3DColorLengthMismatch)
            {
                Debug.LogWarning($"[Rerun] LogBoxes3D('{entityPath}') colors count {colors.Count} does not match box count {count}; missing colors use green.");
                _warnedBoxes3DColorLengthMismatch = true;
            }

            var boxes = new List<RerunBox3D>(count);
            for (var i = 0; i < count; i++)
            {
                var center = RerunCoordinateConverter.ToRerunPosition(centers[i]);
                var halfSize = AbsVector(halfSizes[i]);
                var rotation = rotations != null && i < rotations.Count
                    ? RerunCoordinateConverter.ToRerunRotation(rotations[i])
                    : Quaternion.identity;
                var color = colors != null && i < colors.Count ? colors[i] : Color.green;

                boxes.Add(new RerunBox3D(
                    ToRerunVec3(center),
                    ToRerunVec3(halfSize),
                    new RerunQuat(rotation.x, rotation.y, rotation.z, rotation.w),
                    ToRgba32(color)));
            }

            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeBoxes3DMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, boxes, snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogLineStrip3D(string entityPath, IReadOnlyList<Vector3> points, Color color)
        {
            LogLineStrips3D(entityPath, points, color);
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogLineStrips3D(string entityPath, IReadOnlyList<Vector3> points, Color color)
        {
            if (_runtime == null || !IsRecording) return;
            if (points == null || points.Count == 0) return;

            var convertedPoints = new List<RerunVec3>(points.Count);
            for (var i = 0; i < points.Count; i++)
                convertedPoints.Add(ToRerunVec3(RerunCoordinateConverter.ToRerunPosition(points[i])));

            var strip = new RerunLineStrip3D(convertedPoints, ToRgba32(color));
            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodeLineStrips3DMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, new[] { strip }, snapshot.ToEntries()));
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogPoints3D(string entityPath, IReadOnlyList<Vector3> positions, Color color, float radius = 0.03f)
        {
            if (_runtime == null || !IsRecording) return;
            if (positions == null || positions.Count == 0) return;

            var colors = new Color[positions.Count];
            var radii = new float[positions.Count];
            for (var i = 0; i < positions.Count; i++)
            {
                colors[i] = color;
                radii[i] = radius;
            }

            LogPoints3D(entityPath, positions, colors, radii);
        }
        /// <summary>
        /// Logs the requested data under a Rerun entity path.
        /// </summary>
        public void LogPoints3D(
            string entityPath,
            IReadOnlyList<Vector3> positions,
            IReadOnlyList<Color> colors = null,
            IReadOnlyList<float> radii = null)
        {
            if (_runtime == null || !IsRecording) return;
            if (positions == null || positions.Count == 0) return;

            if (colors != null && colors.Count != positions.Count && !_warnedPoints3DColorLengthMismatch)
            {
                Debug.LogWarning($"[Rerun] LogPoints3D('{entityPath}') colors count {colors.Count} does not match point count {positions.Count}; missing colors use cyan.");
                _warnedPoints3DColorLengthMismatch = true;
            }

            if (radii != null && radii.Count != positions.Count && !_warnedPoints3DRadiusLengthMismatch)
            {
                Debug.LogWarning($"[Rerun] LogPoints3D('{entityPath}') radii count {radii.Count} does not match point count {positions.Count}; missing radii use 0.03.");
                _warnedPoints3DRadiusLengthMismatch = true;
            }

            var points = new List<RerunPoint3D>(positions.Count);
            for (var i = 0; i < positions.Count; i++)
            {
                var position = RerunCoordinateConverter.ToRerunPosition(positions[i]);
                var color = colors != null && i < colors.Count ? colors[i] : Color.cyan;
                var radius = radii != null && i < radii.Count ? Mathf.Max(0f, radii[i]) : 0.03f;
                points.Add(new RerunPoint3D(ToRerunVec3(position), ToRgba32(color), radius));
            }

            var snapshot = _runtime.CaptureTimelineSnapshot();
            _backend.Write(_encoder.EncodePoints3DMessage(
                _runtime.RecordingId, _applicationId,
                entityPath, points, snapshot.ToEntries()));
        }

        private void WriteViewCoordinates()
        {
            _backend.Write(_encoder.EncodeViewCoordinatesMessage(
                _runtime.RecordingId, _applicationId, "world", 3, 1, 6));
        }

        private static Vector3 AbsVector(Vector3 value)
        {
            return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
        }

        private static RerunVec3 ToRerunVec3(Vector3 value)
        {
            return new RerunVec3(value.x, value.y, value.z);
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

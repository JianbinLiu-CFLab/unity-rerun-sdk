// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Encoding
// Purpose: Defines managed Rerun encoding primitives used by RRD files and live transport.

using System;
using System.Collections.Generic;

namespace Unity.RerunSDK.Encoding
{
    /// <summary>
    /// Carries Rerun Vec3 data across Unity2Rerun runtime boundaries.
    /// </summary>
    internal readonly struct RerunVec3
    {
        public RerunVec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }
    /// <summary>
    /// Carries Rerun Quat data across Unity2Rerun runtime boundaries.
    /// </summary>
    internal readonly struct RerunQuat
    {
        public RerunQuat(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float W { get; }
    }
    /// <summary>
    /// Carries Rerun Box3 D data across Unity2Rerun runtime boundaries.
    /// </summary>
    internal readonly struct RerunBox3D
    {
        public RerunBox3D(RerunVec3 center, RerunVec3 halfSize, RerunQuat rotation, uint colorRgba)
        {
            Center = center;
            HalfSize = halfSize;
            Rotation = rotation;
            ColorRgba = colorRgba;
        }

        public RerunVec3 Center { get; }
        public RerunVec3 HalfSize { get; }
        public RerunQuat Rotation { get; }
        public uint ColorRgba { get; }
    }
    /// <summary>
    /// Carries Rerun Line Strip3 D data across Unity2Rerun runtime boundaries.
    /// </summary>
    internal readonly struct RerunLineStrip3D
    {
        public RerunLineStrip3D(IReadOnlyList<RerunVec3> points, uint colorRgba)
        {
            Points = points ?? Array.Empty<RerunVec3>();
            ColorRgba = colorRgba;
        }

        public IReadOnlyList<RerunVec3> Points { get; }
        public uint ColorRgba { get; }
    }
    /// <summary>
    /// Carries Rerun Point3 D data across Unity2Rerun runtime boundaries.
    /// </summary>
    internal readonly struct RerunPoint3D
    {
        public RerunPoint3D(RerunVec3 position, uint colorRgba, float radius)
        {
            Position = position;
            ColorRgba = colorRgba;
            Radius = radius;
        }

        public RerunVec3 Position { get; }
        public uint ColorRgba { get; }
        public float Radius { get; }
    }
}

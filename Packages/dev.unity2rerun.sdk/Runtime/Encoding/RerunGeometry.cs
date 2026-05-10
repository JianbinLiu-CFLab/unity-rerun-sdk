// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace Unity.RerunSDK.Encoding
{
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
}

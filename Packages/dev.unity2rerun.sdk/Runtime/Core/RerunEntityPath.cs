// SPDX-License-Identifier: Apache-2.0

using System;

namespace Unity.RerunSDK.Core
{
    /// Represents a Rerun entity path (e.g. "world/cube", "logs/unity").
    /// Path segments must not be empty or contain whitespace.
    /// An empty path represents the root entity "/".
    public readonly struct RerunEntityPath : IEquatable<RerunEntityPath>
    {
        public string Value { get; }

        public static readonly RerunEntityPath Root = new("/");

        public RerunEntityPath(string path)
        {
            Value = string.IsNullOrEmpty(path) ? "/" : path;
        }

        public static implicit operator RerunEntityPath(string path) => new(path);

        public bool Equals(RerunEntityPath other) => Value == other.Value;
        public override bool Equals(object obj) => obj is RerunEntityPath other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value;

        public static bool operator ==(RerunEntityPath a, RerunEntityPath b) => a.Value == b.Value;
        public static bool operator !=(RerunEntityPath a, RerunEntityPath b) => a.Value != b.Value;
    }
}

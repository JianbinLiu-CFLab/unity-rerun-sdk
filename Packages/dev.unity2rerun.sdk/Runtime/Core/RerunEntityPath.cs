// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text;

namespace Unity.RerunSDK.Core
{
    /// Represents a Rerun entity path (e.g. "world/cube", "logs/unity").
    public readonly struct RerunEntityPath : IEquatable<RerunEntityPath>
    {
        public string Value { get; }

        public static readonly RerunEntityPath Root = new("/");

        public RerunEntityPath(string path)
        {
            Value = string.IsNullOrEmpty(path) || path == "/" ? "/" : Normalize(path);
        }

        public static RerunEntityPath FromString(string raw)
        {
            return new RerunEntityPath(raw);
        }

#if UNITY_5_3_OR_NEWER
        /// Build an entity path from a GameObject's transform hierarchy.
        /// Uses the transform parent chain from leaf to root, reversed.
        public static RerunEntityPath FromGameObject(
            UnityEngine.GameObject go, string root = "world")
        {
            if (go == null) return new RerunEntityPath(root);

            var parts = new System.Collections.Generic.List<string>();
            var t = go.transform;
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();

            var sb = new StringBuilder(root);
            foreach (var p in parts)
                sb.Append('/').Append(p);
            return new RerunEntityPath(sb.ToString());
        }
#endif

        private static string Normalize(string raw)
        {
            raw = raw.Trim('/');
            if (raw.Length == 0) return "/";

            var parts = raw.Split('/');
            var sb = new StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (string.IsNullOrEmpty(part))
                    throw new ArgumentException($"Entity path has empty segment: '{raw}'");

                if (i > 0) sb.Append('/');

                // First segment with __ prefix → reserved Rerun prefix
                if (i == 0 && part.StartsWith("__"))
                    sb.Append("_user").Append(part);
                else
                    sb.Append(EscapePart(part));
            }
            return sb.ToString();
        }

        private static string EscapePart(string part)
        {
            // Characters that don't need escaping in Rerun entity paths
            var sb = new StringBuilder(part.Length);
            foreach (char c in part)
            {
                if ((c >= 'a' && c <= 'z') ||
                    (c >= 'A' && c <= 'Z') ||
                    (c >= '0' && c <= '9') ||
                    c == '_' || c == '-' || c == '.')
                {
                    sb.Append(c);
                }
                else if (c == '/')
                {
                    throw new ArgumentException(
                        $"Entity path part contains '/' separator: '{part}'");
                }
                else
                {
                    // Non-ASCII or special → escape
                    sb.Append('\\');
                    sb.Append((int)c);
                }
            }
            return sb.ToString();
        }

        public bool Equals(RerunEntityPath other) => Value == other.Value;
        public override bool Equals(object obj) => obj is RerunEntityPath other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(RerunEntityPath a, RerunEntityPath b) => a.Value == b.Value;
        public static bool operator !=(RerunEntityPath a, RerunEntityPath b) => a.Value != b.Value;
    }
}

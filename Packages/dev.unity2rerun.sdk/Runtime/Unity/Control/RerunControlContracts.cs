// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Unity/Control
// Purpose: Implements local loopback sidecar control for interactive Unity samples.

#nullable disable

using System;
using System.Globalization;
using System.Text;

namespace Unity.RerunSDK.Unity.Control
{
    /// <summary>
    /// Enumerates supported Rerun Control Command Type values.
    /// </summary>
    public enum RerunControlCommandType
    {
        SetPose,
        SetScale,
        SetColor,
        ResetPose
    }
    /// <summary>
    /// Provides Rerun Control Command Names support for Unity2Rerun.
    /// </summary>
    public static class RerunControlCommandNames
    {
        /// <summary>
        /// Handles the ToWireName workflow for this component.
        /// </summary>
        public static string ToWireName(RerunControlCommandType type)
        {
            switch (type)
            {
                case RerunControlCommandType.SetPose:
                    return "set_pose";
                case RerunControlCommandType.SetScale:
                    return "set_scale";
                case RerunControlCommandType.SetColor:
                    return "set_color";
                case RerunControlCommandType.ResetPose:
                    return "reset_pose";
                default:
                    return "unknown";
            }
        }
    }
    /// <summary>
    /// Carries Rerun Control Command Result data across Unity2Rerun runtime boundaries.
    /// </summary>
    public readonly struct RerunControlCommandResult
    {
        private RerunControlCommandResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message ?? string.Empty;
        }

        public bool IsSuccess { get; }
        public string Message { get; }
        /// <summary>
        /// Handles the Success workflow for this component.
        /// </summary>
        public static RerunControlCommandResult Success(string message = "") =>
            new RerunControlCommandResult(true, message);
        /// <summary>
        /// Handles the Failure workflow for this component.
        /// </summary>
        public static RerunControlCommandResult Failure(string message) =>
            new RerunControlCommandResult(false, message);
    }
    /// <summary>
    /// Carries Rerun Control Vector3 data across Unity2Rerun runtime boundaries.
    /// </summary>
    public readonly struct RerunControlVector3
    {
        public RerunControlVector3(float x, float y, float z)
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
    /// Carries Rerun Control Color data across Unity2Rerun runtime boundaries.
    /// </summary>
    public readonly struct RerunControlColor
    {
        public RerunControlColor(float r, float g, float b, float a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public float R { get; }
        public float G { get; }
        public float B { get; }
        public float A { get; }
    }
    /// <summary>
    /// Carries Rerun Control Command data across Unity2Rerun runtime boundaries.
    /// </summary>
    public struct RerunControlCommand
    {
        public RerunControlCommandType Type { get; private set; }
        public RerunControlVector3 Position { get; private set; }
        public bool HasPosition { get; private set; }
        public RerunControlVector3 RotationEuler { get; private set; }
        public bool HasRotationEuler { get; private set; }
        public float Scale { get; private set; }
        public bool HasScale { get; private set; }
        public RerunControlColor Color { get; private set; }
        public bool HasColor { get; private set; }
        /// <summary>
        /// Parses the external representation into the SDK model.
        /// </summary>
        public static bool TryParseJson(string json, out RerunControlCommand command, out string error)
        {
            command = default;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(json))
            {
                error = "Command JSON is empty.";
                return false;
            }

            if (!TryReadString(json, "type", out var typeValue))
            {
                error = "Command JSON must include a string 'type'.";
                return false;
            }

            if (!TryParseType(typeValue, out var type))
            {
                error = $"Unsupported command type '{typeValue}'.";
                return false;
            }

            command.Type = type;

            if (TryReadVector3(json, "position", out var position))
            {
                command.Position = position;
                command.HasPosition = true;
            }

            if (TryReadVector3(json, "rotationEuler", out var rotation))
            {
                command.RotationEuler = rotation;
                command.HasRotationEuler = true;
            }

            if (TryReadSingle(json, "scale", out var scale))
            {
                command.Scale = scale;
                command.HasScale = true;
            }

            if (TryReadColor(json, "color", out var color))
            {
                command.Color = color;
                command.HasColor = true;
            }

            if (type == RerunControlCommandType.SetScale && !command.HasScale)
            {
                error = "set_scale command requires 'scale'.";
                return false;
            }

            if (type == RerunControlCommandType.SetColor && !command.HasColor)
            {
                error = "set_color command requires 'color'.";
                return false;
            }

            if (type == RerunControlCommandType.SetPose &&
                !command.HasPosition && !command.HasRotationEuler)
            {
                error = "set_pose command requires 'position' or 'rotationEuler'.";
                return false;
            }

            return true;
        }

        private static bool TryParseType(string value, out RerunControlCommandType type)
        {
            switch (value)
            {
                case "set_pose":
                    type = RerunControlCommandType.SetPose;
                    return true;
                case "set_scale":
                    type = RerunControlCommandType.SetScale;
                    return true;
                case "set_color":
                    type = RerunControlCommandType.SetColor;
                    return true;
                case "reset_pose":
                    type = RerunControlCommandType.ResetPose;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }

        private static bool TryReadString(string json, string name, out string value)
        {
            value = string.Empty;
            var key = "\"" + name + "\"";
            var keyIndex = json.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0) return false;

            var colon = json.IndexOf(':', keyIndex + key.Length);
            if (colon < 0) return false;

            var start = json.IndexOf('"', colon + 1);
            if (start < 0) return false;
            var end = json.IndexOf('"', start + 1);
            if (end < 0) return false;

            value = json.Substring(start + 1, end - start - 1);
            return true;
        }

        private static bool TryReadVector3(string json, string name, out RerunControlVector3 value)
        {
            value = default;
            if (!TryReadFloatArray(json, name, 3, out var values))
                return false;

            value = new RerunControlVector3(values[0], values[1], values[2]);
            return true;
        }

        private static bool TryReadColor(string json, string name, out RerunControlColor value)
        {
            value = default;
            if (!TryReadFloatArray(json, name, 4, out var values))
                return false;

            value = new RerunControlColor(values[0], values[1], values[2], values[3]);
            return true;
        }

        private static bool TryReadSingle(string json, string name, out float value)
        {
            value = default;
            var token = ReadRawValue(json, name);
            if (token == null) return false;

            return float.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadFloatArray(string json, string name, int expectedCount, out float[] values)
        {
            values = Array.Empty<float>();
            var key = "\"" + name + "\"";
            var keyIndex = json.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0) return false;

            var colon = json.IndexOf(':', keyIndex + key.Length);
            if (colon < 0) return false;

            var start = json.IndexOf('[', colon + 1);
            if (start < 0) return false;
            var end = json.IndexOf(']', start + 1);
            if (end < 0) return false;

            var pieces = json.Substring(start + 1, end - start - 1)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (pieces.Length != expectedCount) return false;

            values = new float[expectedCount];
            for (var i = 0; i < pieces.Length; i++)
            {
                if (!float.TryParse(pieces[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                    return false;
            }

            return true;
        }

        private static string ReadRawValue(string json, string name)
        {
            var key = "\"" + name + "\"";
            var keyIndex = json.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0) return null;

            var colon = json.IndexOf(':', keyIndex + key.Length);
            if (colon < 0) return null;

            var start = colon + 1;
            while (start < json.Length && char.IsWhiteSpace(json[start]))
                start++;

            var end = start;
            while (end < json.Length && json[end] != ',' && json[end] != '}')
                end++;

            return json.Substring(start, end - start).Trim();
        }
    }
    /// <summary>
    /// Carries Rerun Control State data across Unity2Rerun runtime boundaries.
    /// </summary>
    public struct RerunControlState
    {
        public RerunControlVector3 Position { get; set; }
        public RerunControlVector3 RotationEuler { get; set; }
        public float Scale { get; set; }
        public RerunControlColor Color { get; set; }
        public int CommandCount { get; set; }
        public string LastCommand { get; set; }
        public string ControlUrl { get; set; }
        /// <summary>
        /// Handles the ToJson workflow for this component.
        /// </summary>
        public string ToJson()
        {
            var sb = new StringBuilder(256);
            sb.Append('{');
            AppendPose(sb);
            sb.Append(',');
            AppendVector3(sb, "position", Position);
            sb.Append(',');
            AppendVector3(sb, "rotationEuler", RotationEuler);
            sb.Append(',');
            sb.Append("\"scale\":").Append(Format(Scale)).Append(',');
            AppendColor(sb, "color", Color);
            sb.Append(',');
            sb.Append("\"commandCount\":").Append(CommandCount).Append(',');
            sb.Append("\"lastCommand\":\"").Append(Escape(LastCommand ?? string.Empty)).Append("\",");
            sb.Append("\"controlUrl\":\"").Append(Escape(ControlUrl ?? string.Empty)).Append("\",");
            AppendActions(sb);
            sb.Append(',');
            AppendParameters(sb);
            sb.Append('}');
            return sb.ToString();
        }

        private static void AppendVector3(StringBuilder sb, string name, RerunControlVector3 value)
        {
            sb.Append('"').Append(name).Append("\":[")
                .Append(Format(value.X)).Append(',')
                .Append(Format(value.Y)).Append(',')
                .Append(Format(value.Z)).Append(']');
        }

        private static void AppendColor(StringBuilder sb, string name, RerunControlColor value)
        {
            sb.Append('"').Append(name).Append("\":[")
                .Append(Format(value.R)).Append(',')
                .Append(Format(value.G)).Append(',')
                .Append(Format(value.B)).Append(',')
                .Append(Format(value.A)).Append(']');
        }

        private void AppendPose(StringBuilder sb)
        {
            sb.Append("\"pose\":{");
            AppendVector3(sb, "position", Position);
            sb.Append(',');
            AppendVector3(sb, "rotationEuler", RotationEuler);
            sb.Append(',');
            AppendVector3(sb, "scale", new RerunControlVector3(Scale, Scale, Scale));
            sb.Append('}');
        }

        private void AppendActions(StringBuilder sb)
        {
            sb.Append("\"actions\":[");
            AppendAction(sb, "reset_pose", "Reset Pose", "button", "{\"type\":\"reset_pose\"}");
            sb.Append(',');
            AppendAction(sb, "set_color_green", "Green", "preset", "{\"type\":\"set_color\",\"color\":[0,1,0,1]}");
            sb.Append(',');
            AppendAction(sb, "set_color_red", "Red", "preset", "{\"type\":\"set_color\",\"color\":[1,0.1,0.05,1]}");
            sb.Append(',');
            AppendAction(sb, "set_color_blue", "Blue", "preset", "{\"type\":\"set_color\",\"color\":[0.25,0.5,1,1]}");
            sb.Append(',');
            AppendAction(sb, "scale_down", "Scale Down", "button", "{\"type\":\"set_scale\",\"scale\":" + Format(Math.Max(0.1f, Scale * 0.8f)) + "}");
            sb.Append(',');
            AppendAction(sb, "scale_up", "Scale Up", "button", "{\"type\":\"set_scale\",\"scale\":" + Format(Math.Max(0.1f, Scale * 1.25f)) + "}");
            sb.Append(',');
            AppendAction(sb, "scale_reset", "Scale Reset", "button", "{\"type\":\"set_scale\",\"scale\":1}");
            sb.Append(']');
        }

        private static void AppendAction(StringBuilder sb, string id, string label, string kind, string commandJson)
        {
            sb.Append('{')
                .Append("\"id\":\"").Append(Escape(id)).Append("\",")
                .Append("\"label\":\"").Append(Escape(label)).Append("\",")
                .Append("\"kind\":\"").Append(Escape(kind)).Append("\",")
                .Append("\"command\":").Append(commandJson)
                .Append('}');
        }

        private void AppendParameters(StringBuilder sb)
        {
            sb.Append("\"parameters\":[");
            sb.Append('{')
                .Append("\"name\":\"cube.color\",")
                .Append("\"label\":\"Color\",")
                .Append("\"type\":\"color\",")
                .Append("\"writable\":true,")
                .Append("\"value\":");
            AppendColorValue(sb, Color);
            sb.Append('}');
            sb.Append(',');
            sb.Append('{')
                .Append("\"name\":\"cube.scale\",")
                .Append("\"label\":\"Scale\",")
                .Append("\"type\":\"float\",")
                .Append("\"writable\":true,")
                .Append("\"value\":").Append(Format(Scale))
                .Append('}');
            sb.Append(']');
        }

        private static void AppendColorValue(StringBuilder sb, RerunControlColor value)
        {
            sb.Append('[')
                .Append(Format(value.R)).Append(',')
                .Append(Format(value.G)).Append(',')
                .Append(Format(value.B)).Append(',')
                .Append(Format(value.A)).Append(']');
        }

        private static string Format(float value) =>
            value.ToString("0.########", CultureInfo.InvariantCulture);

        private static string Escape(string value) =>
            value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

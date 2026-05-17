// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Unity
// Purpose: Integrates managed Rerun logging with Unity runtime components.

using System.ComponentModel;

namespace Unity.RerunSDK.Unity
{
    /// <summary>
    /// Enumerates supported Rerun Generated Log Kind values.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum RerunGeneratedLogKind
    {
        TextLog,
        Scalar,
        Transform3D,
    }
    /// <summary>
    /// Carries Rerun Generated Log Entry data across Unity2Rerun runtime boundaries.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct RerunGeneratedLogEntry
    {
        public RerunGeneratedLogEntry(
            string entityPath,
            RerunGeneratedLogKind kind,
            float rateHz,
            string level = "INFO")
        {
            EntityPath = entityPath;
            Kind = kind;
            RateHz = rateHz;
            Level = string.IsNullOrEmpty(level) ? "INFO" : level;
        }

        public string EntityPath { get; }
        public RerunGeneratedLogKind Kind { get; }
        public float RateHz { get; }
        public string Level { get; }
    }
    /// <summary>
    /// Defines the contract for IRerun Generated Log Source.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRerunGeneratedLogSource
    {
        int RerunLog_EntryCount { get; }
        RerunGeneratedLogEntry RerunLog_GetEntry(int index);
        void RerunLog_Publish(int index, RerunManager manager);
    }
}

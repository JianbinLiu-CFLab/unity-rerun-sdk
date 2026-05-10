// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;

namespace Unity.RerunSDK.Unity
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum RerunGeneratedLogKind
    {
        TextLog,
        Scalar,
        Transform3D,
    }

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

    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IRerunGeneratedLogSource
    {
        int RerunLog_EntryCount { get; }
        RerunGeneratedLogEntry RerunLog_GetEntry(int index);
        void RerunLog_Publish(int index, RerunManager manager);
    }
}

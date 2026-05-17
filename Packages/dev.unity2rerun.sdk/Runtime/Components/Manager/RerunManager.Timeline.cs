// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Components/Manager
// Purpose: Exposes timeline controls used by later log calls.

using Unity.RerunSDK.Core;

namespace Unity.RerunSDK.Unity
{
    public partial class RerunManager
    {
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void SetTimeSequence(string name, long value)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTimeline(name, value, RerunTimelineKind.Sequence);
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void SetTimeTimestampNs(string name, long unixNs)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTimeline(name, unixNs, RerunTimelineKind.TimestampNs);
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void SetTimeDurationNs(string name, long durationNs)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTimeline(name, durationNs, RerunTimelineKind.DurationNs);
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void SetTime(string timelineName, long value)
        {
            if (_runtime == null || !IsRecording) return;
            _runtime.SetTime(new RerunTimeline(timelineName), value);
        }
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void ResetTime(string name) => _runtime?.ResetTime(name);
        /// <summary>
        /// Updates the active Rerun timeline state used by later log calls.
        /// </summary>
        public void ResetAllTimes() => _runtime?.ResetAllTimes();
    }
}

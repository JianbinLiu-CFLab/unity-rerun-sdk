// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Core
// Purpose: Defines public runtime configuration options for Rerun recording.

namespace Unity.RerunSDK.Core
{
    /// <summary>
    /// Selects compression for RRD file recording payloads.
    /// </summary>
    public enum RerunRecordingCompression
    {
        /// <summary>Write Arrow IPC payloads without compression.</summary>
        None,

        /// <summary>Write Arrow IPC payloads using Rerun-compatible raw LZ4 block compression.</summary>
        Lz4,
    }
}

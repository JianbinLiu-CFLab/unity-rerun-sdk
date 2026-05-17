// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Core
// Purpose: Defines core Rerun runtime concepts shared by encoding, transport, and Unity layers.

namespace Unity.RerunSDK.Core
{
    /// <summary>
    /// Enumerates supported Rerun Live State values.
    /// </summary>
    public enum RerunLiveState
    {
        Disabled,
        LaunchingViewer,
        Connecting,
        Connected,
        Reconnecting,
        Failed,
        Disconnected,
    }
}

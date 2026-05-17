// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Core
// Purpose: Defines core Rerun runtime concepts shared by encoding, transport, and Unity layers.

using Unity.RerunSDK.Encoding;

namespace Unity.RerunSDK.Core
{
    /// <summary>
    /// Defines the contract for IRerun Backend.
    /// </summary>
    internal interface IRerunBackend
    {
        void Initialize(RerunRuntime runtime);
        void Write(EncodedRerunMessage message);
        void Flush();
        void Shutdown();
    }
}

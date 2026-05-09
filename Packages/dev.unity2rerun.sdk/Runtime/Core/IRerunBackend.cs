// SPDX-License-Identifier: Apache-2.0

using Unity.RerunSDK.Encoding;

namespace Unity.RerunSDK.Core
{
    internal interface IRerunBackend
    {
        void Initialize(RerunRuntime runtime);
        void Write(EncodedRerunMessage message);
        void Flush();
        void Shutdown();
    }
}

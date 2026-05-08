// SPDX-License-Identifier: Apache-2.0

namespace Unity.RerunSDK.Core
{
    /// Backend interface for output targets (.rrd file, gRPC live, etc.).
    internal interface IRerunBackend
    {
        void Initialize(RerunRuntime runtime);
        void WriteArrowMsg(byte[] arrowMsgPayload);
        void Flush();
        void Shutdown();
    }
}

// SPDX-License-Identifier: Apache-2.0

namespace Unity.RerunSDK.Core
{
    /// Backend interface for output targets (.rrd file, gRPC live, etc.).
    public interface IRerunBackend
    {
        void Initialize(RerunRuntime runtime);
        void WriteMessage(byte[] payload);
        void Flush();
        void Shutdown();
    }
}

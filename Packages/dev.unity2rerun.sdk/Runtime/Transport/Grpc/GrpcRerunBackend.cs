// SPDX-License-Identifier: Apache-2.0

using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;

namespace Unity.RerunSDK.Transport.Grpc
{
    internal class GrpcRerunBackend : IRerunBackend
    {
        private readonly RerunGrpcClient _client;

        public GrpcRerunBackend(RerunGrpcClient client)
        {
            _client = client;
        }

        public void Initialize(RerunRuntime runtime)
        {
            _client.Start();
        }

        public void Write(EncodedRerunMessage message)
        {
            _client.Write(message);
        }

        public void Flush()
        {
            // gRPC flush is fire-and-forget; no server ack expected
        }

        public void Shutdown()
        {
            _client.Dispose();
        }
    }
}

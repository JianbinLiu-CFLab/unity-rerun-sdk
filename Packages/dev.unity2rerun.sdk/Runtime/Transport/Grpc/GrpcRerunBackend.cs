// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Transport/Grpc
// Purpose: Streams encoded Rerun messages to a live Rerun Viewer over Grpc.

using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;

namespace Unity.RerunSDK.Transport.Grpc
{
    /// <summary>
    /// Provides Grpc Rerun Backend support for Unity2Rerun.
    /// </summary>
    internal class GrpcRerunBackend : IRerunBackend
    {
        private readonly RerunGrpcClient _client;

        public GrpcRerunBackend(RerunGrpcClient client)
        {
            _client = client;
        }
        /// <summary>
        /// Initializes the backend before messages are written.
        /// </summary>
        public void Initialize(RerunRuntime runtime)
        {
            _client.Start();
        }
        /// <summary>
        /// Writes one encoded Rerun message to the backend.
        /// </summary>
        public void Write(EncodedRerunMessage message)
        {
            _client.Write(message);
        }
        /// <summary>
        /// Flushes buffered output without changing ownership or finalization state.
        /// </summary>
        public void Flush()
        {
            // Grpc flush is fire-and-forget; no server ack expected
        }
        /// <summary>
        /// Stops the component or service and releases owned runtime resources.
        /// </summary>
        public void Shutdown()
        {
            _client.Dispose();
        }
    }
}

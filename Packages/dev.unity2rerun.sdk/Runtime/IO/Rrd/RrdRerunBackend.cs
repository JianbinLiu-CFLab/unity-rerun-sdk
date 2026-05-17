// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/IO/Rrd
// Purpose: Writes Rerun RRD stream records, manifests, and footer metadata.

using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;

namespace Unity.RerunSDK.IO.Rrd
{
    /// <summary>
    /// Provides RRD Rerun Backend support for Unity2Rerun.
    /// </summary>
    internal class RrdRerunBackend : IRerunBackend
    {
        private readonly RrdWriter _writer;
        private readonly RrdFooterBuilder _footerBuilder = new RrdFooterBuilder();

        public RrdRerunBackend(RrdWriter writer)
        {
            _writer = writer;
        }
        /// <summary>
        /// Initializes the backend before messages are written.
        /// </summary>
        public void Initialize(RerunRuntime runtime)
        {
            _writer.WriteStreamHeader();
        }
        /// <summary>
        /// Writes one encoded Rerun message to the backend.
        /// </summary>
        public void Write(EncodedRerunMessage message)
        {
            var span = _writer.WriteMessage(message.RrdKind, message.RrdPayload);
            if (message.ManifestChunkInfo != null)
                _footerBuilder.AddChunk(message.ManifestChunkInfo, span);
        }
        /// <summary>
        /// Flushes buffered output without changing ownership or finalization state.
        /// </summary>
        public void Flush()
        {
            _writer.Flush();
        }
        /// <summary>
        /// Stops the component or service and releases owned runtime resources.
        /// </summary>
        public void Shutdown()
        {
            _writer.FinishWithFooter(_footerBuilder.Build());
        }
    }
}

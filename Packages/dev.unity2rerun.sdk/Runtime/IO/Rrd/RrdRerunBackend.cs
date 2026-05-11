// SPDX-License-Identifier: Apache-2.0

using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;

namespace Unity.RerunSDK.IO.Rrd
{
    internal class RrdRerunBackend : IRerunBackend
    {
        private readonly RrdWriter _writer;
        private readonly RrdFooterBuilder _footerBuilder = new RrdFooterBuilder();

        public RrdRerunBackend(RrdWriter writer)
        {
            _writer = writer;
        }

        public void Initialize(RerunRuntime runtime)
        {
            _writer.WriteStreamHeader();
        }

        public void Write(EncodedRerunMessage message)
        {
            var span = _writer.WriteMessage(message.RrdKind, message.RrdPayload);
            if (message.ManifestChunkInfo != null)
                _footerBuilder.AddChunk(message.ManifestChunkInfo, span);
        }

        public void Flush()
        {
            _writer.Flush();
        }

        public void Shutdown()
        {
            _writer.FinishWithFooter(_footerBuilder.Build());
        }
    }
}

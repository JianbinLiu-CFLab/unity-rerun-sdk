// SPDX-License-Identifier: Apache-2.0

using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;

namespace Unity.RerunSDK.IO.Rrd
{
    internal class RrdRerunBackend : IRerunBackend
    {
        private readonly RrdWriter _writer;

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
            _writer.WriteMessage(message.RrdKind, message.RrdPayload);
        }

        public void Flush()
        {
            _writer.FinishNoFooter();
        }

        public void Shutdown()
        {
            _writer.FinishNoFooter();
        }
    }
}

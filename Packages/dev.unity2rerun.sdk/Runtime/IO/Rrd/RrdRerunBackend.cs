// SPDX-License-Identifier: Apache-2.0

using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;

namespace Unity.RerunSDK.IO.Rrd
{
    internal class RrdRerunBackend : IRerunBackend
    {
        private readonly RrdWriter _writer;
        private readonly ManagedRerunEncoder _encoder;
        private readonly string _applicationId;

        public RrdRerunBackend(RrdWriter writer, ManagedRerunEncoder encoder, string applicationId)
        {
            _writer = writer;
            _encoder = encoder;
            _applicationId = applicationId;
        }

        public void Initialize(RerunRuntime runtime)
        {
            _writer.WriteStreamHeader();
            var msg = _encoder.EncodeSetStoreInfoMessage(runtime.RecordingId, _applicationId);
            Write(msg);
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

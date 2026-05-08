// SPDX-License-Identifier: Apache-2.0

using Unity.RerunSDK.Core;
using Unity.RerunSDK.Encoding;

namespace Unity.RerunSDK.IO.Rrd
{
    /// Backend that writes RRD-encoded messages directly to an .rrd file.
    internal class RrdRerunBackend : IRerunBackend
    {
        private readonly RrdWriter _writer;
        private readonly ManagedRerunEncoder _encoder;
        private readonly string _applicationId;
        private RerunRuntime _runtime;

        public RrdRerunBackend(RrdWriter writer, ManagedRerunEncoder encoder, string applicationId)
        {
            _writer = writer;
            _encoder = encoder;
            _applicationId = applicationId;
        }

        public void Initialize(RerunRuntime runtime)
        {
            _runtime = runtime;
            _writer.WriteStreamHeader();

            var setStoreInfo = _encoder.EncodeSetStoreInfo(runtime.RecordingId, _applicationId);
            _writer.WriteMessage(RrdConstants.MsgKindSetStoreInfo, setStoreInfo);
        }

        public void WriteArrowMsg(byte[] arrowMsgPayload)
        {
            _writer.WriteMessage(RrdConstants.MsgKindArrowMsg, arrowMsgPayload);
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

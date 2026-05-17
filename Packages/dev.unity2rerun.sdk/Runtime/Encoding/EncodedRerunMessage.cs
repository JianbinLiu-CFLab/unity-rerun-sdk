// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Encoding
// Purpose: Defines managed Rerun encoding primitives used by RRD files and live transport.

#nullable enable

namespace Unity.RerunSDK.Encoding
{
    /// Transport envelope for a single Rerun message.
    /// Carries both the RRD inner payload (for .rrd file writing)
    /// and the Grpc outer LogMsg oneof (for live transport).
    internal readonly struct EncodedRerunMessage
    {
        /// RRD message kind: SetStoreInfo=1, ArrowMsg=2.
        public ulong RrdKind { get; }

        /// Protobuf-encoded inner message payload for RRD stream writing.
        public byte[] RrdPayload { get; }

        /// Protobuf-encoded outer LogMsg oneof payload for Grpc WriteMessagesRequest.
        public byte[] GrpcLogMsgBytes { get; }

        /// Whether this message is a SetStoreInfo (needed for reconnect cache).
        public bool IsStoreInfo { get; }

        /// Whether this message contains static data (for ArrowMsg.is_static).
        public bool IsStatic { get; }

        /// Manifest metadata for ArrowMsg chunks, filled by the encoder side.
        public RrdManifestChunkInfo? ManifestChunkInfo { get; }

        public EncodedRerunMessage(
            ulong rrdKind,
            byte[] rrdPayload,
            byte[] grpcLogMsgBytes,
            bool isStoreInfo,
            bool isStatic,
            RrdManifestChunkInfo? manifestChunkInfo = null)
        {
            RrdKind = rrdKind;
            RrdPayload = rrdPayload;
            GrpcLogMsgBytes = grpcLogMsgBytes;
            IsStoreInfo = isStoreInfo;
            IsStatic = isStatic;
            ManifestChunkInfo = manifestChunkInfo;
        }
    }
}

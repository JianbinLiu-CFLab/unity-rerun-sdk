// Copyright (c) 2026 Jianbin Liu and Unity2Rerun contributors.
// SPDX-License-Identifier: Apache-2.0
//
// Module: Runtime/Encoding
// Purpose: Defines managed Rerun encoding primitives used by RRD files and live transport.

// Phase 2: Minimal hand-written protobuf encoder for Rerun transport messages.
// Covers SetStoreInfo and ArrowMsg for TextLog only.
// A full protobuf generation strategy is tracked for Phase3+.

using System;
using System.IO;
using TxtEncoding = System.Text.Encoding;

namespace Unity.RerunSDK.Encoding
{
    /// Minimal hand-written protobuf encoder for Rerun RRD transport messages.
    /// Avoids external protobuf library dependency in the UPM package.
    /// Covers the exact field set needed for Phase 2: SetStoreInfo + ArrowMsg(TextLog).
    internal static class RerunProtobufEncoding
    {
        // -- Wire format helpers --

        private static void WriteVarint(Stream s, ulong value)
        {
            while (value >= 0x80)
            {
                s.WriteByte((byte)(value | 0x80));
                value >>= 7;
            }
            s.WriteByte((byte)value);
        }

        private static void WriteTag(Stream s, int fieldNumber, int wireType)
        {
            var tag = ((ulong)(uint)fieldNumber << 3) | (uint)wireType;
            WriteVarint(s, tag);
        }

        private static void WriteUInt64(Stream s, int field, ulong value)
        {
            WriteTag(s, field, 0); // varint
            WriteVarint(s, value);
        }

        private static void WriteFixed64(Stream s, int field, ulong value)
        {
            WriteTag(s, field, 1); // 64-bit
            var buf = BitConverter.GetBytes(value);
            s.Write(buf, 0, 8);
        }

        private static void WriteLengthDelimited(Stream s, int field, byte[] data)
        {
            WriteTag(s, field, 2); // length-delimited
            WriteVarint(s, (ulong)data.Length);
            s.Write(data, 0, data.Length);
        }

        private static void WriteString(Stream s, int field, string value)
        {
            WriteLengthDelimited(s, field, TxtEncoding.UTF8.GetBytes(value ?? ""));
        }

        private static byte[] EncodeVarint(ulong value)
        {
            using var ms = new MemoryStream();
            WriteVarint(ms, value);
            return ms.ToArray();
        }

        // -- Tuid encoding --

        /// Tuid has optional fixed64 fields: time_ns(1), inc(2)
        public static byte[] EncodeTuid(ulong timeNs, ulong inc)
        {
            using var ms = new MemoryStream();
            WriteFixed64(ms, 1, timeNs);
            WriteFixed64(ms, 2, inc);
            return ms.ToArray();
        }

        // -- StoreId encoding --

        /// StoreId: kind(1, enum=int32), recording_id(2, string), application_id(3, msg)
        public static byte[] EncodeStoreId(int kind, string recordingId, string applicationId)
        {
            using var ms = new MemoryStream();
            WriteUInt64(ms, 1, (ulong)kind);
            WriteString(ms, 2, recordingId);

            // ApplicationId is a nested message: field 3
            var appIdBytes = EncodeApplicationId(applicationId);
            WriteLengthDelimited(ms, 3, appIdBytes);

            return ms.ToArray();
        }

        private static byte[] EncodeApplicationId(string id)
        {
            using var ms = new MemoryStream();
            WriteString(ms, 1, id);
            return ms.ToArray();
        }

        // -- StoreSource encoding --

        /// StoreSource: kind(1), extra(2). Kind=6=Other.
        public static byte[] EncodeStoreSource(string extraPayload)
        {
            using var ms = new MemoryStream();
            WriteUInt64(ms, 1, 6); // STORE_SOURCE_KIND_OTHER
            // extra is StoreSourceExtra { payload(bytes=1) }
            var extraBytes = EncodeStoreSourceExtra(extraPayload);
            WriteLengthDelimited(ms, 2, extraBytes);
            return ms.ToArray();
        }

        private static byte[] EncodeStoreSourceExtra(string payload)
        {
            using var ms = new MemoryStream();
            WriteLengthDelimited(ms, 1, TxtEncoding.UTF8.GetBytes(payload));
            return ms.ToArray();
        }

        // -- StoreVersion encoding --

        /// StoreVersion: crate_version_bits(1, int32)
        private static byte[] EncodeStoreVersion()
        {
            using var ms = new MemoryStream();
            WriteUInt64(ms, 1, (ulong)0x00170000); // 0.23.0
            return ms.ToArray();
        }

        // -- SetStoreInfo encoding --

        /// SetStoreInfo: row_id(1, Tuid), info(2, StoreInfo)
        public static byte[] EncodeSetStoreInfo(
            string recordingId,
            string applicationId,
            ulong rowIdTimeNs,
            ulong rowIdInc)
        {
            using var ms = new MemoryStream();

            // row_id (field 1, Tuid message)
            var tuidBytes = EncodeTuid(rowIdTimeNs, rowIdInc);
            WriteLengthDelimited(ms, 1, tuidBytes);

            // info (field 2, StoreInfo message)
            var storeInfoBytes = EncodeStoreInfo(recordingId, applicationId);
            WriteLengthDelimited(ms, 2, storeInfoBytes);

            return ms.ToArray();
        }

        private static byte[] EncodeStoreInfo(string recordingId, string applicationId)
        {
            using var ms = new MemoryStream();

            // application_id (field 1, deprecated but still sent for compat)
            var appIdBytes = EncodeApplicationId(applicationId);
            WriteLengthDelimited(ms, 1, appIdBytes);

            // store_id (field 2)
            var storeIdBytes = EncodeStoreId(1, recordingId, applicationId); // kind=1=RECORDING
            WriteLengthDelimited(ms, 2, storeIdBytes);

            // store_source (field 5)
            var sourceBytes = EncodeStoreSource("unity2rerun");
            WriteLengthDelimited(ms, 5, sourceBytes);

            // store_version (field 6)
            var versionBytes = EncodeStoreVersion();
            WriteLengthDelimited(ms, 6, versionBytes);

            return ms.ToArray();
        }

        // -- ArrowMsg encoding --

        /// ArrowMsg: store_id(1), chunk_id(6, optional), compression(2), uncompressed_size(3),
        /// encoding(4), payload(5), is_static(7, optional)
        public static byte[] EncodeArrowMsg(
            string recordingId,
            string applicationId,
            ulong chunkIdTimeNs,
            ulong chunkIdInc,
            int compression, // 1=NONE, 2=LZ4
            ulong uncompressedSize,
            byte[] arrowIpcPayload,
            bool isStatic = false)
        {
            using var ms = new MemoryStream();

            // store_id (field 1)
            var storeIdBytes = EncodeStoreId(1, recordingId, applicationId);
            WriteLengthDelimited(ms, 1, storeIdBytes);

            // chunk_id (field 6, optional)
            var chunkIdBytes = EncodeTuid(chunkIdTimeNs, chunkIdInc);
            WriteLengthDelimited(ms, 6, chunkIdBytes);

            // compression (field 2, enum)
            WriteUInt64(ms, 2, (ulong)compression);

            // uncompressed_size (field 3, uint64)
            WriteUInt64(ms, 3, uncompressedSize);

            // encoding (field 4, enum: 1=ARROW_IPC)
            WriteUInt64(ms, 4, 1); // ENCODING_ARROW_IPC

            // payload (field 5, bytes)
            WriteLengthDelimited(ms, 5, arrowIpcPayload);

            // is_static (field 7, optional bool)
            if (isStatic)
            {
                WriteUInt64(ms, 7, 1);
            }

            return ms.ToArray();
        }
        // -- Grpc LogMsg outer wrapper encoding --

        /// Wrap a SetStoreInfo inner payload as an outer LogMsg oneof.
        /// RRD stream writes the inner payload directly; Grpc WriteMessagesRequest
        /// requires the outer LogMsg wrapper with oneof tag.
        public static byte[] WrapSetStoreInfoAsLogMsg(byte[] setStoreInfoInnerPayload)
        {
            // LogMsg: field 1 (SetStoreInfo) = wire type 2 (len-delimited)
            return WrapAsLogMsg(1, setStoreInfoInnerPayload);
        }

        /// Wrap an ArrowMsg inner payload as an outer LogMsg oneof.
        public static byte[] WrapArrowMsgAsLogMsg(byte[] arrowMsgInnerPayload)
        {
            return WrapAsLogMsg(2, arrowMsgInnerPayload);
        }

        private static byte[] WrapAsLogMsg(int oneofFieldNum, byte[] innerPayload)
        {
            using var ms = new MemoryStream();
            WriteLengthDelimited(ms, oneofFieldNum, innerPayload);
            return ms.ToArray();
        }
    }
}

// SPDX-License-Identifier: Apache-2.0
//
// Arrow IPC writer using Apache.Arrow library.
// Builds TextLog RecordBatch with Rerun Sorbet schema.

using System.Collections.Generic;
using System.IO;
using Apache.Arrow;
using Apache.Arrow.Arrays;
using Apache.Arrow.Ipc;
using Apache.Arrow.Types;

namespace Unity.RerunSDK.Encoding
{
    public static class RerunArrowIpcWriter
    {
        /// Build a TextLog Arrow IPC stream for a single row.
        public static byte[] BuildTextLogPayload(
            string entityPath, string text, string level,
            long logTick, ulong rowIdTimeNs, ulong rowIdInc)
        {
            var schema = BuildTextLogSchema(entityPath);
            var batch = BuildTextLogRecordBatch(schema, text, level, logTick, rowIdTimeNs, rowIdInc);
            return SerializeToIpc(schema, batch);
        }

        private static Schema BuildTextLogSchema(string entityPath)
        {
            // row_id: FixedSizeBinary(16)
            var rowIdMeta = new Dictionary<string, string>
            {
                { "rerun:kind", "control" },
                { "ARROW:extension:name", "rerun.datatypes.TUID" },
                { "ARROW:extension:metadata", @"{""namespace"":""row""}" }
            };
            var rowIdField = new Field("row_id", new FixedSizeBinaryType(16), false, rowIdMeta);

            // log_tick: Int64
            var tickMeta = new Dictionary<string, string>
            {
                { "rerun:kind", "index" },
                { "rerun:index_name", "log_tick" }
            };
            var tickField = new Field("log_tick", Int64Type.Default, false, tickMeta);

            // text: Utf8
            var textMeta = new Dictionary<string, string>
            {
                { "rerun:kind", "data" },
                { "rerun:component", "TextLog:text" },
                { "rerun:archetype", "rerun.archetypes.TextLog" },
                { "rerun:component_type", "rerun.components.Text" }
            };
            var textField = new Field("TextLog:text", StringType.Default, true, textMeta);

            // level: Utf8
            var levelMeta = new Dictionary<string, string>
            {
                { "rerun:kind", "data" },
                { "rerun:component", "TextLog:level" },
                { "rerun:archetype", "rerun.archetypes.TextLog" },
                { "rerun:component_type", "rerun.components.TextLogLevel" }
            };
            var levelField = new Field("TextLog:level", StringType.Default, true, levelMeta);

            var batchMeta = new Dictionary<string, string>
            {
                { "sorbet:version", "0.1.3" },
                { "rerun:id", "16A36600D1590000017E00005EA2E000" },
                { "rerun:entity_path", entityPath }
            };

            return new Schema(
                new Field[] { rowIdField, tickField, textField, levelField },
                batchMeta);
        }

        private static RecordBatch BuildTextLogRecordBatch(
            Schema schema, string text, string level,
            long logTick, ulong rowIdTimeNs, ulong rowIdInc)
        {
            // row_id: FixedSizeBinary(16), big-endian
            var rowIdData = CreateFixedSizeBinary16(rowIdTimeNs, rowIdInc);
            var rowIdArray = BuildFixedSizeBinaryArray(16, new[] { rowIdData });

            // log_tick: Int64
            var tickBuilder = new Int64Array.Builder();
            tickBuilder.Append(logTick);

            // text: Utf8
            var textBuilder = new StringArray.Builder();
            textBuilder.Append(text ?? "", System.Text.Encoding.UTF8);

            // level: Utf8
            var levelBuilder = new StringArray.Builder();
            levelBuilder.Append(level ?? "INFO", System.Text.Encoding.UTF8);

            return new RecordBatch(schema, new IArrowArray[]
            {
                rowIdArray,
                tickBuilder.Build(default),
                textBuilder.Build(default),
                levelBuilder.Build(default)
            }, 1);
        }

        private static byte[] SerializeToIpc(Schema schema, RecordBatch batch)
        {
            using var ms = new MemoryStream();
            using (var writer = new ArrowStreamWriter(ms, schema, leaveOpen: true))
            {
                writer.WriteRecordBatch(batch);
            }
            return ms.ToArray();
        }

        private static FixedSizeBinaryArray BuildFixedSizeBinaryArray(int byteWidth, byte[][] values)
        {
            var type = new FixedSizeBinaryType(byteWidth);
            var validityBuffer = ArrowBuffer.Empty;

            var totalBytes = 0;
            foreach (var v in values) totalBytes += v.Length;
            var valuesData = new byte[totalBytes];
            var pos = 0;
            foreach (var v in values)
            {
                System.Array.Copy(v, 0, valuesData, pos, v.Length);
                pos += v.Length;
            }
            var valuesBuf = new ArrowBuffer(new System.ReadOnlyMemory<byte>(valuesData));

            var arrayData = new ArrayData(type, values.Length, 0, 0,
                new[] { validityBuffer, valuesBuf }, null);
            return new FixedSizeBinaryArray(arrayData);
        }

        private static byte[] CreateFixedSizeBinary16(ulong timeNs, ulong inc)
        {
            // Tuid: big-endian [timeNs: 8][inc: 8]
            var bytes = new byte[16];
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(0, 8), timeNs);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(8, 8), inc);
            return bytes;
        }
    }
}

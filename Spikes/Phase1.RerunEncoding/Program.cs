// Phase 1: Pure C# Rerun encoding spike
// Generates min_text_log.rrd that Rerun Viewer can open with a single TextLog entry.

using Apache.Arrow;
using Apache.Arrow.Arrays;
using Apache.Arrow.Ipc;
using Google.Protobuf;
using Phase1.RerunEncoding;
using ArrowSchema = Apache.Arrow.Schema;
using RerunLogMsg = Rerun.LogMsg.V1Alpha1;
using RerunCommon = Rerun.Common.V1Alpha1;

const string OutDir = "out";
const string OutFile = "out/min_text_log.rrd";

Directory.CreateDirectory(OutDir);

using var fileStream = File.Create(OutFile);
using var writer = new RrdWriter(fileStream);

// Step 1: Write StreamHeader
writer.WriteStreamHeader();
Console.WriteLine("[1/4] StreamHeader written");

// Step 2: Write SetStoreInfo
// IMPORTANT: RRD stream uses log_msg::Msg semantics, NOT the LogMsg oneof wrapper.
// We serialize SetStoreInfo directly (not wrapped in LogMsg).

var recordingIdStr = Guid.NewGuid().ToString();
var storeId = new RerunCommon.StoreId
{
    Kind = RerunCommon.StoreKind.Recording,
    RecordingId = recordingIdStr,
    ApplicationId = new RerunCommon.ApplicationId { Id = "unity2rerun_phase1" }
};

var setStoreInfo = new RerunLogMsg.SetStoreInfo
{
    RowId = new RerunCommon.Tuid
    {
        TimeNs = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1_000_000,
        Inc = 1
    },
    Info = new RerunLogMsg.StoreInfo
    {
        StoreId = storeId,
        StoreVersion = new RerunLogMsg.StoreVersion { CrateVersionBits = 0x00170000 },
        StoreSource = new RerunLogMsg.StoreSource
        {
            Kind = RerunLogMsg.StoreSourceKind.Other,
            Extra = new RerunLogMsg.StoreSourceExtra
            {
                Payload = ByteString.CopyFromUtf8("unity2rerun")
            }
        }
    }
};

writer.WriteMessage(RrdConstants.MsgKindSetStoreInfo, setStoreInfo);
Console.WriteLine("[2/4] SetStoreInfo written");

// Step 3: Build Arrow RecordBatch for TextLog and serialize to IPC
var arrowPayload = BuildTextLogArrowPayload();
Console.WriteLine($"[3/4] Arrow IPC payload built ({arrowPayload.Length} bytes)");

// Step 4: Write ArrowMsg (directly, not wrapped in LogMsg)
var arrowMsg = new RerunLogMsg.ArrowMsg
{
    StoreId = storeId,
    ChunkId = new RerunCommon.Tuid { TimeNs = 2, Inc = 2 },
    Compression = RerunCommon.Compression.None,
    UncompressedSize = (ulong)arrowPayload.Length,
    Encoding = RerunLogMsg.Encoding.ArrowIpc,
    Payload = ByteString.CopyFrom(arrowPayload)
};

writer.WriteMessage(RrdConstants.MsgKindArrowMsg, arrowMsg);
Console.WriteLine("[4/4] ArrowMsg written");

// Writer is disposed by using, which calls FinishNoFooter
Console.WriteLine($"Done! Output: {Path.GetFullPath(OutFile)}");
Console.WriteLine($"File size: {new FileInfo(OutFile).Length} bytes");

static byte[] BuildTextLogArrowPayload()
{
    var schema = BuildTextLogSchema();

    // Column 0: row_id - FixedSizeBinary(16)
    var rowIdBytes = GenerateTuidBytes(1, 1);
    var rowIdArray = BuildFixedSizeBinaryArray(16, new[] { rowIdBytes });

    // Column 1: log_tick - Int64
    var tickBuilder = new Int64Array.Builder();
    tickBuilder.Append(0L);
    var tickArray = tickBuilder.Build(default);

    // Column 2: text - String (Utf8)
    var textBuilder = new StringArray.Builder();
    textBuilder.Append("hello from Unity2Rerun phase1", System.Text.Encoding.UTF8);
    var textArray = textBuilder.Build(default);

    // Column 3: level - String (Utf8)
    var levelBuilder = new StringArray.Builder();
    levelBuilder.Append("INFO", System.Text.Encoding.UTF8);
    var levelArray = levelBuilder.Build(default);

    var recordBatch = new RecordBatch(schema, new IArrowArray[]
    {
        rowIdArray, tickArray, textArray, levelArray
    }, 1);

    // Serialize to Arrow IPC stream format
    using var ms = new MemoryStream();
    using (var ipcWriter = new ArrowStreamWriter(ms, schema, leaveOpen: true))
    {
        ipcWriter.WriteRecordBatch(recordBatch);
    }

    return ms.ToArray();
}

static ArrowSchema BuildTextLogSchema()
{
    // Column 1: Row ID - must have rerun:kind=control and Arrow extension metadata
    var rowIdMetadata = new Dictionary<string, string>
    {
        { "rerun:kind", "control" },
        { "ARROW:extension:name", "rerun.datatypes.TUID" },
        { "ARROW:extension:metadata", @"{""namespace"":""row""}" }
    };
    var rowIdField = new Field("row_id",
        new Apache.Arrow.Types.FixedSizeBinaryType(16), false, rowIdMetadata);

    // Column 2: Timeline index
    var tickMetadata = new Dictionary<string, string>
    {
        { "rerun:kind", "index" },
        { "rerun:index_name", "log_tick" }
    };
    var tickField = new Field("log_tick",
        Apache.Arrow.Types.Int64Type.Default, false,
        tickMetadata);

    // Column 3: Component - Text
    var textMetadata = new Dictionary<string, string>
    {
        { "rerun:kind", "data" },
        { "rerun:component", "TextLog:text" },
        { "rerun:archetype", "rerun.archetypes.TextLog" },
        { "rerun:component_type", "rerun.components.Text" }
    };
    var textField = new Field("TextLog:text",
        Apache.Arrow.Types.StringType.Default, true,
        textMetadata);

    // Column 4: Component - Level
    var levelMetadata = new Dictionary<string, string>
    {
        { "rerun:kind", "data" },
        { "rerun:component", "TextLog:level" },
        { "rerun:archetype", "rerun.archetypes.TextLog" },
        { "rerun:component_type", "rerun.components.TextLogLevel" }
    };
    var levelField = new Field("TextLog:level",
        Apache.Arrow.Types.StringType.Default, true,
        levelMetadata);

    // Record batch metadata
    var batchMetadata = new Dictionary<string, string>
    {
        { "sorbet:version", "0.1.3" },
        { "rerun:id", "16A36600D1590000017E00005EA2E000" },
        { "rerun:entity_path", "logs/unity" }
    };

    return new ArrowSchema(
        new Field[] { rowIdField, tickField, textField, levelField },
        batchMetadata);
}

static FixedSizeBinaryArray BuildFixedSizeBinaryArray(int byteWidth, byte[][] values)
{
    var type = new Apache.Arrow.Types.FixedSizeBinaryType(byteWidth);

    int nullCount = 0;
    var validityBuffer = ArrowBuffer.Empty;

    var valuesBytes = new byte[values.Sum(v => v.Length)];
    int offset = 0;
    foreach (var v in values)
    {
        System.Array.Copy(v, 0, valuesBytes, offset, v.Length);
        offset += v.Length;
    }
    var valuesBuffer = new ArrowBuffer(new ReadOnlyMemory<byte>(valuesBytes));

    var arrayData = new ArrayData(type, values.Length, nullCount, 0,
        new[] { validityBuffer, valuesBuffer }, null);

    return new FixedSizeBinaryArray(arrayData);
}

static byte[] GenerateTuidBytes(ulong timeNs, ulong inc)
{
    // Tuid uses BIG-ENDIAN encoding for both fields
    // [time_nanos: 8bytes BE][inc: 8bytes BE] = 16 bytes
    var bytes = new byte[16];
    System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(0, 8), timeNs);
    System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(bytes.AsSpan(8, 8), inc);
    return bytes;
}

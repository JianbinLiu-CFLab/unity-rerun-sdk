# RRD Compression

Unity2Rerun can optionally compress `.rrd` file Arrow payloads with Rerun-compatible raw LZ4 block compression.

Unity2Rerun `.rrd` recording supports `None` and `Lz4` only. Unity2Foxglove's `Zstd` support is MCAP chunk compression and is not a Unity2Rerun RRD ArrowMsg mode.

## Scope

The compression setting affects file recording only. Live gRPC output remains uncompressed so existing Viewer streaming behavior and diagnostics stay unchanged.

`SetStoreInfo` messages are not compressed. Only Arrow message payloads written into the `.rrd` stream use the selected recording compression.

## Inspector

On `RerunManager`, open the **RRD Output** section and set **Recording Compression**:

| Value | Behavior |
| --- | --- |
| None | Writes raw Arrow IPC payloads. This is the default. |
| Lz4 | Writes raw LZ4 block-compressed Arrow IPC payloads in `.rrd` files. |

If you change the value while recording is already active, the new value applies to the next recording session.

## Script

```csharp
var manager = GetComponent<RerunManager>();
manager.RecordingCompression = RerunRecordingCompression.Lz4;
manager.StartRecording();
```

## Validation

Generate comparable None and LZ4 smoke recordings, then verify both files with the Rerun CLI:

```powershell
dotnet run --project Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj -- --write-phase14-compression-comparison build/RRD/phase14_compression
rerun rrd verify build/RRD/phase14_compression_none.rrd
rerun rrd verify build/RRD/phase14_compression_lz4.rrd
```

The comparison command prints an RRD inspection summary for both files. The important release evidence is the ArrowMsg compression field:

- the None recording should report `CompressionNone` equal to `ArrowMsg`;
- the LZ4 recording should report `CompressionLz4` equal to `ArrowMsg`;
- `CompressionOther` must be `0` for both recordings.

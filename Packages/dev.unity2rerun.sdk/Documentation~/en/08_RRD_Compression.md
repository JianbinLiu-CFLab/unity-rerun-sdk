# RRD Compression

Unity2Rerun can optionally compress `.rrd` file Arrow payloads with Rerun-compatible raw LZ4 block compression.

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

Generate a smoke recording and verify it with the Rerun CLI:

```powershell
dotnet run --project Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj -- --write-phase13-lz4-rrd build/RRD/phase13_lz4_smoke.rrd
rerun rrd verify build/RRD/phase13_lz4_smoke.rrd
rerun rrd stats build/RRD/phase13_lz4_smoke.rrd
```

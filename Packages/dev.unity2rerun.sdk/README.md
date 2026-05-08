# Unity2Rerun SDK

Unity native SDK for [Rerun](https://rerun.io) — log Unity runtime data to `.rrd` files and Rerun Viewer without external bridge processes.

## Status

Phase 2 — UPM package skeleton with basic TextLog `.rrd` output.

## Quick Start

```csharp
var go = new GameObject("Rerun");
var mgr = go.AddComponent<RerunManager>();
mgr.StartRecording();
mgr.LogText("logs/unity", "Hello from Unity!");
mgr.StopRecording();
```

## Package Structure

```
Runtime/
  Core/         IRerunBackend, RerunRuntime, RerunEntityPath, RerunTimeline
  Encoding/     ManagedRerunEncoder, RerunProtobufEncoding, RerunArrowIpcWriter
  IO/Rrd/       RrdWriter — low-level RRD binary framing
  Unity/        RerunManager — MonoBehaviour entry point
  Plugins/      Apache.Arrow.dll, Google.Protobuf.dll
```

## Supported Platforms

- Windows (Editor + Standalone)
- IL2CPP planned for Phase 3+
- WebGL not supported (requires gRPC)

## License

Apache-2.0

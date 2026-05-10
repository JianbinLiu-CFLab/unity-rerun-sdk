# Unity2Rerun SDK

Unity native SDK for [Rerun](https://rerun.io): log Unity runtime data to `.rrd` files and Rerun Viewer without external bridge processes.

## Status

Phase 7 - `.rrd` and live output, Publisher components, IL2CPP build support, and `[RerunLog]` source generation.

## Quick Start

```csharp
var go = new GameObject("Rerun");
var mgr = go.AddComponent<RerunManager>();
mgr.StartRecording();
mgr.LogText("logs/unity", "Hello from Unity!");
mgr.StopRecording();
```

Attribute-driven publishing:

```csharp
[RerunTransform("world/player", RateHz = 30f)]
public partial class PlayerDebug : MonoBehaviour
{
    [RerunLog("logs/player", RateHz = 1f)]
    private string _status = "ready";

    [RerunScalar("metrics/player_fps", RateHz = 10f)]
    private float _fps;
}
```

## Package Structure

```
Runtime/
  Core/         IRerunBackend, RerunRuntime, RerunEntityPath, RerunTimeline
  Encoding/     ManagedRerunEncoder, RerunProtobufEncoding, RerunArrowIpcEncoder
  IO/Rrd/       RrdWriter - low-level RRD binary framing
  Transport/    gRPC live transport and backend fan-out
  Unity/        RerunManager, Publishers, RerunLog attributes
  Plugins/      Apache.Arrow.dll, Google.Protobuf.dll, gRPC dependencies
Editor/
  SourceGenerators/  RerunLog Roslyn analyzer layout
  Shared/            Shared source emitter for Editor and Player fallback
```

## Supported Platforms

- Windows Editor
- Windows Standalone IL2CPP Player
- WebGL is not supported because live transport requires gRPC.

## License

Apache-2.0
## Citation / Research Positioning

If you use Unity2Rerun in research, please cite the repository-level [`CITATION.cff`](../../CITATION.cff). A concise research-positioning note is available in [`PAPER.md`](../../PAPER.md).

# Unity2Rerun SDK

Unity native SDK for [Rerun](https://rerun.io): log Unity runtime data to `.rrd` files and Rerun Viewer without external bridge processes.

## Version requirements

- Unity 6000.0 LTSC or later (developed on 6000.3.14f1 LTSC; compatible with 6000.0.74f1 LTSC)
- Editor + Standalone Player. Windows is the verified target for the current SDK work; macOS/Linux are intended targets but not yet verified.
- Rerun Viewer / CLI 0.31.4+
- Live transport dependency: Cysharp `YetAnotherHttpHandler` 1.11.5 with its native dependency package. FileOnly `.rrd` output does not require it.

## Status

Phase 13 development - `.rrd` and live output, official-compatible RRD footer/manifests, optional LZ4 compression for `.rrd` file recording, Publisher components, IL2CPP build support, `[RerunLog]` source generation, EncodedImage, 3D boxes, trajectories, Points3D, Pinhole camera metadata, laser-scan/point-cloud publishers, local sidecar controls, and live transport health diagnostics.

## Quick install

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "dev.unity2rerun.sdk": "file:../../Packages/dev.unity2rerun.sdk"
  }
}
```

For live gRPC output, also install Cysharp `YetAnotherHttpHandler` 1.11.5 as described in `Documentation~/en/00_Prerequisites.md`.

## Minimal usage

```csharp
var go = new GameObject("Rerun");
var mgr = go.AddComponent<RerunManager>();
mgr.StartRecording();
mgr.LogText("logs/unity", "Hello from Unity!");
mgr.StopRecording();
```

Interactive 3D/image publishing:

```csharp
mgr.LogEncodedImage("camera/main", jpegBytes, "image/jpeg");
mgr.LogPinhole("camera/main", RerunPinhole.FromVerticalFov(640, 480, 60f));
mgr.LogTransform("world/cube", cubeTransform);
mgr.LogBox3D("world/cube", Vector3.zero, cubeTransform.lossyScale * 0.5f, Quaternion.identity, Color.green);
mgr.LogLineStrips3D("world/cube_trajectory", trajectoryPoints, Color.yellow);
mgr.LogPoints3D("world/points", pointPositions, Color.cyan, radius: 0.03f);
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

## Features

- Rerun `.rrd` file recording with footer/manifests verified by `rerun rrd verify`
- Optional LZ4 compression for `.rrd` file Arrow payloads through `RerunManager.RecordingCompression`
- Live Rerun Viewer output through gRPC
- Read-only live transport health snapshot in `RerunManager` and the Inspector
- TextLog, Scalar, Transform3D, EncodedImage, Pinhole, Boxes3D, LineStrips3D, and Points3D publishing
- Sensor-oriented publisher components for camera pinhole metadata, point clouds, and planar laser scans
- Inspector-driven publishers and samples
- `[RerunLog]` attribute-driven source generation, not runtime reflection
- Local loopback sidecar control sample with parameter-like state and action buttons for Unity-driven interactive demos
- IL2CPP standalone build support
- Unity-to-Rerun coordinate conversion

## Package Structure

```
Runtime/
  Core/         IRerunBackend, RerunRuntime, RerunEntityPath, RerunTimeline
  Encoding/     ManagedRerunEncoder, RerunProtobufEncoding, RerunArrowIpcEncoder
  IO/Rrd/       RrdWriter - low-level RRD binary framing
  Transport/    gRPC live transport and backend fan-out
  Components/   RerunManager, publishers, RerunLog attributes, loopback control
  Plugins/      Apache.Arrow.dll, Google.Protobuf.dll, gRPC and compression dependencies
Editor/
  SourceGenerators/  RerunLog Roslyn analyzer layout
  Shared/            Shared source emitter for Editor and Player fallback
```

## Supported Platforms

- Windows Editor
- Windows Standalone IL2CPP Player
- Phase 8 sidecar control is Windows Editor focused; Player sidecar support remains a later validation item.
- WebGL is not supported because live transport requires gRPC.

## Full documentation

See [Documentation~/README.md](Documentation~/README.md).

## License

Apache-2.0
## Citation / Research Positioning

If you use Unity2Rerun in research, please cite the repository-level [`CITATION.cff`](../../CITATION.cff). A concise research-positioning note is available in [`PAPER.md`](../../PAPER.md).

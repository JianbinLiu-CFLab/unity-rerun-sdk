# Unity2Rerun SDK

Unity-native SDK for [Rerun](https://rerun.io): record Unity runtime data to `.rrd` files, stream live output to Rerun Viewer, and instrument scenes through code, Inspector components, or generated field logging.

## Version Requirements

- Unity 6000.0 LTSC or later (developed on 6000.3.14f1 LTSC; compatible with 6000.0.74f1 LTSC)
- Editor + Standalone Player. Windows is the verified target for the current SDK work; macOS/Linux are intended targets but not yet verified.
- Rerun Viewer / CLI 0.31.4+
- Optional live transport dependency: Cysharp `YetAnotherHttpHandler` 1.11.5 with its native dependency package. FileOnly `.rrd` output does not require it.

## Quick Install

Add this package to `Packages/manifest.json` when using the repository layout:

```json
{
  "dependencies": {
    "dev.unity2rerun.sdk": "file:../../Packages/dev.unity2rerun.sdk"
  }
}
```

Or install from the repository with a Git URL:

```text
https://github.com/JianbinLiu-CFLab/unity-rerun-sdk.git?path=/Packages/dev.unity2rerun.sdk
```

For live gRPC output, also install Cysharp `YetAnotherHttpHandler` 1.11.5 as described in `Documentation~/en/00_Prerequisites.md`.

## Minimal Usage

### File Recording

```csharp
using Unity.RerunSDK.Unity;
using UnityEngine;

public class MinimalRerunRecorder : MonoBehaviour
{
    private RerunManager _rerun;

    private void Awake()
    {
        _rerun = gameObject.AddComponent<RerunManager>();
        _rerun.StartRecording();
    }

    private void Update()
    {
        if (!_rerun.IsRecording)
            return;

        _rerun.SetTimeSequence("frame", Time.frameCount);
        _rerun.LogText("logs/unity", "Hello from Unity");
        _rerun.LogScalar("metrics/fps", 1.0 / Time.deltaTime);
        _rerun.LogTransform("world/object", transform);
    }

    private void OnDestroy()
    {
        _rerun?.StopRecording();
    }
}
```

After Play Mode stops, open the generated file:

```powershell
rerun path/to/recording.rrd
```

### 3D, Sensor, And Image Publishing

```csharp
mgr.LogEncodedImage("camera/main", jpegBytes, "image/jpeg");
mgr.LogPinhole("camera/main", RerunPinhole.FromVerticalFov(640, 480, 60f));
mgr.LogTransform("world/cube", cubeTransform);
mgr.LogBox3D("world/cube", Vector3.zero, cubeTransform.lossyScale * 0.5f, Quaternion.identity, Color.green);
mgr.LogLineStrips3D("world/cube_trajectory", trajectoryPoints, Color.yellow);
mgr.LogPoints3D("world/points", pointPositions, Color.cyan, radius: 0.03f);
```

### Attribute-Driven Publishing

```csharp
using Unity.RerunSDK.Unity;
using UnityEngine;

public partial class PlayerDebug : MonoBehaviour
{
    [RerunLog("logs/player", RateHz = 1f)]
    private string _status = "ready";

    [RerunScalar("metrics/player_fps", RateHz = 10f)]
    private float _fps;

    [RerunTransform("world/player", RateHz = 30f)]
    private Transform _playerTransform;
}
```

## Output Modes

| Mode | File output | Live output | Use when |
|------|-------------|-------------|----------|
| FileOnly | Yes | No | You want reliable `.rrd` artifacts and the fewest dependencies. |
| LiveOnly | No | Yes | You only need a running Viewer session. |
| FileAndLive | Yes | Yes | You want live visualization while keeping the `.rrd` file as the reliable artifact. |

## Features

- Rerun `.rrd` file recording with footer/manifests verified by `rerun rrd verify`
- Optional LZ4 compression for file-recorded Arrow payloads through `RerunManager.RecordingCompression`
- Live Rerun Viewer output through gRPC
- Read-only live transport health snapshot in `RerunManager` and the Inspector
- TextLog, Scalar, Transform3D, EncodedImage, Pinhole, Boxes3D, LineStrips3D, and Points3D publishing
- Sensor-oriented publisher components for camera images, camera pinhole metadata, point clouds, and planar laser scans
- Inspector-driven publishers and package samples
- `[RerunLog]` attribute-driven source generation, not runtime reflection
- Local loopback sidecar control sample with parameter-like state and action buttons for Unity-driven interactive demos
- IL2CPP standalone build support
- Unity-to-Rerun coordinate conversion

## Package Structure

```text
Runtime/
  Core/         Backend contracts, runtime state, entity paths, timelines, compression modes
  Encoding/     ManagedRerunEncoder, RerunArrowIpcEncoder, protobuf wrapping, payload compression
  IO/Rrd/       RrdWriter and RRD file backend
  Transport/    Backend fan-out and gRPC live transport
  Components/   RerunManager, publishers, RerunLog attributes, loopback control
  Utilities/    Shared runtime helpers
  Plugins/      Apache.Arrow.dll, Google.Protobuf.dll, gRPC and compression dependencies
Editor/
  SourceGenerators/  RerunLog Roslyn analyzer layout
  Shared/            Shared source emitter for Editor and Player fallback
```

## Samples

| Sample | Purpose |
|--------|---------|
| Basic Rrd Recording | Minimal TextLog `.rrd` recording through `RerunManager`. |
| Publisher Components | Inspector-driven Transform, Scalar, and TextLog publishing. |
| Live Viewer | FileAndLive output with Rerun Viewer transport. |
| Generated RerunLog | Attribute-driven TextLog, Scalar, and Transform3D publishing. |
| Interactive 3D Control | EncodedImage, Pinhole, Boxes3D, Points3D, laser scan, trajectory, metrics, logs, and loopback sidecar control. |

## Type Coverage

Unity2Rerun implements a curated Rerun runtime subset instead of mirroring every official schema at once. The current encoder surface covers 9 runtime archetypes and 16 emitted components. The public coverage matrix lives at `../../../docs/releases/RERUN_TYPE_COVERAGE_MATRIX.md` and can be checked from the repository root with:

```powershell
python Scripts/release/check_rerun_type_coverage.py
```

## Supported Platforms

- Windows Editor
- Windows Standalone IL2CPP Player
- macOS/Linux are intended targets but not yet verified for this package line
- WebGL is not supported because the current file/live stack depends on APIs unavailable in WebGL builds

## Full Documentation

See [Documentation~/README.md](Documentation~/README.md).

## Citation / Research Positioning

If you use Unity2Rerun in research, please cite the repository-level [`CITATION.cff`](../../CITATION.cff). A concise research-positioning note is available in [`PAPER.md`](../../PAPER.md).

## License

Apache-2.0

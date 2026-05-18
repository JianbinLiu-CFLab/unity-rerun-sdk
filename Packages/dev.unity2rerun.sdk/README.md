# Unity2Rerun SDK

Unity-native SDK for [Rerun](https://rerun.io): record Unity runtime data to `.rrd` files, stream live output to Rerun Viewer, and instrument scenes through code, Inspector components, or generated field logging.

## Version Requirements

- Unity 6000.0 LTSC or later (developed on 6000.3.14f1 LTSC; compatible with 6000.0.74f1 LTSC)
- Editor + Standalone Player. Windows is the verified target for the current SDK work; macOS/Linux are intended targets but not yet verified.
- Rerun Viewer / CLI 0.31.4+
- Optional live transport dependency: Cysharp `YetAnotherHttpHandler` 1.11.5 with its native dependency package. FileOnly `.rrd` output does not require it.

## Installation

Install the package through Unity Package Manager by selecting `Packages/dev.unity2rerun.sdk/package.json`, or by using the repository Git URL with the package path suffix `?path=/Packages/dev.unity2rerun.sdk`.

For live gRPC output, install Cysharp `YetAnotherHttpHandler` 1.11.5 as described in [Prerequisites](Documentation~/en/00_Prerequisites.md). FileOnly `.rrd` output does not require that dependency.

## Usage Paths

| Goal | Recommended path |
|------|------------------|
| Record a first `.rrd` file | Follow the [installation and first recording guide](Documentation~/en/01_Installation_and_Quick_Start.md). |
| Avoid writing code | Use [Publisher components](Documentation~/en/02_Publisher_Components.md). |
| Stream to a running Viewer | Read [Output modes and live troubleshooting](Documentation~/en/03_Output_Modes_and_Live_Troubleshooting.md). |
| Add image, camera, point-cloud, scan, or 3D geometry logging | Use [Interactive 3D control](Documentation~/en/07_Interactive_3D_Control.md) and the matching samples. |
| Add attribute-driven field logging | Use [RerunLog source generator](Documentation~/en/06_RerunLog_Source_Generator.md). |
| Build a standalone Player | Use [IL2CPP build guide](Documentation~/en/05_IL2CPP_Build_Guide.md). |

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

| Path | Purpose |
|------|---------|
| `Runtime/Core` | Backend contracts, runtime state, entity paths, timelines, and compression modes. |
| `Runtime/Encoding` | Managed Rerun encoding, Arrow IPC chunks, protobuf wrapping, and payload compression. |
| `Runtime/IO/Rrd` | RRD writer and file backend. |
| `Runtime/Transport` | Backend fan-out and gRPC live transport. |
| `Runtime/Components` | `RerunManager`, publishers, attributes, and loopback control components. |
| `Runtime/Utilities` | Shared runtime helpers. |
| `Runtime/Plugins` | Apache Arrow, Google Protobuf, gRPC, and compression dependencies. |
| `Editor/SourceGenerators` | RerunLog Roslyn analyzer layout. |
| `Editor/Shared` | Shared source emitter for Editor and Player fallback. |

## Samples

| Sample | Purpose |
|--------|---------|
| Basic Rrd Recording | Minimal TextLog `.rrd` recording through `RerunManager`. |
| Publisher Components | Inspector-driven Transform, Scalar, and TextLog publishing. |
| Live Viewer | FileAndLive output with Rerun Viewer transport. |
| Generated RerunLog | Attribute-driven TextLog, Scalar, and Transform3D publishing. |
| Interactive 3D Control | EncodedImage, Pinhole, Boxes3D, Points3D, laser scan, trajectory, metrics, logs, and loopback sidecar control. |

## Type Coverage

Unity2Rerun implements a curated Rerun runtime subset instead of mirroring every official schema at once. The current encoder surface covers 9 runtime archetypes and 16 emitted components. The public coverage matrix lives at `../../../docs/releases/RERUN_TYPE_COVERAGE_MATRIX.md`.

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

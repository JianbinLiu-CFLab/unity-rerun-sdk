# Unity2Rerun

[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![Unity](https://img.shields.io/badge/Unity-6000.0%2B-black?logo=unity)](https://unity.com/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![Release](https://img.shields.io/badge/release-v0.4.0-green)](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/releases)
[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.20247512.svg)](https://doi.org/10.5281/zenodo.20247512)
[![Tests](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/actions/workflows/dotnet-tests.yml/badge.svg)](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/actions/workflows/dotnet-tests.yml)
[![Docs Check](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/actions/workflows/docs-check.yml/badge.svg)](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/actions/workflows/docs-check.yml)

> **Positioning**: Unity2Rerun is a Unity-focused SDK for recording and streaming Unity runtime data into [Rerun](https://rerun.io). It aims for the Rerun-native workflows that matter most inside Unity: `.rrd` recordings, live Viewer output, Inspector-driven publishers, generated field logging, sensor-style visualization, and IL2CPP-oriented validation. It is not an official Rerun project and does not target full multi-language Rerun SDK parity.

Unity2Rerun runs inside Unity. It writes Rerun `.rrd` files directly, can stream live output to Rerun Viewer over gRPC, and avoids external bridge processes for the normal file-recording workflow.

## Purpose

Unity2Rerun turns Unity Editor and standalone Player sessions into Rerun recordings. It addresses four core needs.

### RRD Recording

- Record Unity runtime data to `.rrd` files that can be opened by Rerun Viewer.
- Include RRD footer/manifests for `rerun rrd verify` and release checks.
- Optionally compress file-recorded Arrow payloads with LZ4 while keeping live gRPC payloads uncompressed.

### Live Visualization

- Stream data to a running Rerun Viewer over gRPC.
- Use `FileAndLive` mode when a local file should remain the reliable source of truth.
- Keep live transport failures isolated from `.rrd` output.

### Runtime Debugging

- Publish TextLog, Scalar, Transform3D, EncodedImage, Pinhole, Boxes3D, LineStrips3D, and Points3D data from Unity.
- Use Inspector-driven publisher components for no-code scene instrumentation.
- Use the local loopback sidecar sample for Unity-driven interactive demos when Rerun Viewer does not provide arbitrary parameter/service panels.

### Generated Logging

- Annotate MonoBehaviour fields with `[RerunLog]`, `[RerunScalar]`, or `[RerunTransform]`.
- Generate strongly typed publishing code instead of relying on runtime reflection.
- Keep generated logging compatible with Editor and IL2CPP-oriented workflows.

## Who This Is For

Unity2Rerun is for Unity developers who want runtime state visible outside the Game view without building custom debug UI or maintaining a separate bridge process.

It is especially useful for:

- Robotics, simulation, digital-twin, and visualization teams already using Rerun.
- Unity developers who need plots, logs, images, camera metadata, point clouds, scans, and 3D overlays during Play Mode.
- Researchers who need reproducible `.rrd` artifacts and release-friendly provenance.
- Tool builders who want Unity-native components plus a small managed API surface.

## The Problem

Common approaches for exporting Unity runtime data to external visualization tools tend to add friction:

- External bridge processes introduce launch ordering, platform, and deployment complexity.
- Ad-hoc socket scripts lack schema discipline, replayable artifacts, and release validation.
- Runtime reflection-based telemetry can be fragile under IL2CPP and hard to audit.
- Custom in-Editor debug windows are useful locally but do not produce shareable recordings.

## The Solution

Unity2Rerun keeps the workflow direct:

1. Add a `RerunManager` to the scene.
2. Log through manager APIs, publisher components, or generated `[RerunLog]` sources.
3. Save `.rrd` files, stream to Rerun Viewer, or do both.
4. Verify recordings with the Rerun CLI when preparing releases or bug reports.

## Installation

SDK core targets Unity 6000.0 LTSC or later. The repository demo project and current validation workflow are developed on Unity 6000.3.14f1 LTSC and verified on Windows.

### Use as Unity Package

For adding the SDK to your own Unity project:

1. Clone this repository.
2. Unity menu: `Window > Package Manager > + > Add package from disk...`
3. Select `Packages/dev.unity2rerun.sdk/package.json`.

Or install via Git URL:

```text
https://github.com/JianbinLiu-CFLab/unity-rerun-sdk.git?path=/Packages/dev.unity2rerun.sdk
```

### Open the Demo Project

For quickly exploring the package in a ready-made Unity project:

1. Clone this repository.
2. Unity Hub > Open > select the `Unity2Rerun` directory.
3. Wait for Unity Package Manager to resolve the local SDK package from `../../Packages/dev.unity2rerun.sdk`.
4. Open the sample scene and press Play.

## Quick Start

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

After Play Mode stops, open the generated recording:

```powershell
rerun path/to/recording.rrd
```

### Sensor And 3D Publishing

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

## Project Layout

- Use `Packages/dev.unity2rerun.sdk` when installing the SDK into your own Unity project.
- Use `Unity2Rerun` when opening the ready-made demo project.
- Use `Packages/dev.unity2rerun.sdk/Samples~/BasicRrdRecording` for the smallest file-recording sample.
- Use `Packages/dev.unity2rerun.sdk/Samples~/PublisherComponents` for Inspector-driven TextLog, Scalar, and Transform publishing.
- Use `Packages/dev.unity2rerun.sdk/Samples~/GeneratedRerunLog` for generated attribute logging.
- Use `Packages/dev.unity2rerun.sdk/Samples~/Interactive3DControl` for image, pinhole, 3D geometry, point-cloud, laser-scan, metrics, logs, and loopback control smoke testing.

## Documentation

Start with the document that matches what you are trying to do:

| Goal | Read this |
|------|-----------|
| Install the SDK into your own Unity project | [Package documentation](Packages/dev.unity2rerun.sdk/Documentation~/README.md) |
| Understand prerequisites and live transport dependencies | [Prerequisites](Packages/dev.unity2rerun.sdk/Documentation~/en/00_Prerequisites.md) |
| Publish your first `.rrd` recording | [Installation and quick start](Packages/dev.unity2rerun.sdk/Documentation~/en/01_Installation_and_Quick_Start.md) |
| Use Inspector-driven publishers | [Publisher components](Packages/dev.unity2rerun.sdk/Documentation~/en/02_Publisher_Components.md) |
| Choose FileOnly, LiveOnly, or FileAndLive output | [Output modes and live troubleshooting](Packages/dev.unity2rerun.sdk/Documentation~/en/03_Output_Modes_and_Live_Troubleshooting.md) |
| Understand runtime architecture | [Architecture](Packages/dev.unity2rerun.sdk/Documentation~/en/04_Architecture.md) |
| Build and validate IL2CPP Players | [IL2CPP build guide](Packages/dev.unity2rerun.sdk/Documentation~/en/05_IL2CPP_Build_Guide.md) |
| Use generated field logging | [RerunLog source generator](Packages/dev.unity2rerun.sdk/Documentation~/en/06_RerunLog_Source_Generator.md) |
| Verify interactive 3D, image, scan, and sidecar workflows | [Interactive 3D control](Packages/dev.unity2rerun.sdk/Documentation~/en/07_Interactive_3D_Control.md) |
| Understand `.rrd` compression evidence | [RRD compression](Packages/dev.unity2rerun.sdk/Documentation~/en/08_RRD_Compression.md) |
| Check official Rerun type coverage | [Rerun type coverage matrix](docs/releases/RERUN_TYPE_COVERAGE_MATRIX.md) |

Release and provenance documents:

- [Changelog](CHANGELOG.md)
- [v0.4.0 release notes](docs/releases/RELEASE_NOTES_v0.4.0.md)
- [Zenodo release checklist](docs/releases/ZENODO_RELEASE_CHECKLIST.md)
- [AI training notice](AI_NOTICE.md)

## Validation Commands

These checks are mainly for contributors and maintainers before changing SDK internals:

```powershell
dotnet test Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj --no-restore
python Scripts/release/validate_package.py
python Scripts/release/check_rerun_type_coverage.py
```

For RRD output evidence, also verify generated recordings with the Rerun CLI:

```powershell
rerun rrd verify path/to/recording.rrd
rerun rrd stats path/to/recording.rrd
```

## Capabilities And Limitations

### Supported

- Rerun `.rrd` file recording with footer/manifests verified by `rerun rrd verify`.
- Optional LZ4 compression for file-recorded Arrow payloads.
- Live Rerun Viewer output through gRPC.
- File-first `FileAndLive` mode where live failures do not break `.rrd` output.
- TextLog, Scalar, Transform3D, EncodedImage, Pinhole, Boxes3D, LineStrips3D, and Points3D publishing.
- Sensor-oriented publisher components for camera metadata, camera images, point clouds, and planar laser scans.
- `[RerunLog]` attribute-driven source generation without runtime reflection for generated fields.
- Local loopback sidecar control sample for Unity-driven interactive demos.
- Windows Editor and Windows Standalone IL2CPP validation.

### Not Supported

- Full official Rerun SDK schema parity. Coverage is tracked in the type coverage matrix and expanded by focused Unity workflows.
- MCAP recording. Unity2Rerun records Rerun-native `.rrd` files.
- Live payload compression. LZ4 applies to file-recorded Arrow payloads only.
- Production remote control security for the loopback sidecar. It is a local development sample, not an authenticated remote-control server.
- Deterministic Unity physics or input replay. Recordings are visualization and inspection artifacts, not full simulation replays.
- WebGL, because the current file/live stack depends on APIs that are not available in WebGL builds.

## Citation / Research Positioning

If you use Unity2Rerun in research, please cite the software metadata in [CITATION.cff](CITATION.cff). Use the Zenodo Concept DOI [10.5281/zenodo.20247512](https://doi.org/10.5281/zenodo.20247512) to cite the project across all versions, or the version-specific DOI [10.5281/zenodo.20247513](https://doi.org/10.5281/zenodo.20247513) for exact reproduction of v0.4.0. A concise research-positioning note is available in [PAPER.md](PAPER.md).

## License

This project is licensed under the [Apache License 2.0](LICENSE) so it can be used, modified, and integrated in research, commercial, and open-source Unity projects with a clear patent and attribution model.

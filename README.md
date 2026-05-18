# Unity2Rerun

[![License](https://img.shields.io/badge/license-Apache%202.0-blue.svg)](LICENSE)
[![Unity](https://img.shields.io/badge/Unity-6000.0%2B-black?logo=unity)](https://unity.com/)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![Release](https://img.shields.io/badge/release-v0.4.0-green)](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/releases)
[![DOI](https://zenodo.org/badge/DOI/10.5281/zenodo.20247512.svg)](https://doi.org/10.5281/zenodo.20247512)
[![Tests](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/actions/workflows/dotnet-tests.yml/badge.svg)](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/actions/workflows/dotnet-tests.yml)
[![Docs Check](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/actions/workflows/docs-check.yml/badge.svg)](https://github.com/JianbinLiu-CFLab/unity-rerun-sdk/actions/workflows/docs-check.yml)

> **Positioning**: Unity2Rerun is a Unity-focused SDK for recording and streaming Unity runtime data into [Rerun](https://rerun.io). It focuses on the Rerun-native workflows that matter most inside Unity: `.rrd` recordings, live Viewer output, Inspector-driven publishers, generated field logging, sensor-style visualization, and IL2CPP-oriented validation. It is not an official Rerun project and does not target full multi-language Rerun SDK parity.

Unity2Rerun runs inside Unity. It writes Rerun `.rrd` files directly, can stream live output to Rerun Viewer over gRPC, and avoids external bridge processes for the normal file-recording workflow.

## What It Does

Unity2Rerun turns Unity Editor and standalone Player sessions into Rerun recordings and live Viewer streams.

| Need | Unity2Rerun support |
|------|---------------------|
| Record runtime data | Writes `.rrd` files with footer/manifests for Rerun CLI verification. |
| Watch data live | Streams to Rerun Viewer over gRPC with file-first failure isolation. |
| Instrument scenes without custom UI | Provides Inspector publishers for logs, metrics, transforms, images, camera metadata, point clouds, scans, and 3D geometry. |
| Avoid runtime reflection for field logging | Generates publishing code from `[RerunLog]`, `[RerunScalar]`, and `[RerunTransform]`. |
| Validate release artifacts | Includes package hygiene, RRD compression, type coverage, and runtime test checks. |

## Who This Is For

- Robotics, simulation, digital-twin, and visualization teams already using Rerun.
- Unity developers who need plots, logs, images, camera metadata, point clouds, scans, and 3D overlays during Play Mode.
- Researchers who need reproducible `.rrd` artifacts and release-friendly provenance.
- Tool builders who want Unity-native components plus a small managed API surface.

## Why It Exists

Exporting Unity runtime data to external tools often turns into a bridge project of its own. External processes add launch ordering and deployment friction, ad-hoc socket scripts do not create durable artifacts, runtime reflection can be fragile under IL2CPP, and custom Editor windows do not travel well across bug reports or papers.

Unity2Rerun keeps the workflow direct: add a `RerunManager`, publish through manager APIs, components, or generated sources, then inspect the result in Rerun Viewer from a live stream or `.rrd` file.

## Installation

For adding the SDK to your own Unity project, use Unity Package Manager and select `Packages/dev.unity2rerun.sdk/package.json` from this repository. Git URL installation is also supported with the package path suffix `?path=/Packages/dev.unity2rerun.sdk`.

For exploring the ready-made project, open the `Unity2Rerun` directory in Unity Hub and let Unity resolve the local SDK package from `../../Packages/dev.unity2rerun.sdk`.

SDK core targets Unity 6000.0 LTSC or later. The repository demo project and current validation workflow are developed on Unity 6000.3.14f1 LTSC and verified on Windows.

## Project Layout

| Path | Purpose |
|------|---------|
| `Packages/dev.unity2rerun.sdk` | Reusable Unity package for installation into other projects. |
| `Unity2Rerun` | Ready-made Unity project for local smoke testing and demo work. |
| `Packages/dev.unity2rerun.sdk/Samples~/BasicRrdRecording` | Smallest file-recording sample. |
| `Packages/dev.unity2rerun.sdk/Samples~/PublisherComponents` | Inspector-driven TextLog, Scalar, and Transform publishing. |
| `Packages/dev.unity2rerun.sdk/Samples~/GeneratedRerunLog` | Generated attribute logging sample. |
| `Packages/dev.unity2rerun.sdk/Samples~/Interactive3DControl` | Image, pinhole, 3D geometry, point-cloud, laser-scan, metrics, logs, and loopback control sample. |

## Documentation

Start with the document that matches what you are trying to do:

| Goal | Read this |
|------|-----------|
| Install the SDK into your own Unity project | [Package documentation](Packages/dev.unity2rerun.sdk/Documentation~/README.md) |
| Understand prerequisites and live transport dependencies | [Prerequisites](Packages/dev.unity2rerun.sdk/Documentation~/en/00_Prerequisites.md) |
| Publish your first `.rrd` recording | [Installation and first recording guide](Packages/dev.unity2rerun.sdk/Documentation~/en/01_Installation_and_Quick_Start.md) |
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

## Validation

Maintainer checks cover runtime tests, package hygiene, public documentation hygiene, RRD evidence, and official Rerun type coverage. Command details live in the package documentation and release scripts so this README stays focused on orientation.

## Citation / Research Positioning

If you use Unity2Rerun in research, please cite the software metadata in [CITATION.cff](CITATION.cff). Use the Zenodo Concept DOI [10.5281/zenodo.20247512](https://doi.org/10.5281/zenodo.20247512) to cite the project across all versions, or the version-specific DOI [10.5281/zenodo.20247513](https://doi.org/10.5281/zenodo.20247513) for exact reproduction of v0.4.0. A concise research-positioning note is available in [PAPER.md](PAPER.md).

## License

This project is licensed under the [Apache License 2.0](LICENSE) so it can be used, modified, and integrated in research, commercial, and open-source Unity projects with a clear patent and attribution model.

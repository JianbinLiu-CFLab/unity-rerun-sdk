# Samples and Demo Project

## Purpose

Use this page to choose the right Unity2Rerun entry point before opening Unity or importing a package sample.

## Project Choices

| Entry point | Use when | Notes |
| --- | --- | --- |
| Your own Unity project | You want to add Unity2Rerun to an existing scene or application. | Install the package from `Packages/dev.unity2rerun.sdk/package.json`, then add `RerunManager` and the publishers you need. |
| Repository `Unity2Rerun` project | You want a ready-made project for local smoke testing. | Keep it inside the repository so its local package reference can resolve. |
| UPM samples | You want importable examples inside another Unity project. | Import samples from Package Manager after installing the package. |

Generated `.rrd` recordings should stay outside the package, normally under repository-level `build/RRD` or a project-specific output directory.

## Package Samples

| Sample | Demonstrates | Dependencies | Expected output | Choose it when |
| --- | --- | --- | --- | --- |
| Basic Rrd Recording | Minimal TextLog, Scalar, and Transform3D file recording. | Rerun CLI/Viewer. | `.rrd` file with `logs/unity`, `metrics/fps`, and `world/cube`. | You want the smallest file-output smoke test. |
| Publisher Components | Inspector-driven TextLog, Scalar, and Transform3D publishers. | Rerun CLI/Viewer. | `.rrd` file with `logs/unity`, `metrics/fps`, and `world/cube`. | You want no-code scene instrumentation. |
| Live Viewer | FileAndLive output to Rerun Viewer. | Rerun Viewer plus Cysharp `YetAnotherHttpHandler` for Unity HTTP/2 gRPC. | Live Viewer updates and an optional `.rrd` file. | You want live streaming behavior. |
| Generated RerunLog | Attribute-driven generated logging. | Source generator included in the package. | `logs/generated`, `metrics/generated_fps`, and `world/generated_cube`. | You want generated field logging without runtime reflection. |
| Interactive 3D Control | Image, pinhole, 3D geometry, point cloud, laser scan, metrics, logs, and loopback control. | Rerun CLI/Viewer. Live mode also needs the live dependency. | 3D cube, trajectory, image, plots, text logs, and optional sidecar control. | You want the broadest manual acceptance pass. |

## Suggested Reading Order

1. Start with `01_Installation_and_Quick_Start.md` for a first file recording.
2. Read `02_Publisher_Components.md` if you prefer Inspector workflows.
3. Read `03_Output_Modes_and_Live_Troubleshooting.md` before using live output.
4. Use each sample README for exact setup and acceptance steps.


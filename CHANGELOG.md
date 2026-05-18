# Changelog

All notable changes to Unity2Rerun are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

---

## Unreleased

### Added

- Optional LZ4 compression for Rerun `.rrd` file Arrow payloads through `RerunManager.RecordingCompression`.
- Phase 13 LZ4 smoke `.rrd` writer for repeatable Rerun CLI validation.

### Changed

- Live gRPC output remains explicitly uncompressed even when `.rrd` file recording uses LZ4.

## 0.4.0 - 2026-05-17

### Added

- Rerun `Pinhole` camera metadata encoding and `RerunManager.LogPinhole`.
- `RerunPinholeCameraPublisher` for Unity camera intrinsics/frustum metadata.
- `RerunPointCloudPublisher` for Transform-based or externally supplied point-cloud frames.
- `RerunLaserScanPublisher` for planar scan ranges mapped to `Points3D` plus optional `LineStrips3D`.
- Phase 11 smoke `.rrd` writer for repeatable Rerun CLI validation.
- Release metadata updates for Zenodo/GitHub archiving through `CITATION.cff` and release notes.

### Validation

- `dotnet test Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj --no-restore` passed 66/66 tests.
- `rerun rrd verify build/RRD/phase11_sensor_smoke.rrd` verified without error.
- Manual Rerun replay confirmed camera image, Pinhole camera entity, point cloud, laser scan points, and laser scan outline.

## 0.3.0 - 2026-05-10

### Added

- RRD footer and manifest finalization by default.
- Official `rerun rrd verify` compatibility for generated smoke recordings.
- `EncodedImage`, `Boxes3D`, and `LineStrips3D` archetype support.
- Interactive 3D Control sample with camera image, cube box, trajectory, metrics, logs, and loopback sidecar control.
- Read-only live transport health snapshot API and Inspector diagnostics.
- `[RerunLog]` attribute-driven source generation and IL2CPP build-time generated-file fallback.
- FileOnly, LiveOnly, and FileAndLive output modes.

### Changed

- `RrdRerunBackend.Flush()` now flushes the stream without finalizing it.
- `RrdRerunBackend.Shutdown()` writes the full RRD End message and StreamFooter.
- Test smoke writers now use the same backend finalization path as runtime recording.

### Fixed

- Removed the known `Missing RRD footer / no RRD manifests` limitation for newly generated files.
- Prevented live transport diagnostics from changing bounded-queue send semantics.

### Known Limitations

- Windows Editor and Windows Standalone IL2CPP Player are the verified targets.
- Sidecar control is Windows Editor focused; Player sidecar support remains a later validation item.
- Live gRPC requires project-level `YetAnotherHttpHandler`.

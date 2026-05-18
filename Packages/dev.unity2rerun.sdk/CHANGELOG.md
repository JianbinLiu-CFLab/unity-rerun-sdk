# Changelog

All notable changes to Unity2Rerun SDK are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

---

## Unreleased

### Added

- Optional LZ4 compression for `.rrd` file Arrow payloads through `RerunManager.RecordingCompression`.
- Phase 13 LZ4 smoke `.rrd` writer covering TextLog, Scalar, Transform3D, Points3D, and ViewCoordinates chunks.
- Bundled minimal K4os LZ4 runtime DLLs and third-party notice coverage for RRD compression.

### Changed

- Live gRPC output is encoded from uncompressed Arrow messages even when `.rrd` file recording uses LZ4.

## 0.4.0 - 2026-05-17

### Added

- `RerunPinhole` encoding and `RerunManager.LogPinhole` for Rerun `Pinhole` camera metadata.
- `RerunPinholeCameraPublisher`, `RerunPointCloudPublisher`, and `RerunLaserScanPublisher` sensor-oriented publisher components.
- Phase 11 smoke `.rrd` writer covering Pinhole, EncodedImage association, point clouds, and planar laser-scan visualization.

### Validation

- xUnit runtime suite passed 66/66 tests.
- `build/RRD/phase11_sensor_smoke.rrd` passed `rerun rrd verify`.
- Manual replay confirmed `/world/camera`, `/world/points`, `/world/laser_scan`, and `/world/laser_scan_outline`.

## 0.3.0 - 2026-05-10

### Added

- RRD footer and manifest finalization by default.
- Official `rerun rrd verify` compatibility for Phase3 and Phase8 smoke recordings.
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
- Phase 8 sidecar control is Windows Editor focused; Player sidecar support remains a later validation item.
- Live gRPC requires project-level `YetAnotherHttpHandler`.

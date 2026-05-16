# Unity2Rerun v0.4.0 Release Notes

Release date: 2026-05-17

Unity2Rerun v0.4.0 is the first public release candidate prepared for GitHub-to-Zenodo archival. It combines the Phase 9 RRD footer/manifest work, Phase 10 Rerun-first interactive parity, and Phase 11 sensor typed publisher slice into a citable software snapshot.

## Highlights

- **Official-compatible `.rrd` files:** New recordings include RRD footer/manifests and pass `rerun rrd verify`.
- **Live and file output:** `RerunManager` supports FileOnly, LiveOnly, and FileAndLive modes.
- **Interactive 3D sample:** Camera image, cube transform/box, trajectory, fps, command count, TextLog, sidecar action controls, and Rerun replay are covered.
- **Sensor typed publishing:** Pinhole camera metadata, point clouds, and planar laser scans are available through API and Inspector publishers.
- **Generated logging:** `[RerunLog]`, `[RerunScalar]`, and `[RerunTransform]` use generated code instead of runtime reflection.
- **IL2CPP-oriented architecture:** The generated logging path is designed around Editor source generation plus build-time generated-file fallback.

## Included User-Facing APIs

- `RerunManager.LogText`
- `RerunManager.LogScalar`
- `RerunManager.LogTransform`
- `RerunManager.LogEncodedImage`
- `RerunManager.LogPinhole`
- `RerunManager.LogBox3D` / `LogBoxes3D`
- `RerunManager.LogLineStrips3D`
- `RerunManager.LogPoints3D`

## Publisher Components

- `RerunCameraImagePublisher`
- `RerunPinholeCameraPublisher`
- `RerunPointCloudPublisher`
- `RerunLaserScanPublisher`
- Transform, Scalar, TextLog, Points3D, and Interactive 3D publishers

## Compatibility Notes

- Verified target: Windows Editor and Windows Standalone IL2CPP Player.
- Unity target: Unity 6000.0 LTSC or later.
- Developed on Unity 6000.3.14f1 LTSC and compatible with Unity 6000.0.74f1 LTSC.
- Rerun Viewer / CLI target: 0.31.4+.
- Live gRPC output requires project-level Cysharp `YetAnotherHttpHandler` 1.11.5 and native dependency package.
- WebGL is not supported.

## Validation

Completed before preparing this release:

```powershell
dotnet test Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj --no-restore
dotnet run --project Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj -- --write-phase11-rrd build/RRD/phase11_sensor_smoke.rrd
rerun rrd verify build/RRD/phase11_sensor_smoke.rrd
rerun rrd stats build/RRD/phase11_sensor_smoke.rrd
```

Observed result:

- xUnit: 66 / 66 passed.
- Rerun verify: `phase11_sensor_smoke.rrd` verified without error.
- Rerun stats listed Pinhole, EncodedImage, Points3D, LineStrips3D, Transform3D, Scalar, TextLog, and ViewCoordinates components.
- Manual replay confirmed camera image, Pinhole camera entity, point cloud, laser scan points, laser scan outline, cube, trajectory, and fps plots.

## Citation

This release is prepared for Zenodo archival. After the GitHub release is processed by Zenodo, use the generated version-specific DOI for exact reproduction of v0.4.0, or the Concept DOI for citing the Unity2Rerun project as a whole.

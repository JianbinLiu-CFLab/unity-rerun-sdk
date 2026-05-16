# Unity2Rerun SDK 0.4.0 Release Notes

Date: 2026-05-17

Unity2Rerun 0.4.0 closes the Phase 11 sensor typed publisher slice. The release adds Rerun `Pinhole` camera metadata, camera/image association guidance, point-cloud publishing, and planar laser-scan visualization on top of the 0.3.0 RRD footer/manifest work.

## Highlights

- **Pinhole camera metadata:** `RerunPinhole` and `RerunManager.LogPinhole` publish Rerun `Pinhole` components as static chunks.
- **Camera publisher parity:** `RerunPinholeCameraPublisher` emits camera intrinsics/frustum metadata and can publish camera pose on the same entity path.
- **Sensor-oriented publishers:** `RerunPointCloudPublisher` and `RerunLaserScanPublisher` provide Unity Inspector entry points for point clouds and scan ranges.
- **Rerun-first replay:** Phase 11 smoke recordings include `Pinhole`, `EncodedImage`, `Points3D`, and `LineStrips3D` data that pass official Rerun CLI verification.

## Compatibility Notes

- Existing scenes keep previous serialized values.
- To associate image and Pinhole metadata in Rerun Viewer, set `RerunCameraImagePublisher` and `RerunPinholeCameraPublisher` to the same entity path, for example `world/camera`.
- Pinhole camera coordinates use Rerun `RDF` (`[3, 2, 5]`: right, down, forward), while the world entity remains `RIGHT_HAND_Y_UP` (`[3, 1, 6]`).
- The first Pinhole helper derives `fx` and `fy` from Unity vertical FOV and assumes `fx == fy`; it is intended for visualization, not calibrated camera intrinsics.

## Validation

Completed before preparing this release:

```powershell
python Scripts/release/validate_package.py
dotnet test Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj --no-restore
dotnet run --project Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj -- --write-phase11-rrd build/RRD/phase11_sensor_smoke.rrd
rerun rrd verify build/RRD/phase11_sensor_smoke.rrd
rerun rrd stats build/RRD/phase11_sensor_smoke.rrd
```

Observed result:

- xUnit: 66 / 66 passed.
- Release package validation: passed.
- Rerun verify: `phase11_sensor_smoke.rrd` verified without error.
- Rerun stats listed `Pinhole:image_from_camera`, `Pinhole:resolution`, `Pinhole:camera_xyz`, `Points3D:positions`, `LineStrips3D:strips`, and `EncodedImage:blob`.
- Manual replay confirmed camera image, Pinhole camera entity, point cloud, laser scan points, and laser scan outline.

## Zenodo / Citation

This release is archived by Zenodo.

- Concept DOI for citing Unity2Rerun across all versions: [10.5281/zenodo.20247512](https://doi.org/10.5281/zenodo.20247512)
- Version DOI for exact reproduction of v0.4.0: [10.5281/zenodo.20247513](https://doi.org/10.5281/zenodo.20247513)

# Interactive 3D Control Sample

## Purpose

This sample is the broadest Unity2Rerun manual acceptance scene. It combines Unity Game view interaction, Rerun 3D visualization, camera image publishing, pinhole metadata, optional point-cloud and laser-scan publishers, metrics, text logs, and a local loopback sidecar control page.

## Setup

1. Create a cube and add `RerunInteractiveCubeController`.
2. Add `RerunManager` to the scene and choose `FileOnly` or `FileAndLive`.
3. Add `RerunInteractive3DPublisher` to the cube or a nearby driver object.
4. Add `RerunInteractiveControlBridge` to the cube or a nearby driver object if sidecar control is needed.
5. Add `RerunCameraImagePublisher` to a camera-facing object or the camera itself.
6. Add `RerunPinholeCameraPublisher` to the same camera and set both camera publishers to the same entity path, such as `world/camera`, when camera metadata should align with images.
7. Optionally add `RerunPointCloudPublisher` or `RerunLaserScanPublisher` for point-cloud or planar scan visualization.

Keep `Run In Background` enabled on `RerunManager` when using the sidecar page. Sidecar commands are received on a loopback HTTP thread and applied from Unity `Update`.

## Controls

- Left mouse drag: rotate the cube in the Game view.
- Right mouse drag: pan the cube in the camera plane.
- Mouse wheel: zoom or scale the cube.
- `R`: reset the Unity-side pose.

## Expected Output

Expected Rerun streams:

- `world/cube` for Transform3D and Boxes3D
- `world/cube_trajectory`
- `camera/main`
- `metrics/interactive/fps`
- `metrics/interactive/trajectory_points`
- `metrics/interactive/command_count`
- `logs/rerun/control`
- `logs/rerun/image`

Optional streams:

- `world/points` if `RerunPoints3DPublisher` is added
- `world/camera` if `RerunPinholeCameraPublisher` is aligned with the camera image entity path
- `world/laser_scan`
- `world/laser_scan_outline`

## Sidecar Control

The bridge defaults to `http://127.0.0.1:18765/`. If that port is busy, it falls back to a random free loopback port. Use the runtime Control URL field in the Inspector, or the `logs/rerun/control` TextLog stream, to find the actual URL.

The sidecar is intentionally local-only:

- no remote bind
- no TLS
- no authentication
- intended for local Editor sample control

The sidecar exposes action buttons, writable `cube.color` and `cube.scale` state, pose state, current color, command count, last command, and the current control URL.

## Manual Acceptance

- Move the cube in the Unity Game view and confirm `world/cube` and `world/cube_trajectory` update.
- Confirm `camera/main` displays the camera image when the camera publisher is enabled.
- Confirm `world/camera` appears when pinhole metadata is enabled and entity paths are aligned.
- Confirm metrics update while Play Mode runs.
- Open the sidecar URL, apply a color or scale command, and confirm `logs/rerun/control` and `metrics/interactive/command_count` update.
- Stop Play Mode and verify the generated `.rrd` file.

## Troubleshooting Notes

- If old metrics appear in the Viewer, reset the Viewer layout or open a fresh layout.
- If sidecar commands do not apply while the browser has focus, enable Run In Background.
- If the camera image is missing, confirm an active camera exists and Max Encoded Bytes is not too low.
- If point or scan streams are missing, confirm the optional publishers are present and enabled.

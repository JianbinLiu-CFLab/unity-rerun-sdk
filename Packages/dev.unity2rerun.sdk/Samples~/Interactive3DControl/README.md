# Interactive 3D Control Sample

This sample demonstrates the Phase 8 loop:

- Unity Game View controls a cube.
- Unity2Rerun publishes Transform3D, Boxes3D, LineStrips3D trajectory, optional Points3D, Pinhole camera metadata, EncodedImage, metrics, and TextLog.
- A local sidecar panel controls Unity through `127.0.0.1` with action buttons and parameter-like state.
- Rerun Viewer visualizes the result live or from a `.rrd` file.

## Scene Setup

1. Create a cube and add `RerunInteractiveCubeController`.
2. Add `RerunManager` to the scene and choose `FileAndLive` or `FileOnly`.
3. Add `RerunInteractive3DPublisher` to the cube or a nearby driver object.
4. Add `RerunInteractiveControlBridge` to the cube or a nearby driver object.
5. Add `RerunCameraImagePublisher` to a camera-facing object or the camera itself.
6. Optional for Phase 11 sensor smoke: add `RerunPinholeCameraPublisher` to the same camera and set both camera publishers to the same entity path, such as `world/camera`.
7. Optional: add `RerunPointCloudPublisher` or `RerunLaserScanPublisher` to a nearby GameObject for point-cloud or planar scan visualization.

Keep `Run In Background` enabled on `RerunManager`. Sidecar commands arrive on a loopback HTTP thread and are applied from Unity `Update`; if Unity stops updating while the browser has focus, commands will appear to apply only after returning to or stopping Play mode.

## Controls

- Drag with left mouse button: rotate the cube in the Game View.
- Drag with right mouse button: pan the cube in the camera plane.
- Mouse wheel: zoom/scale the cube.
- `R`: reset the Unity-side pose.

## Sidecar Panel

The bridge defaults to `http://127.0.0.1:18765/`.

If that port is busy, the server falls back to a random free loopback port. Use the `Control URL` field in the Inspector or the `logs/rerun/control` TextLog stream to find the actual URL.

The sidecar is intentionally local-only:

- no remote bind
- no TLS
- no auth
- Windows Editor sample target for the current interactive control path

The `/state` endpoint includes:

- `actions`: reset pose, color presets, scale up/down/reset
- `parameters`: writable `cube.color` and `cube.scale`
- pose, current color, command count, last command, and control URL

The sidecar page renders those actions/parameters dynamically and posts commands to `/command`. Accepted commands are logged back to Rerun as `logs/rerun/control` and update `metrics/interactive/command_count`.

## Rerun Streams

Expected streams:

- `world/cube` (Transform3D + Boxes3D)
- `world/cube_trajectory`
- `world/points` if `RerunPoints3DPublisher` is added
- `world/camera` if `RerunPinholeCameraPublisher` is added with the camera image entity path aligned
- `world/laser_scan` and `world/laser_scan_outline` if `RerunLaserScanPublisher` is added
- `camera/main`
- `metrics/interactive/fps`
- `metrics/interactive/trajectory_points`
- `metrics/interactive/command_count`
- `logs/rerun/control`
- `logs/rerun/image`

If the Viewer shows old panels such as `metrics/fps` or `metrics/generated_fps`, that is a persisted Viewer blueprint rather than Phase 8 data. Run `rerun reset` or create a fresh layout, then reopen the recording.

## Recommended Rerun Layout

Use a manual grid layout for Phase 10:

- 3D view: `world`
- Image view: `camera/main`
- Time series: `metrics/interactive/fps`
- Time series: `metrics/interactive/command_count`
- Time series: `metrics/interactive/trajectory_points`
- Text log: `logs/rerun/control`

Rerun Desktop does not currently provide a Foxglove-style Parameters or Service Call panel for arbitrary Unity callbacks. The sidecar page is the supported control surface for this sample.

## Acceptance Notes

Record a short acceptance pass in `Developer/Phase8_Interactive_3D_EncodedImage_Sidecar_Control_Acceptance.md`:

- Rerun Viewer screenshot with 3D cube, trajectory, image, plots, and logs.
- Unity Game View control smoke.
- Sidecar panel control smoke.
- FileOnly replay smoke.
- Unity Profiler samples for image encode and spatial publish paths.

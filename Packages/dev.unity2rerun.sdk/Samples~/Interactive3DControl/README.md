# Interactive 3D Control Sample

This sample demonstrates the Phase 8 loop:

- Unity Game View controls a cube.
- Unity2Rerun publishes Transform3D, Boxes3D, LineStrips3D trajectory, EncodedImage, metrics, and TextLog.
- A local sidecar panel controls Unity through `127.0.0.1`.
- Rerun Viewer visualizes the result live or from a `.rrd` file.

## Scene Setup

1. Create a cube and add `RerunInteractiveCubeController`.
2. Add `RerunManager` to the scene and choose `FileAndLive` or `FileOnly`.
3. Add `RerunInteractive3DPublisher` to the cube or a nearby driver object.
4. Add `RerunInteractiveControlBridge` to the cube or a nearby driver object.
5. Add `RerunCameraImagePublisher` to a camera-facing object or the camera itself.

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
- Windows Editor sample target for Phase 8

## Rerun Streams

Expected streams:

- `world/cube` (Transform3D + Boxes3D)
- `world/cube_trajectory`
- `camera/main`
- `metrics/interactive/fps`
- `metrics/interactive/trajectory_points`
- `metrics/interactive/command_count`
- `logs/rerun/control`
- `logs/rerun/image`

If the Viewer shows old panels such as `metrics/fps` or `metrics/generated_fps`, that is a persisted Viewer blueprint rather than Phase 8 data. Run `rerun reset` or create a fresh layout, then reopen the recording.

## Acceptance Notes

Record a short acceptance pass in `Developer/Phase8_Interactive_3D_EncodedImage_Sidecar_Control_Acceptance.md`:

- Rerun Viewer screenshot with 3D cube, trajectory, image, plots, and logs.
- Unity Game View control smoke.
- Sidecar panel control smoke.
- FileOnly replay smoke.
- Unity Profiler samples for image encode and spatial publish paths.

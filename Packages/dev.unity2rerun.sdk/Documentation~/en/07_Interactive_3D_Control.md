# Interactive 3D Control

Phase 8 added a visible end-to-end demo for Unity2Rerun. Phase 10 polishes it into the Rerun-first parity sample:

- `LogEncodedImage` publishes JPEG/PNG bytes as Rerun `EncodedImage`.
- `LogBox3D` / `LogBoxes3D` publishes Rerun `Boxes3D` using half-extents.
- `LogLineStrips3D` publishes a trajectory as Rerun `LineStrips3D`.
- `LogPoints3D` / `RerunPoints3DPublisher` publishes Rerun `Points3D` point clouds.
- `RerunInteractiveControlBridge` exposes a local-only sidecar panel for controlling Unity.

## Sidecar Control

The sidecar binds only to `127.0.0.1`. The default port is `18765`; if it is unavailable, the server falls back to a random free loopback port and logs the actual URL.

Keep `Run In Background` enabled on `RerunManager`. The sidecar receives commands on a background HTTP thread, then applies them from Unity `Update`; Unity must continue ticking while the browser or Rerun Viewer has focus.

The sidecar publishes parameter-like state through `/state` and accepts action commands through `/command`. The built-in state model includes:

- actions: reset pose, color presets, scale up/down/reset
- parameters: writable `cube.color` and `cube.scale`
- pose, color, command count, last command, and control URL

Accepted commands are logged back into Rerun as `logs/rerun/control` and increment `metrics/interactive/command_count`.

The sidecar is a Windows Editor sample feature. It does not modify Rerun Viewer and does not expose remote control.

## Image Publishing

`RerunCameraImagePublisher` captures a camera, encodes JPEG or PNG, and publishes:

```csharp
manager.LogEncodedImage("camera/main", bytes, "image/jpeg");
```

The default sample profile is `640x480`, JPEG quality `70`, `10Hz`, with a maximum encoded payload size.

## 3D Publishing

For Unity cubes, use half-extents:

```csharp
var halfSize = Abs(transform.lossyScale) * 0.5f;
manager.LogTransform("world/cube", transform);
manager.LogBox3D("world/cube", Vector3.zero, halfSize, Quaternion.identity, Color.green);
```

The sample logs Transform3D and Boxes3D on the same `world/cube` entity. This mirrors the Foxglove scene pattern: the transform moves the entity, while the cube primitive stays centered in the entity-local frame. `lossyScale * 0.5f` is a practical sample default for a Unity cube.

Trajectory publishing keeps the most recent points and sends them as one `LineStrips3D` entity.

Point publishing uses Rerun `Points3D`:

```csharp
manager.LogPoints3D("world/points", pointPositions, Color.cyan, radius: 0.03f);
```

For Inspector-driven smoke tests, add `RerunPoints3DPublisher` to a GameObject. It publishes a small synthetic point cloud around the object so the Rerun 3D view has a point-cloud parity slice without adding lidar packet decoding.

## Recommended Viewer Layout

Use a manual Rerun Viewer grid layout:

- 3D view: `world`
- Image view: `camera/main`
- Time series: `metrics/interactive/fps`
- Time series: `metrics/interactive/command_count`
- Time series: `metrics/interactive/trajectory_points`
- Text log: `logs/rerun/control`

Rerun Desktop does not currently expose a Foxglove-style Parameters or Service Call panel for arbitrary Unity callbacks. Unity2Rerun uses the local sidecar page for that control surface.

## Performance Notes

Use Unity Profiler markers:

- `RerunCameraImagePublisher.Update`
- `RerunCameraImagePublisher.ImageEncode`
- `RerunInteractive3DPublisher.Spatial`

Record average/peak frame time, GC allocation, and encoded payload size in the Phase 8 acceptance note.

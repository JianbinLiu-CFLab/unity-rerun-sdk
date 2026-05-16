# Publisher Components

Publisher components let you record Rerun data through the Unity Inspector — no code required.

## RerunManager

The central hub. One per scene, typically on a dedicated `Rerun` GameObject.

| Field | Description |
|-------|-------------|
| Application Id | Name shown in Rerun Viewer |
| Output Mode | FileOnly / LiveOnly / FileAndLive |
| Output Path | `.rrd` file path. Use `{TIMESTAMP}` for auto-naming |
| Record On Start | Start recording automatically on Play |
| Write View Coordinates | Write `RIGHT_HAND_Y_UP` on `world` entity |

## RerunTransformPublisher

Publishes a Transform's position and rotation to `world/<entity>`.

| Field | Description |
|-------|-------------|
| Target | Transform to publish. Empty = self |
| Entity Path | Custom path. Empty = auto from GameObject |
| Publish Rate Hz | 0 = every frame, 10 = 10 Hz |

## RerunScalarPublisher

Publishes a numeric value to `<entity>`.

| Field | Description |
|-------|-------------|
| Source | Built-in data source (Fps, DeltaTime, TransformPosition, Constant, etc.) |
| Constant Value | Value when source is Constant |
| Target | Transform for position/rotation sources |

## RerunTextLogPublisher

Publishes a text log entry to `logs/<entity>`.

| Field | Description |
|-------|-------------|
| Message | Log body text |
| Level | INFO, WARN, ERROR, DEBUG, TRACE |
| Repeat | Publish continuously (false = one-shot) |
| Append Frame Count | Add frame number to message |

## RerunPoints3DPublisher

Publishes a small synthetic Rerun `Points3D` point cloud around the GameObject. This is the first sensor parity slice and is intended for 3D view smoke tests rather than lidar packet decoding.

| Field | Description |
|-------|-------------|
| Point Count | Number of points to emit |
| Cloud Radius | Synthetic cloud radius around the GameObject |
| Point Radius | Rerun visual radius per point |
| Color | Point color |
| Animate | Rotate the synthetic point cloud over time |

## RerunPinholeCameraPublisher

Publishes Rerun `Pinhole` camera metadata, optionally with the camera pose on the same entity path. Use the same entity path as `RerunCameraImagePublisher` when you want the Viewer to associate the camera image and the frustum.

| Field | Description |
|-------|-------------|
| Camera | Camera to describe. Empty = `Camera.main` |
| Entity Path | Defaults to `world/camera` |
| Width / Height | Resolution used by `Pinhole:resolution` |
| Image Plane Distance | Frustum image-plane distance in the 3D view |
| Frustum Color | Wireframe color |
| Line Width | Wireframe radius used by Rerun `Radius` |
| Publish Camera Pose | Also publish `Transform3D` on the camera entity |

The camera space uses Rerun `RDF` (`[3, 2, 5]`: right, down, forward). This intentionally differs from the world entity's `RIGHT_HAND_Y_UP` (`[3, 1, 6]`). The first implementation derives `fx` and `fy` from Unity's vertical FOV and assumes `fx == fy`, which is suitable for visualization but not calibrated camera intrinsics.

## RerunPointCloudPublisher

Publishes external point-cloud frames or a set of Transform sources through Rerun `Points3D`.

| Field | Description |
|-------|-------------|
| Sources | Transforms used as point positions |
| Use Children When Sources Empty | Publish child transforms when no source array is assigned |
| Default Color | Point color for transform-derived points |
| Default Radius | Rerun visual radius per point |

Code can call `SetFrame(RerunPointCloudFrame frame)` to publish sensor-owned positions, colors, and radii without using child transforms.

## RerunLaserScanPublisher

Publishes planar laser-scan ranges as `Points3D` and, optionally, a `LineStrips3D` outline.

| Field | Description |
|-------|-------------|
| Beam Count | Number of synthetic demo ranges when no external ranges are provided |
| Angle Min / Max | Scan fan in degrees |
| Range Min / Max | Valid range window |
| Publish Points | Emit `world/laser_scan` `Points3D` |
| Publish Line Strip | Emit `world/laser_scan_outline` `LineStrips3D` |
| Point Radius | Rerun visual radius per point |

Code can call `SetRanges(IReadOnlyList<float> ranges)` to feed real scan data. Invalid, NaN, infinite, or out-of-range samples are skipped before publishing.

# Inspector Reference

## Purpose

Use this page when you need field-by-field guidance for Unity2Rerun components in the Unity Inspector.

## RerunManager

| Field | Meaning | Notes |
| --- | --- | --- |
| Application Id | Application name shown in Rerun Viewer. | Defaults to `unity_app`. |
| Output Mode | Selects file output, live output, or both. | `FileOnly` is the dependency-light default. |
| Output Path | `.rrd` output path. | Relative paths resolve from the Unity project root. `{TIMESTAMP}` is supported. |
| Recording Compression | Compression for file recording. | Applies to `.rrd` Arrow payloads only; live gRPC remains uncompressed. |
| Record On Start | Starts recording automatically when the component starts. | Disable if code or UI should call `StartRecording()`. |
| Run In Background | Keeps Unity updating while another app has focus. | Recommended when using Rerun Viewer or the sidecar panel. |
| Write View Coordinates | Writes the world coordinate convention at recording start. | Keep enabled unless you intentionally manage coordinates yourself. |
| Live Endpoint | Rerun gRPC endpoint. | Default is `rerun+http://127.0.0.1:9876/proxy`. |
| Auto Launch Viewer | Starts Rerun Viewer if needed. | Requires `rerun` on PATH or Viewer Executable Path set. |
| Viewer Executable Path | Full path to the `rerun` executable. | Leave empty to auto-detect from PATH. |
| Connect Timeout Ms | Live connection timeout. | Increase on slow machines. |
| Reconnect Delay Ms | Delay between live reconnect attempts. | Used after live transport failures. |
| Max Live Queue Messages | Live queue size before dropping. | File output is not dropped by this queue. |

During Play Mode, the custom Inspector also shows status and transport health: recording state, live state, resolved output path, live support/running flags, queue depth, dropped count, reconnect count, sent StoreInfo count, sent data count, last error, sidecar control URL, and command count.

## Shared Publisher Fields

All `RerunPublisherBase` publishers share these fields:

| Field | Meaning | Notes |
| --- | --- | --- |
| Manager | Target `RerunManager`. | Leave empty to auto-detect one in the scene. |
| Entity Path | Rerun entity path. | Leave empty to derive from the GameObject hierarchy where supported. |
| Publish Rate Hz | Publish cadence. | `0` or negative means every frame. |
| Publish On Enable | Starts publishing when enabled. | Disable for manual `PublishOnce()` workflows. |
| Warn If Manager Missing | Logs a warning if no manager is found. | Useful while setting up scenes. |

## Publisher-Specific Fields

| Component | Fields | Notes |
| --- | --- | --- |
| Rerun Text Log Publisher | Message, Level, Repeat, Append Frame Count | Publishes TextLog entries. If Repeat is false, it publishes once then stops. |
| Rerun Scalar Publisher | Source, Constant Value, Target | Source includes FPS, delta times, frame count, transform position/rotation axes, and constant values. Target appears for transform-based sources. |
| Rerun Transform Publisher | Target | Publishes the target transform, or this GameObject's transform when empty. |
| Rerun Camera Image Publisher | Camera, Width, Height, Format, JPEG Quality, Max Encoded Bytes | Captures a camera to JPEG or PNG. Oversized frames can be dropped and logged. |
| Rerun Pinhole Camera Publisher | Camera, Width, Height, Image Plane Distance, Frustum Color, Line Width, Publish Camera Pose | Publishes static pinhole metadata once, then optionally publishes the camera pose on the same entity path. |
| Rerun Points3D Publisher | Point Count, Cloud Radius, Point Radius, Color, Animate | Generates a synthetic point cloud around the component transform. |
| Rerun Point Cloud Publisher | Sources, Use Children When Sources Empty, Default Color, Default Radius | Publishes transform-derived points or frames provided from code through `SetFrame(...)`. |
| Rerun Laser Scan Publisher | Beam Count, Angle Min/Max, Range Min/Max, Publish Points, Publish Line Strip, Animate Synthetic Ranges, Point Color, Line Color, Point Radius | Publishes scan points and an optional outline line strip. External ranges can be provided from code. |
| Rerun Interactive Control Bridge | Manager, Target, Preferred Port, Start On Enable, Control URL | Starts a local loopback sidecar page for sample control. Control URL is read-only at runtime. |

## Defaults To Keep

- Keep `Run In Background` enabled when using live Viewer or sidecar control.
- Keep `FileOnly` for dependency-light file smoke tests.
- Use `FileAndLive` when you want live Viewer updates while retaining a reliable `.rrd` artifact.
- Keep camera image and pinhole entity paths aligned when you want the Viewer to associate image and camera metadata.


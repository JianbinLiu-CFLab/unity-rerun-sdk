# Rerun Type Coverage Matrix

Date: 2026-05-18

Source snapshot: `third-party/rerun/crates/store/re_sdk_types/definitions/rerun`

This matrix records the Unity2Rerun runtime coverage against the official Rerun SDK FlatBuffer type definitions. The primary denominator is runtime `archetypes/` and `components/` only: `blueprint/`, `datatypes/`, and `testing/` are intentionally excluded from the archetype coverage count because they are viewer configuration, shared support types, or test-only schema.

Current baseline:

- Official runtime archetypes: 48
- Official runtime components: 78
- Unity2Rerun emitted archetypes: 9
- Unity2Rerun emitted components: 16

## Status Model

- `Done`: emitted by `RerunArrowIpcEncoder` and backed by a public manager, publisher, or test surface.
- `Partial`: some encoder support exists, but the Unity-facing surface or validation evidence is incomplete.
- `Future`: relevant to Unity2Rerun, but not implemented yet.
- `Not Unity-scope`: official Rerun type exists, but it is outside the current Unity2Rerun runtime target.

## Archetype Matrix

| Official archetype | Status | Unity2Rerun evidence | User surface | Next decision |
| --- | --- | --- | --- | --- |
| AnnotationContext | Future | No encoder output | None | Consider with semantic labels and class/keypoint workflows. |
| Arrows2D | Future | No encoder output | None | Candidate for 2D overlay geometry. |
| Arrows3D | Future | No encoder output | None | Candidate for vectors, normals, and force visualization. |
| Asset3D | Future | No encoder output | None | Candidate after mesh/material asset import policy is defined. |
| AssetVideo | Future | No encoder output | None | Candidate with video stream support. |
| BarChart | Future | No encoder output | None | Candidate for simple metric dashboards. |
| Boxes2D | Future | No encoder output | None | Candidate for camera detection overlays. |
| Boxes3D | Done | `RerunArrowIpcEncoder`; `Phase8ArchetypeTests`; `RerunManager.LogBoxes3D` | Manager API | Keep aligned with 3D geometry tests. |
| Capsules3D | Future | No encoder output | None | Candidate for character/controller debug geometry. |
| Clear | Future | No encoder output | None | Needs explicit scene-clearing semantics before exposing. |
| CoordinateFrame | Future | No encoder output | None | Candidate for compact frame-axis visualization. |
| Cylinders3D | Future | No encoder output | None | Candidate for 3D primitive coverage. |
| DepthImage | Future | No encoder output | None | Candidate with camera/depth sensor pipeline. |
| Ellipsoids3D | Future | No encoder output | None | Candidate for covariance and uncertainty visualization. |
| EncodedDepthImage | Future | No encoder output | None | Candidate after depth encoding and calibration policy. |
| EncodedImage | Done | `RerunArrowIpcEncoder`; `Phase8ArchetypeTests`; `RerunCameraImagePublisher` | Manager API and publisher component | Keep paired with camera/pinhole smoke tests. |
| GeoLineStrings | Future | No encoder output | None | Candidate only if geospatial Unity use cases emerge. |
| GeoPoints | Future | No encoder output | None | Candidate only if geospatial Unity use cases emerge. |
| GraphEdges | Future | No encoder output | None | Candidate for graph/network debug views. |
| GraphNodes | Future | No encoder output | None | Candidate for graph/network debug views. |
| GridMap | Future | No encoder output | None | Candidate for occupancy or navigation maps. |
| Image | Future | No encoder output | None | Candidate if raw image buffers are needed in addition to encoded image. |
| InstancePoses3D | Future | No encoder output | None | Candidate for instanced meshes and large repeated objects. |
| LineStrips2D | Future | No encoder output | None | Candidate for 2D overlay lines. |
| LineStrips3D | Done | `RerunArrowIpcEncoder`; `Phase8ArchetypeTests`; `RerunLaserScanPublisher` | Manager API and publisher component | Keep as the scan outline and 3D polyline path. |
| McapChannel | Not Unity-scope | No encoder output | None | MCAP reflection/data-loader family; Unity2Rerun records native RRD. |
| McapMessage | Not Unity-scope | No encoder output | None | MCAP reflection/data-loader family; Unity2Rerun records native RRD. |
| McapSchema | Not Unity-scope | No encoder output | None | MCAP reflection/data-loader family; Unity2Rerun records native RRD. |
| McapStatistics | Not Unity-scope | No encoder output | None | MCAP reflection/data-loader family; Unity2Rerun records native RRD. |
| Mesh3D | Future | No encoder output | None | Candidate after mesh topology and material decisions. |
| Pinhole | Done | `RerunArrowIpcEncoder`; `Phase11SensorTests`; `RerunPinholeCameraPublisher` | Manager API and publisher component | Keep associated with encoded image entity paths. |
| Points2D | Future | No encoder output | None | Candidate for screen-space detections and landmarks. |
| Points3D | Done | `RerunArrowIpcEncoder`; `Phase8ArchetypeTests`; `RerunPoints3DPublisher`; `RerunPointCloudPublisher` | Manager API and publisher component | Keep as the primary point-cloud surface. |
| RecordingInfo | Future | No encoder output | None | Candidate for richer recording metadata. |
| Scalars | Done | `RerunArrowIpcEncoder`; `RrdSmokeTests`; `RerunScalarPublisher` | Manager API and publisher component | Keep as the metric baseline. |
| SegmentationImage | Future | No encoder output | None | Candidate with annotation context support. |
| SeriesLines | Future | No encoder output | None | Candidate for plotting richer metric series. |
| SeriesPoints | Future | No encoder output | None | Candidate for plotting richer metric series. |
| Status | Future | No encoder output | None | Candidate for diagnostics and health reporting. |
| StatusConfiguration | Future | No encoder output | None | Candidate with status visualization policy. |
| Tensor | Future | No encoder output | None | Candidate only if raw tensor logging is needed. |
| TextDocument | Future | No encoder output | None | Candidate for structured text artifacts. |
| TextLog | Done | `RerunArrowIpcEncoder`; `RrdSmokeTests`; `RerunTextLogPublisher` | Manager API and publisher component | Keep as the logging baseline. |
| Transform3D | Done | `RerunArrowIpcEncoder`; `RrdSmokeTests`; `RerunTransformPublisher` | Manager API and publisher component | Keep as the spatial transform baseline. |
| TransformAxes3D | Future | No encoder output | None | Candidate for explicit transform gizmos. |
| VideoFrameReference | Future | No encoder output | None | Candidate with video stream support. |
| VideoStream | Future | No encoder output | None | Candidate with video stream support. |
| ViewCoordinates | Done | `RerunArrowIpcEncoder`; `Phase11SensorTests`; camera coordinate setup | Manager API | Keep as the camera/world coordinate convention anchor. |

## Component Coverage Summary

| Component | Used By | Status | Note |
| --- | --- | --- | --- |
| Blob | EncodedImage | Done | Encoded image bytes. |
| Color | Boxes3D, LineStrips3D, Pinhole, Points3D | Done | Shared packed color component. |
| HalfSize3D | Boxes3D | Done | Box extents. |
| ImagePlaneDistance | Pinhole | Done | Camera frustum depth. |
| LineStrip3D | LineStrips3D | Done | 3D polyline payload. |
| MediaType | EncodedImage | Done | Image MIME type. |
| PinholeProjection | Pinhole | Done | Camera intrinsics matrix. |
| Position3D | Points3D | Done | Point positions. |
| Radius | Pinhole, Points3D | Done | Line width and point radius. |
| Resolution | Pinhole | Done | Image dimensions. |
| RotationQuat | Boxes3D, Transform3D | Done | Quaternion rotation. |
| Scalar | Scalars | Done | Metric value. |
| Text | TextLog | Done | Log text payload. |
| TextLogLevel | TextLog | Done | Log severity level. |
| Translation3D | Boxes3D, Transform3D | Done | Centers and transforms. |
| ViewCoordinates | Pinhole, ViewCoordinates | Done | Rerun axis convention. |

## Top Next Candidates

- Image/sensor family: `Image`, `DepthImage`, `EncodedDepthImage`, `SegmentationImage`.
- 2D/3D geometry family: `Points2D`, `Boxes2D`, `LineStrips2D`, `Arrows3D`, `Mesh3D`, `CoordinateFrame`, `TransformAxes3D`.
- Plot/text family: `SeriesLines`, `SeriesPoints`, `BarChart`, `TextDocument`.
- Semantic context family: `AnnotationContext`, plus related class/keypoint components.
- Larger tracks: video, graph, geo, grid map, and tensor support should be scheduled only after a concrete Unity workflow exists.

## Validation

Run the drift check from the repository root:

```powershell
python Scripts/release/check_rerun_type_coverage.py
```

Optionally write a generated scan report under `build/reports`:

```powershell
python Scripts/release/check_rerun_type_coverage.py --write build/reports/rerun_type_coverage.md
```

# Publisher Components Sample

## Purpose

This sample verifies no-code publishing through Inspector-driven components. A small driver configures TextLog, Scalar, and Transform3D publishers on an existing target object.

## Setup

1. Create an empty GameObject named `Rerun`.
2. Add `RerunManager` to `Rerun`.
3. Create a cube named `SampleCube`.
4. Add `RerunPublisherSampleDriver` to `Rerun`.
5. Assign `SampleCube` to the driver's Target Object field.
6. Enter Play Mode for a few seconds.
7. Stop Play Mode and open the generated `.rrd` file.

The sample expects an existing target object. It does not create scene objects automatically.

## Expected Output

The driver configures these outputs:

- `logs/unity`
- `metrics/fps`
- `world/cube`

## Manual Acceptance

- The Console reports that publisher components were configured on the target object.
- The generated `.rrd` opens in Rerun Viewer.
- TextLog, FPS scalar, and cube transform streams are present.

## Troubleshooting Notes

- If setup logs say Target Object is missing, assign a cube or another visible GameObject.
- If no data appears, confirm the `RerunManager` is recording.
- If an entity path is unexpected, inspect the publisher components added to the target object.

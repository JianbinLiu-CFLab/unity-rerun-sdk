# Generated RerunLog Sample

## Purpose

This sample verifies attribute-driven publishing through `[RerunLog]`, `[RerunScalar]`, and `[RerunTransform]`. It is the recommended sample when you want generated field logging without runtime reflection.

## Setup

1. In Package Manager, import `Generated RerunLog`.
2. Create a visible cube in the scene and name it `SampleCube`.
3. Add `RerunGeneratedLogSample` to `SampleCube`.
4. Add a `RerunManager` to the scene.
5. Set the manager to `FileOnly` or `FileAndLive`.
6. Enter Play Mode and let the scene run for a few seconds.

The sample does not create scene objects automatically. Keeping the cube visible in the Hierarchy makes the generated logging setup easy to inspect.

## Expected Output

Open the generated `.rrd` file in Rerun Viewer and confirm these streams exist:

- `logs/generated`
- `metrics/generated_fps`
- `world/generated_cube`

## Manual Acceptance

- Unity Console has no compile errors or generated source errors.
- The generated log stream updates about once per second.
- The generated FPS metric updates repeatedly.
- The cube transform appears as `world/generated_cube`.

## Troubleshooting Notes

- The annotated class must remain `partial`.
- A `RerunManager` must be active and recording.
- If no generated streams appear, let Play Mode run long enough for the configured publish rates.

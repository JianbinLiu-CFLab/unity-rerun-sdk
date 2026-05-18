# Basic Rrd Recording Sample

## Purpose

This is the smallest Unity2Rerun file-recording sample. It writes TextLog, Scalar, and Transform3D data to a local `.rrd` file through `RerunManager`.

## Setup

1. Import `Basic Rrd Recording` from Package Manager.
2. Create an empty GameObject named `Rerun`.
3. Add `RerunManager` to `Rerun`.
4. Add `RerunSampleLogger` to the same GameObject.
5. Keep Output Mode as `FileOnly`.
6. Enter Play Mode for a few seconds.
7. Stop Play Mode and open the `.rrd` path printed in the Console.

The sample creates a child cube named `SampleCube` at runtime.

## Expected Output

The generated `.rrd` should contain:

- `logs/unity`
- `metrics/fps`
- `world/cube`

## Manual Acceptance

- Unity Console reports a generated `.rrd` output path.
- `rerun rrd verify` succeeds for the saved file.
- Rerun Viewer shows the cube transform, FPS metric, and text logs.

## Troubleshooting Notes

- If no file is produced, confirm `RerunManager` is recording and Output Mode is `FileOnly` or `FileAndLive`.
- If the file is empty, let Play Mode run for a few seconds before stopping.
- Keep generated recordings outside `Packages/dev.unity2rerun.sdk`, normally under repository-level `build/RRD`.


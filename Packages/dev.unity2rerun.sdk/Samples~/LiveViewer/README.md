# Live Viewer Sample

## Purpose

This sample verifies `FileAndLive` output: Unity writes a `.rrd` file while also streaming TextLog, Scalar, and Transform3D data to Rerun Viewer.

## Setup

1. Install Cysharp `YetAnotherHttpHandler` and its native dependency package as described in `Documentation~/en/00_Prerequisites.md`.
2. Ensure Rerun Viewer 0.31.4 or newer is installed and available on PATH, or set `Viewer Executable Path` in `RerunManager`.
3. Add `RerunManager` to a GameObject.
4. Set Output Mode to `FileAndLive`.
5. Enable Auto Launch Viewer or start Rerun Viewer manually.
6. Attach `RerunLiveViewerSample` to the same GameObject.
7. Enter Play Mode.

FileOnly output does not require `YetAnotherHttpHandler`. Live gRPC output does.

## Expected Output

Live Viewer and the saved `.rrd` should show:

- `logs/unity`
- `metrics/fps`
- `world/cube`

## Manual Acceptance

- Rerun Viewer opens or connects successfully.
- Live TextLog, Scalar, and Transform3D updates appear while Play Mode runs.
- Stopping Play Mode still leaves a verifiable `.rrd` file.
- A live failure does not prevent FileAndLive file output from closing cleanly.

## Troubleshooting Notes

- If the Console reports an HTTP/1.1 downgrade, install or repair `YetAnotherHttpHandler`.
- If port `9876` is already used, stop the existing Rerun process or change the live endpoint.
- If live output is not required, switch to `FileOnly` and verify the file path first.

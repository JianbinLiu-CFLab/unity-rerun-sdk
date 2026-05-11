# Unity2Rerun SDK 0.3.0 Release Notes

Date: 2026-05-10

## Highlights

- `.rrd` files now include official-compatible RRD footer/manifests by default.
- `rerun rrd verify` passes for the Phase3 and Phase8 smoke recordings.
- Live transport exposes read-only health diagnostics in `RerunManager` and the Inspector.
- Interactive 3D sample records camera image, cube 3D box, trajectory, fps, command count, and sidecar control logs.

## Validation

```powershell
dotnet test Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj
rerun rrd verify build/RRD/phase9_phase3_smoke.rrd
rerun rrd verify build/RRD/phase9_phase8_smoke.rrd
rerun rrd stats build/RRD/phase9_phase3_smoke.rrd
rerun rrd stats build/RRD/phase9_phase8_smoke.rrd
```

Observed result:

- xUnit: 58 / 58 passed.
- Rerun verify: both smoke files verified without error.
- Rerun stats: expected Phase3 and Phase8 entity paths and components listed.

## Notes

- Existing no-footer recordings from earlier phases can still be opened directly in Rerun Viewer, but should be regenerated with 0.3.0 for `rerun rrd verify` compatibility.
- In this local environment, Rerun CLI printed an analytics access warning. Verification still passed and the warning is not an RRD data error.

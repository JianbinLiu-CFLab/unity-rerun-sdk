# Phase 14 RRD Compression Evidence

Date: 2026-05-18

Branch: `feature/phase14-rrd-compression-evidence-diagnostics`

## Summary

Phase 14 adds developer-side RRD compression diagnostics and a deterministic comparison smoke path. It does not add `Zstd` or change runtime recording behavior.

The comparison command writes one uncompressed `.rrd` and one LZ4 `.rrd` under repository-level `build/RRD`, then prints ArrowMsg compression evidence for both files.

## Commands

```powershell
dotnet test Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj --no-restore
python Scripts/release/validate_package.py
dotnet run --project Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj -- --write-phase14-compression-comparison build/RRD/phase14_compression
rerun rrd verify build/RRD/phase14_compression_none.rrd
rerun rrd verify build/RRD/phase14_compression_lz4.rrd
Get-ChildItem Packages/dev.unity2rerun.sdk -Recurse -Filter *.rrd
```

## Results

- `dotnet test`: passed, 77/77.
- `python Scripts/release/validate_package.py`: passed.
- `phase14_compression_none.rrd`: verified by `rerun rrd verify`.
- `phase14_compression_lz4.rrd`: verified by `rerun rrd verify`.
- Package `.rrd` scan: no files found.
- Unity Editor: user confirmed no compile errors, missing scripts, or missing types.

## Analyzer Evidence

None recording:

```text
ArrowMsg: 5
CompressionNone: 5
CompressionLz4: 0
CompressionOther: 0
StoredToUncompressedRatio: 1.000000
Accepted: True
```

LZ4 recording:

```text
ArrowMsg: 5
CompressionNone: 0
CompressionLz4: 5
CompressionOther: 0
StoredToUncompressedRatio: 0.535686
Accepted: True
```

## Viewer Port Note

Opening the generated None RRD produced a Rerun Viewer proxy port conflict (`os error 10048`) while still starting file loading. This is a local Viewer port-use issue, not RRD evidence failure.

For visual-only inspection, close stale Rerun Viewer processes or inspect the occupied proxy port first:

```powershell
Get-Process rerun -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 9876 -ErrorAction SilentlyContinue
```

Then reopen:

```powershell
rerun .\build\RRD\phase14_compression_none.rrd
rerun .\build\RRD\phase14_compression_lz4.rrd
```

Machine-readable acceptance is based on `RrdInspector` and `rerun rrd verify`; Viewer visual inspection is optional follow-up.

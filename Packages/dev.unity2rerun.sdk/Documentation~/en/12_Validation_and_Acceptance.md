# Validation and Acceptance

## Purpose

Use this page before publishing a release, merging documentation changes, or closing a manual Unity acceptance pass.

## Automated Checks

Run from the repository root:

```powershell
python Scripts/release/validate_package.py
python Scripts/release/check_rerun_type_coverage.py
dotnet test Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj --no-restore
git diff --check
```

Expected pass signals:

- package validation prints `Unity2Rerun release package validation passed`;
- type coverage reports 48 official runtime archetypes, 78 official runtime components, 9 emitted archetypes, and 16 emitted components;
- runtime tests pass with zero failures;
- `git diff --check` prints no whitespace errors.

## RRD Evidence

Use repository-level `build/RRD` for generated recordings.

Compression comparison:

```powershell
dotnet run --project Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj -- --write-phase14-compression-comparison build/RRD/phase14_compression
```

Verify generated files:

```powershell
rerun rrd verify build/RRD/phase14_compression_none.rrd
rerun rrd verify build/RRD/phase14_compression_lz4.rrd
rerun rrd stats build/RRD/phase14_compression_lz4.rrd
```

Expected pass signals:

- both files verify successfully;
- the None recording reports uncompressed Arrow messages;
- the LZ4 recording reports LZ4-compressed Arrow messages;
- the LZ4 stored payload bytes are lower than declared uncompressed bytes for the comparison recording.

## Manual Unity Acceptance

### Project Open

1. Open the `Unity2Rerun` project in Unity.
2. Wait for import and compilation to finish.
3. Confirm the Console has no compile errors, missing script warnings, missing type warnings, or namespace resolution errors.

### Basic RRD File Smoke

1. Import or set up the Basic Rrd Recording sample.
2. Run Play Mode for a few seconds.
3. Stop Play Mode and open the Console output path.
4. Verify the file with `rerun rrd verify`.

Expected streams:

- `logs/unity`
- `metrics/fps`
- `world/cube`

### Publisher Components Smoke

1. Create a `Rerun` GameObject with `RerunManager`.
2. Create a visible cube named `SampleCube`.
3. Add `RerunPublisherSampleDriver` and assign `SampleCube`.
4. Run Play Mode and open the generated `.rrd`.

Expected streams:

- `logs/unity`
- `metrics/fps`
- `world/cube`

### Live Viewer Smoke

Run this only when Cysharp `YetAnotherHttpHandler` is installed.

1. Set `RerunManager` Output Mode to `FileAndLive`.
2. Enable Auto Launch Viewer or start Rerun Viewer manually.
3. Run Play Mode.
4. Confirm live TextLog, Scalar, and Transform3D updates appear in the Viewer.
5. Stop Play Mode and verify the generated `.rrd`.

Expected result: live output may reconnect or fail without breaking file output.

### Generated RerunLog Smoke

1. Add `RerunManager`.
2. Add `RerunGeneratedLogSample` to a visible cube.
3. Run Play Mode long enough for at least one publish tick.
4. Open the generated `.rrd`.

Expected streams:

- `logs/generated`
- `metrics/generated_fps`
- `world/generated_cube`

### Interactive 3D Control Smoke

1. Set up the Interactive 3D Control sample.
2. Run Play Mode.
3. Move the cube in the Game view.
4. Open the sidecar Control URL if `RerunInteractiveControlBridge` is enabled.
5. Stop Play Mode and open the generated `.rrd`.

Expected streams:

- `world/cube`
- `world/cube_trajectory`
- `camera/main`
- `metrics/interactive/fps`
- `metrics/interactive/trajectory_points`
- `metrics/interactive/command_count`
- `logs/rerun/control`
- `logs/rerun/image`

Optional streams:

- `world/points`
- `world/camera`
- `world/laser_scan`
- `world/laser_scan_outline`

## Public Documentation Hygiene

Before committing documentation changes, confirm user-facing docs do not include local machine paths, Obsidian embeds, or private planning notes.

```powershell
rg -n -e "Developer[\\/]" -e "\bTODO\b" -e "\bTBD\b" -e "\bFIXME\b" -e "!\[\[" README.md Packages/dev.unity2rerun.sdk/README.md Packages/dev.unity2rerun.sdk/Documentation~ Packages/dev.unity2rerun.sdk/Samples~
```

If `rg` is unavailable:

```powershell
Get-ChildItem README.md, Packages/dev.unity2rerun.sdk/README.md, Packages/dev.unity2rerun.sdk/Documentation~, Packages/dev.unity2rerun.sdk/Samples~ -Recurse -File |
  Select-String -Pattern 'Developer[\\/]', '\bTODO\b', '\bTBD\b', '\bFIXME\b', '!\[\['
```


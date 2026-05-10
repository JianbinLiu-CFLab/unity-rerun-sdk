# Phase 7 RerunLog Source Generator Acceptance

Date: 2026-05-10
Status: Accepted

## Scope

Phase 7 adds attribute-driven telemetry publishing for Unity scripts:

- `[RerunLog]`
- `[RerunScalar]`
- `[RerunTransform]`

The generated code implements `IRerunGeneratedLogSource` and is driven by the visible scene `RerunManager`. Runtime publishing is zero-reflection. Editor uses Roslyn source generation for diagnostics and development experience; Player/IL2CPP uses physical `.g.cs` fallback generated before build.

## Architecture Decision

Phase 7 intentionally keeps three goals together:

- Editor path: Roslyn Incremental Source Generator provides compile-time diagnostics and generated partial classes.
- Player path: build-time physical `.g.cs` fallback gives IL2CPP/AOT deterministic C# input.
- Shared semantics: both paths use the shared emitter so Editor and Player behavior do not drift.

Runtime does not create a hidden Hub object. `RerunManager` discovers active `IRerunGeneratedLogSource` components and drives their generated publish methods.

## Automated Verification

Run from:

```powershell
cd "D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox"
```

Commands:

```powershell
dotnet test Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj --no-restore
dotnet build Packages/dev.unity2rerun.sdk/Editor/SourceGenerators/RerunLogSourceGenerator.csproj --no-restore
dotnet build Spikes/Phase1.RerunEncoding/Phase1.RerunEncoding.csproj --no-restore
dotnet build Spikes/Phase4.GrpcTransport/Phase4.GrpcTransport.csproj --no-restore
git diff --check
```

Expected:

- Runtime tests pass.
- Source generator project builds.
- Phase 1 and Phase 4 spikes build.
- Whitespace check is clean.

## Generator Layout Check

Expected analyzer output:

```text
Packages/dev.unity2rerun.sdk/Editor/SourceGenerators/analyzers/dotnet/cs/RerunLogSourceGenerator.dll
```

Important package hygiene:

- `Packages/dev.unity2rerun.sdk/Editor/SourceGenerators/obj/` must not exist.
- Source generator intermediate files belong under `build/SourceGenerators/...`, not inside the UPM package.
- The analyzer DLL must be imported as a Unity Roslyn analyzer, not as a normal runtime/editor plugin.

This avoids Unity trying to load analyzer build artifacts and failing on `Microsoft.CodeAnalysis`, `Microsoft.CodeAnalysis.CSharp`, or `System.Collections.Immutable`.

## Manual Editor RRD Smoke

1. Open the Unity project:

```text
D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\Unity2Rerun
```

2. In Unity Package Manager:

```text
In Project > Packages - Unity2Rerun > Unity2Rerun SDK > Samples
```

3. Import:

```text
Generated RerunLog
```

4. In the scene, create a cube manually:

```text
GameObject > 3D Object > Cube
```

5. Rename it:

```text
SampleCube
```

6. Select `SampleCube`, then add:

```text
Rerun Generated Log Sample
```

7. Ensure the scene has a visible `Rerun` GameObject with `RerunManager`.

8. Set `RerunManager`:

```text
Output Mode = FileOnly
Record On Start = enabled
Output Path = ../build/RRD/phase7_rerunlog_{TIMESTAMP}.rrd
```

9. Play for at least 5 seconds, then stop.

10. Open the newest generated file:

```powershell
cd "D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\RRD"
rerun .\phase7_rerunlog_最新那个.rrd
```

Accepted Editor RRD sample:

```text
D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\RRD\phase7_rerunlog_20260510_065323.rrd
```

Verification command:

```powershell
rerun rrd verify --check-footers false "D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\RRD\phase7_rerunlog_20260510_065323.rrd"
```

Observed result:

```text
1 file verified without error.
```

Expected Viewer entities:

- `logs/generated`
- `metrics/generated_fps`
- `world/generated_cube`

Observed:

- `logs/generated` showed text rows such as `generated frame ...`.
- `metrics/generated_fps` showed a scalar curve.
- `world/generated_cube` appeared under the world tree.

Note: if Rerun Viewer shows `metrics/fps has no own components...`, that is a stale blueprint/layout panel from older Phase 3/6 samples. Phase 7 writes `metrics/generated_fps`, not `metrics/fps`. Remove the stale panel or reset the blueprint.

## Manual FileAndLive Smoke

Use the same scene and set:

```text
Output Mode = FileAndLive
Auto Launch Viewer = enabled
```

Expected Unity Console messages:

```text
[Rerun] Viewer ready on http://127.0.0.1:9876/proxy
[RerunGrpcClient] Starting live stream loop to http://127.0.0.1:9876/proxy
[RerunGrpcClient] Using Cysharp YetAnotherHttpHandler for HTTP/2 live gRPC
[RerunGrpcClient] WriteMessages stream opened to http://127.0.0.1:9876/proxy
[RerunGrpcClient] StoreInfo sent to live stream
[RerunGrpcClient] Data message sent to live stream
[RerunGrpcClient] WriteMessages request stream completed
```

Accepted during earlier Phase 4/5 live validation. No new Phase 7-specific live failure was observed.

## IL2CPP Build

Run from:

```powershell
cd "D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\Scripts"
```

Command:

```powershell
python .\build_unity_il2cpp.py --unity-path "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe"
```

Accepted build:

```text
D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260510-070502\WindowsIL2CPP\Unity2RerunDemo.exe
```

Observed build output:

```text
[unity-log] [RerunBuild] Build succeeded: D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260510-070502\WindowsIL2CPP\Unity2RerunDemo.exe
[build_unity_il2cpp] Unity exited after 14:52.
[build_unity_il2cpp] Build command completed successfully.
```

Build log:

```text
D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260510-070502\Unity2Rerun_IL2CPP_build.log
```

Important build log evidence:

```text
[RerunLogBuildPreprocess] Generating RerunLog Player fallback sources...
[RerunLogBuildPreprocess] Generated 1 .g.cs file(s).
[RerunLogBuildPreprocess] Wrote RerunLog_link.xml for 1 type(s).
[RerunBuild] Build succeeded
```

Generated fallback files:

```text
D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\Unity2Rerun\Assets\Scripts\Generated\RerunLog\Unity_RerunSDK_Samples_RerunGeneratedLogSample_RerunLog.g.cs
D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\Unity2Rerun\Assets\RerunLog_link.xml
```

The generated `.g.cs` contains:

- `RerunLog_EntryCount => 3`
- `world/generated_cube`
- `logs/generated`
- `metrics/generated_fps`
- direct calls to `manager.LogTransform`, `manager.LogText`, and `manager.LogScalar`
- `#if !UNITY_EDITOR`, so it is physical Player fallback only

The generated `RerunLog_link.xml` preserves:

```text
Unity.RerunSDK.Samples.RerunGeneratedLogSample
```

## IL2CPP Player Smoke

Run:

```text
D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260510-070502\WindowsIL2CPP\Unity2RerunDemo.exe
```

Player log:

```text
C:\Users\LJB\AppData\LocalLow\DefaultCompany\Unity2Rerun\Player.log
```

Observed Player log:

```text
[Rerun] Recording started mode=FileOnly -> D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260510-070502\build\RRD\phase7_rerunlog_20260510_072240.rrd
[Rerun] Recording stopped -> D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260510-070502\build\RRD\phase7_rerunlog_20260510_072240.rrd
```

No Rerun runtime exception was observed in Player.log.

Accepted Player RRD:

```text
D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260510-070502\build\RRD\phase7_rerunlog_20260510_072240.rrd
```

Verify command:

```powershell
rerun rrd verify --check-footers false "D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260510-070502\build\RRD\phase7_rerunlog_20260510_072240.rrd"
```

Observed result:

```text
1 file verified without error.
```

Stats command:

```powershell
rerun rrd stats "D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260510-070502\build\RRD\phase7_rerunlog_20260510_072240.rrd"
```

Observed entity counts:

```text
/logs/generated: 23
/metrics/generated_fps: 219
/world: 1
/world/generated_cube: 627
```

Observed component counts:

```text
Scalars:scalars: 219
TextLog:level: 23
TextLog:text: 23
Transform3D:quaternion: 627
Transform3D:translation: 627
ViewCoordinates:xyz: 1
```

Print sample command:

```powershell
rerun rrd print "D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260510-070502\build\RRD\phase7_rerunlog_20260510_072240.rrd" | Select-String -Pattern "generated|metrics|logs|world" -CaseSensitive:$false | Select-Object -First 80
```

Observed chunks include:

```text
/world - data columns: [ViewCoordinates:xyz]
/world/generated_cube - data columns: [Transform3D:translation Transform3D:quaternion]
/logs/generated - data columns: [TextLog:text TextLog:level]
/metrics/generated_fps - data columns: [Scalars:scalars]
```

## Non-Blocking Noise

`rerun rrd verify` may print:

```text
[ERROR re_analytics] Failed to initialize analytics: 拒绝访问。 (os error 5)
```

This is Rerun CLI analytics initialization noise and does not affect `.rrd` verification.

The Unity build log may contain early Unity Licensing handshake/update messages, but the build completed successfully and produced the Player executable.

## Acceptance Result

Accepted.

Evidence:

- Editor-generated Phase 7 RRD opens in Rerun Viewer.
- Editor RRD verifies with `1 file verified without error`.
- IL2CPP build succeeded.
- Build preprocess generated physical `.g.cs` fallback.
- Build preprocess generated `RerunLog_link.xml`.
- IL2CPP Player ran and produced a new Phase 7 RRD.
- Player RRD verifies with `1 file verified without error`.
- Player RRD contains `logs/generated`, `metrics/generated_fps`, and `world/generated_cube`.

Conclusion:

Phase 7 `RerunLog` Source Generator, physical `.g.cs` fallback, and IL2CPP/AOT Player path are accepted.

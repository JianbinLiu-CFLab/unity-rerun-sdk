# Scripts

## build_unity_il2cpp.py

Windows IL2CPP Standalone Player build script for Unity2Rerun.

### Prerequisites

- Unity 2022.3+ or Unity 6 with Windows Build Support (IL2CPP)
- Python 3.9+
- Rerun Viewer 0.31.4+ (for smoke verification)

### Usage

```powershell
# Auto-detect Unity from UNITY_EXE / UNITY_PATH / Unity Hub
python Scripts/build_unity_il2cpp.py

# Explicit Unity path
python Scripts/build_unity_il2cpp.py --unity-path "C:/Program Files/Unity/Hub/Editor/2022.3.10f1/Editor/Unity.exe"

# Dry run (print resolved paths without building)
python Scripts/build_unity_il2cpp.py --dry-run

# Optional aliases are also supported for compatibility:
# --unity, --project, --output, --build-dir
```

### Outputs

- Player build: `build/Unity/win64-il2cpp-<timestamp>/WindowsIL2CPP/Unity2RerunDemo.exe`
- Build log: `build/Unity/win64-il2cpp-<timestamp>/Unity2Rerun_IL2CPP_build.log`
- SDK `.rrd` recordings: `build/RRD/` (written by the Player at runtime)

## release/validate_package.py

Release-facing package hygiene checks for the public UPM package and Zenodo snapshot.

### Usage

```powershell
python Scripts/release/validate_package.py
```

### Checks

- package identity, version, license, and declared samples
- required release metadata files and documentation
- Unity sample `.meta` sidecars
- public docs/samples for local paths, private folder references, and TODO-style markers
- sample folders for generated/cache artifacts
- bundled third-party binaries against `THIRD_PARTY_NOTICES.md`

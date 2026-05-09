# IL2CPP Build Guide

## Prerequisites

- Unity 2022.3+ or Unity 6
- Windows Build Support (IL2CPP) installed via Unity Hub
- Cysharp `YetAnotherHttpHandler` (Git dependency, see Prerequisites)
- `Scripts/build_unity_il2cpp.py` (Python 3.9+)

## Build

```powershell
python Scripts/build_unity_il2cpp.py --unity-path "<path-to-Unity.exe>"
```

## Player.log Location

`%USERPROFILE%\AppData\LocalLow\<CompanyName>\<ProductName>\Player.log`

## Common Issues

### asmdef / DLL load error

- Verify all DLLs in `Runtime/Plugins` match `Runtime/Unity.RerunSDK.asmdef` references
- Verify `Runtime/link.xml` preserves all required assemblies

### IL2CPP stripping / missing method

- Ensure `link.xml` from the SDK is present in the consuming project
- If using a custom `link.xml`, merge the SDK's preservation rules

### Protobuf / Arrow missing methods

- Google.Protobuf and Apache.Arrow require reflection support
- Both are preserved in the SDK `link.xml`

### RRD footer verify fails

- `rerun rrd verify` requires a footer manifest not yet written by the SDK
- Use `rerun rrd stats <file>` or open the file directly in Rerun Viewer
- Full footer support is tracked for Phase 9

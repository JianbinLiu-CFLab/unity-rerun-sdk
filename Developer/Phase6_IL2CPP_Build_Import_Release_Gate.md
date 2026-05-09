# Phase 6 IL2CPP Build, Import & Release Gate

## Environment

| Item | Value |
|------|-------|
| Unity Version | (record after build) |
| Rerun Viewer Version | (record) |
| .NET SDK | (record) |
| OS | (record) |

## Baseline Gate

- [ ] Runtime tests: 38/38 passed
- [ ] Phase 1 spike build: passed
- [ ] Phase 4 spike build: passed
- [ ] Whitespace check: clean

## Package Import Gate

- [ ] Clean Unity project: (project path)
- [ ] Package Manager import from disk: no compile errors
- [ ] Samples visible: Basic Rrd Recording, Publisher Components, Live Viewer
- [ ] Publisher Components sample compiles
- [ ] YetAnotherHttpHandler 1.11.5 installed

## Dependency Preservation

- [ ] link.xml covers 11 assemblies (verified)
- [ ] asmdef references match Plugins DLLs (verified)
- [ ] Editor asmdef: Editor-only platform, references Runtime

## Editor Manual Smoke

- [ ] Scene: `Unity2Rerun/Assets/Scenes/SampleScene.unity`
- [ ] FileOnly: `.rrd` contains TextLog, Scalar, Transform3D
- [ ] FileAndLive + Auto Launch: Viewer connected, Stream opened
- [ ] No HTTP/1.1 downgrade
- [ ] Sample `.rrd` saved to `build/RRD/`

## IL2CPP Player Smoke

- [ ] Build command:
  ```powershell
  python Scripts/build_unity_il2cpp.py --unity-path "<path>"
  ```
- [ ] Build log: (path)
- [ ] Player launched, `.rrd` written to `build/RRD/`
- [ ] `rerun rrd stats`: TextLog, Scalar, Transform3D, ViewCoordinates present
- [ ] `rerun rrd print`: data verified
- [ ] Player.log: no asmdef/DLL/protobuf/Arrow/HTTP2 errors

## Conclusion

- [ ] Pass / Fail
- Notes:

# Prerequisites

## Unity

- Unity 6000.0 LTSC or later (developed on 6000.3.14f1 LTSC; compatible with 6000.0.74f1 LTSC)
- Windows (Editor + Standalone Player). macOS/Linux are intended targets but not yet verified.
- IL2CPP Player support: Phase 6+

## Rerun Viewer

- [Rerun 0.31.4+](https://rerun.io)
- Install via `pip install rerun-sdk` or download from [GitHub Releases](https://github.com/rerun-io/rerun/releases)
- Ensure `rerun` (or `rerun.exe`) is on your PATH

## Live Transport (gRPC)

Live transport requires HTTP/2 gRPC support in Unity. Install **Cysharp YetAnotherHttpHandler** and its native dependency package.

Add to `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "Unity NuGet",
      "url": "https://unitynuget-registry.openupm.com",
      "scopes": ["org.nuget"]
    }
  ],
  "dependencies": {
    "com.cysharp.yetanotherhttphandler.dependencies": "https://github.com/Cysharp/YetAnotherHttpHandler.git?path=src/YetAnotherHttpHandler.Dependencies#1.11.5",
    "com.cysharp.yetanotherhttphandler": "https://github.com/Cysharp/YetAnotherHttpHandler.git?path=src/YetAnotherHttpHandler#1.11.5"
  }
}
```

## RRD Compression

Optional `.rrd` LZ4 recording compression uses bundled K4os runtime DLLs in the Unity package. It does not add extra project-level setup for file recording, and it does not compress live gRPC payloads.

## Known Limitations

- `LiveOnly` mode requires a running Viewer; auto-launch is recommended.

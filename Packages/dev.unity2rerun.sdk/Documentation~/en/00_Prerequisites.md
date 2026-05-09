# Prerequisites

## Unity

- Unity 2022 LTS (2022.3+) or Unity 6 (6000.0+)
- Windows (Editor + Standalone Player)
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

## Known Limitations

- `.rrd` files do not yet contain a footer manifest (`rerun rrd verify` will fail).
  Files are still valid and open correctly in Rerun Viewer and `rerun rrd stats/print`.
- `LiveOnly` mode requires a running Viewer; auto-launch is recommended.

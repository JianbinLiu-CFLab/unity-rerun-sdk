# Live Viewer Sample

Requires Rerun Viewer listening on port 9876, or `Auto Launch Viewer` enabled.

## Prerequisites

- Cysharp `YetAnotherHttpHandler` installed with its native dependency package.
  Add to `Packages/manifest.json`:
  ```json
  "com.cysharp.yetanotherhttphandler.dependencies": "https://github.com/Cysharp/YetAnotherHttpHandler.git?path=src/YetAnotherHttpHandler.Dependencies#1.11.5",
  "com.cysharp.yetanotherhttphandler": "https://github.com/Cysharp/YetAnotherHttpHandler.git?path=src/YetAnotherHttpHandler#1.11.5"
  ```
- Rerun Viewer 0.31.4+ installed and on PATH, or `Viewer Executable Path` set.

## Steps

1. Add `RerunManager` to a GameObject.
2. Set `Output Mode` to `File And Live`.
3. Enable `Auto Launch Viewer` or start `rerun` manually.
4. Attach `RerunLiveViewerSample.cs` to the same GameObject.
5. Enter Play Mode.
6. The cube transform, FPS scalar, and frame logs should appear live in the Viewer.

## Troubleshooting

### HTTP/1.1 downgrade

```
Bad gRPC response. Response protocol downgraded to HTTP/1.1.
```

Ensure `YetAnotherHttpHandler` is installed in `Packages/manifest.json`.

### Port already in use

```
gRPC connection failed: 127.0.0.1:9876
```

Stop any existing `rerun` process or change the port in `RerunManager`.

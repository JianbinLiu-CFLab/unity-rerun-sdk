# Output Modes & Live Troubleshooting

## Output Modes

| Mode | `.rrd` file | Live Viewer | Behavior |
|------|------------|-------------|----------|
| FileOnly | Yes | No | Default. Saves to disk only. |
| LiveOnly | No | Required | Live stream only. Fails if no Viewer is running. |
| FileAndLive | Yes | Optional | Both file and live. Live failure does not affect file output. |

## Live Transport Setup

1. Install `YetAnotherHttpHandler` (see Prerequisites).
2. Set `Output Mode` to `File And Live`.
3. Enable `Auto Launch Viewer` or start `rerun` manually.
4. Verify Console shows: `Using Cysharp YetAnotherHttpHandler for HTTP/2 live gRPC`.

## Transport Health

During Play Mode, `RerunManager` exposes a read-only Transport Health section in the Inspector:

- `Live State`
- `Supported`
- `Running`
- `Queue Depth`
- `Dropped`
- `Reconnects`
- `Sent StoreInfo`
- `Sent Data`
- `Last Error`

The same data is available from `RerunManager.GetTransportStatsSnapshot()`. These counters are diagnostic only; they do not change the file-first behavior of `FileAndLive`.

## RRD Verification

`.rrd` files written by the SDK include RRD footer/manifests by default. For release checks or bug reports, run:

```powershell
rerun rrd verify path/to/file.rrd
rerun rrd stats path/to/file.rrd
```

`verify` should exit successfully. `stats` should list the expected entity paths and components.

## Common Errors

### Bad gRPC response. Response protocol downgraded to HTTP/1.1.

**Cause:** Unity's built-in HTTP stack does not support HTTP/2.

**Fix:** Install `YetAnotherHttpHandler` in `Packages/manifest.json`.

### Cysharp YetAnotherHttpHandler loaded, but Http2Only property was not found.

**Cause:** Version/API mismatch between the SDK and `YetAnotherHttpHandler`, or stale compiled assemblies after changing the package.

**Fix:** Use the validated `1.11.5` Git dependencies from Prerequisites, then let Unity recompile. If the warning persists, restart the Editor.

### Cancelled / No grpc-status found (on shutdown)

**Cause:** gRPC stream closed during Unity shutdown or stop. Expected behavior.

**Action:** No action needed. This is not an error.

### rerun rrd verify fails

**Cause:** The file may be incomplete, still being written, or produced by an older SDK revision before Phase 9 footer support.

**Action:** Stop recording cleanly, then rerun `rerun rrd verify <file>`. For old no-footer files, open them directly in Rerun Viewer or regenerate them with the current SDK.

## Player (IL2CPP) Troubleshooting

### DLL load error at Player startup

**Cause:** Missing assembly references in `link.xml` or incompatible plugin platform settings.

**Fix:** Verify `Runtime/link.xml` covers all 11 assemblies. See IL2CPP Build Guide.

### Protobuf / Arrow missing method in Player

**Cause:** IL2CPP stripping removed reflection-dependent types.

**Fix:** Ensure the SDK's `link.xml` is present in the consuming project's root or merged into the project-level `link.xml`.

### Live HTTP/2 handler not found in Player

**Cause:** `YetAnotherHttpHandler` is a project-level Git dependency, not bundled in the SDK.

**Fix:** Install `YetAnotherHttpHandler` in the consuming project's `Packages/manifest.json`. See Prerequisites.

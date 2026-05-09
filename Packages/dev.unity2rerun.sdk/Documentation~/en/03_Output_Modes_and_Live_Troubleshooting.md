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

### rerun rrd verify fails with missing footer/manifests

**Cause:** The SDK does not yet write full RRD footer manifests. This is a known limitation tracked for a future phase.

**Action:** Use `rerun rrd stats <file>` or open the file directly in Rerun Viewer. Both work correctly.

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

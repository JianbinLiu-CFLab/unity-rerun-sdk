---
title: Phase 4 gRPC Gate Findings
status: done
updated: 2026-05-09
---

# Phase 4 gRPC Gate Findings

## Verdict: PASSED

Managed gRPC route is viable for Unity2Rerun live transport. No `Grpc.Core` native plugin or external bridge process needed.

## Environment

- Rerun Viewer: 0.31.4 (pip-installed via conda Python 3.13.9)
- .NET SDK: 10.0.203
- OS: Windows 10 IoT Enterprise LTSC 2021

## Dependency Closure

Full transitive closure of `Grpc.Net.Client 2.76.0` (all DLLs in `Runtime/Plugins`):

| DLL | Source Package | Size (netstandard2.0) |
|-----|---------------|------------------------|
| Grpc.Net.Client.dll | Grpc.Net.Client 2.76.0 | 222 KB |
| Grpc.Core.Api.dll | Grpc.Core.Api 2.76.0 | 70 KB |
| Grpc.Net.Common.dll | Grpc.Net.Common 2.76.0 | 24 KB |
| System.Threading.Channels.dll | System.Threading.Channels 8.0.0 | 74 KB |
| Microsoft.Extensions.Logging.Abstractions.dll | 8.0.0 (transitive) | 68 KB |
| Microsoft.Extensions.DependencyInjection.Abstractions.dll | 8.0.0 (transitive) | 64 KB |
| Microsoft.Bcl.AsyncInterfaces.dll | 8.0.0 (transitive) | 27 KB |
| System.Diagnostics.DiagnosticSource.dll | 6.0.0 (transitive) | 153 KB |
| Google.Protobuf.dll (reuse) | 3.28.3 | 484 KB |
| Apache.Arrow.dll (reuse) | 19.0.0 | 350 KB |

**New DLLs total: ~702 KB. UPM Plugins total: ~1.6 MB.**

Key transitive dependencies verified via `dotnet list package --include-transitive`:

```
Grpc.Net.Client 2.76.0
├── Grpc.Net.Common 2.76.0
│   ├── Grpc.Core.Api 2.76.0
│   │   └── System.Threading.Channels 8.0.0
│   ├── Microsoft.Extensions.Logging.Abstractions 8.0.0
│   │   └── Microsoft.Extensions.DependencyInjection.Abstractions 8.0.0
│   ├── System.Diagnostics.DiagnosticSource 6.0.0
├── Microsoft.Bcl.AsyncInterfaces 8.0.0
```

`Grpc.Tools 2.67.0` used as codegen tool only — not shipped in UPM runtime package.

## UPM Integration Status

- [`asmdef`](Runtime/Unity.RerunSDK.asmdef) references all 8 DLLs
- [`link.xml`](Runtime/link.xml) preserves all 8 assemblies for IL2CPP
- **Unity Editor import / Play Mode not yet verified** — requires a Unity project with UPM local path import. This is tracked as the first task in Phase 4 E section (live transport integration).

## Test Results

### Plaintext HTTP/2 Connectivity

`GrpcChannel.ForAddress("http://127.0.0.1:9876", GrpcChannelOptions { Credentials = Insecure })` — success.

### WriteMessages Client-Stream

`MessageProxyServiceClient.WriteMessages()` sent SetStoreInfo + ArrowMsg through client-stream; `CompleteAsync()` + `await call` returned `WriteMessagesResponse { }` without error.

### Verification Command

```powershell
# Terminal 1: start Viewer
python -m rerun

# Terminal 2: run spike
dotnet run --project Spikes/Phase4.GrpcTransport
```

## Risks & Next Steps

1. **Unity Editor compatibility**: DLLs are `netstandard2.0` — expected compatible with Unity 6000.0. First E-section task validates this.
2. **IL2CPP preservation**: `link.xml` covers all 8 assemblies. IL2CPP Player build validation planned for Phase 4 completion.
3. **Google.Protobuf version drift**: `3.28.3` is shared with Phase 3 — no duplication.

## Spike Code

`Spikes/Phase4.GrpcTransport/Program.cs`

# Third-Party Notices

This Unity package uses third-party open source software and public specifications.

---

## Rerun

- URL: https://github.com/rerun-io/rerun
- License: Apache-2.0 / MIT dual license
- Usage: RRD framing, protobuf schema, Arrow/Sorbet metadata conventions, archetype/component naming, and Rerun Viewer / CLI compatibility behavior are referenced from the official Rerun project. Unity2Rerun is an independent C# implementation.

## Apache Arrow

- URL: https://github.com/apache/arrow
- License: Apache-2.0
- Usage: Arrow IPC serialization and runtime Arrow array/schema types.

## Google.Protobuf

- URL: https://github.com/protocolbuffers/protobuf
- License: BSD-3-Clause
- Usage: protobuf message encoding/decoding for Rerun transport messages and generated protocol types.

## grpc-dotnet

- URL: https://github.com/grpc/grpc-dotnet
- License: Apache-2.0
- Usage: managed gRPC live transport to Rerun Viewer.

## K4os LZ4

- URL: https://github.com/MiloszKrajewski/K4os.Compression.LZ4
- License: MIT
- Usage: optional raw LZ4 block compression for `.rrd` file Arrow payloads:
  - `K4os.Compression.LZ4.dll`
  - `K4os.Hash.xxHash.dll`

## Microsoft .NET support libraries

- URL: https://dot.net/
- License: MIT
- Usage: bundled managed support assemblies required by gRPC and async runtime behavior in Unity:
  - `Microsoft.Bcl.AsyncInterfaces.dll`
  - `Microsoft.Extensions.DependencyInjection.Abstractions.dll`
  - `Microsoft.Extensions.Logging.Abstractions.dll`
  - `System.Diagnostics.DiagnosticSource.dll`
  - `System.Threading.Channels.dll`

## Cysharp YetAnotherHttpHandler

- URL: https://github.com/Cysharp/YetAnotherHttpHandler
- License: MIT
- Usage: Unity-compatible HTTP/2 handler for live gRPC transport. This dependency is installed at the consuming project level and is not bundled into the package.

## Unity

- URL: https://unity.com
- License: Unity terms
- Usage: Unity Editor/Runtime APIs used by the package integration layer and samples.

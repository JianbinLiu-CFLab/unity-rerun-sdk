# Architecture

## Package Structure

```
Runtime/
  Core/         IRerunBackend, RerunRuntime, RerunEntityPath, RerunTimeline,
                RerunOutputMode, RerunLiveState
  Encoding/     ManagedRerunEncoder, RerunArrowIpcEncoder, RerunProtobufEncoding,
                RerunTuidGenerator, EncodedRerunMessage
  IO/Rrd/       RrdWriter, RrdRerunBackend
  Transport/    CompositeRerunBackend
  Transport/Grpc/  RerunGrpcClient, RerunGrpcEndpoint, RerunGrpcViewerProbe,
                GrpcRerunBackend, Protos/
  Unity/        RerunManager, RerunCoordinateConverter, RerunViewerLauncher,
                RerunPublisherBase, RerunTransformPublisher,
                RerunScalarPublisher, RerunTextLogPublisher
  Plugins/      Apache.Arrow.dll, Google.Protobuf.dll, Grpc.Net.Client.dll, ...
Editor/
  RerunManagerEditor, RerunPublisherBaseEditor, RerunScalarPublisherEditor
```

## Data Flow

```
RerunManager (or Publisher)
  ↓ LogText/LogScalar/LogTransform
ManagedRerunEncoder
  ↓ EncodeXxxMessage() → EncodedRerunMessage
Backend
  ├── RrdRerunBackend → RrdWriter → .rrd file
  └── GrpcRerunBackend → RerunGrpcClient → Rerun Viewer (gRPC)
```

## Key Design Decisions

- **Pure C# encoding** — no native plugins or external bridge processes
- **`.rrd` first** — file output is the primary and most reliable path
- **Shared `EncodedRerunMessage`** — both file and live backends consume the same message type
- **Live fault isolation** — gRPC failures never affect `.rrd` file output
- **Publisher components** — Inspector-driven, no-code recording on top of Manager API

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
  Components/   RerunManager, RerunCoordinateConverter, RerunViewerLauncher,
                RerunPublisherBase, RerunTransformPublisher,
                RerunScalarPublisher, RerunTextLogPublisher
  Utilities/    Utility helpers shared by runtime components
  Plugins/      Apache.Arrow.dll, Google.Protobuf.dll, Grpc.Net.Client.dll, ...
Editor/
  RerunManagerEditor, RerunPublisherBaseEditor, RerunScalarPublisherEditor
```

## Data Flow

```
RerunManager (or Publisher)
  -> LogText/LogScalar/LogTransform
ManagedRerunEncoder
  -> EncodeXxxMessage() -> EncodedRerunMessage
Backend
  +-- RrdRerunBackend -> RrdWriter -> .rrd file
  +-- GrpcRerunBackend -> RerunGrpcClient -> Rerun Viewer (gRPC)
```

## Key Design Decisions

- **Pure C# encoding** - no native plugins or external bridge processes
- **`.rrd` first** - file output is the primary and most reliable path
- **Shared `EncodedRerunMessage`** - both file and live backends consume the same message type
- **Live fault isolation** - gRPC failures never affect `.rrd` file output
- **Publisher components** - Inspector-driven, no-code recording on top of Manager API

## Rerun Type Coverage

Unity2Rerun intentionally implements a curated Rerun-native runtime subset instead of mirroring every official Rerun schema at once. The current encoder surface covers text logs, scalars, transforms, coordinate conventions, encoded camera images, pinhole camera metadata, 3D boxes, 3D line strips, and 3D points.

Coverage is tracked in `docs/releases/RERUN_TYPE_COVERAGE_MATRIX.md` and validated by `Scripts/release/check_rerun_type_coverage.py`. The matrix compares Unity2Rerun against official runtime `archetypes/` and `components/` definitions while excluding Rerun viewer blueprint, shared datatype, and testing schemas from the runtime denominator.

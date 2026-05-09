# 架构说明

## 包结构

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

## 数据流

```
RerunManager (或 Publisher)
  ↓ LogText/LogScalar/LogTransform
ManagedRerunEncoder
  ↓ EncodeXxxMessage() → EncodedRerunMessage
Backend
  ├── RrdRerunBackend → RrdWriter → .rrd 文件
  └── GrpcRerunBackend → RerunGrpcClient → Rerun Viewer (gRPC)
```

## 关键设计决策

- **纯 C# 编码** — 无原生插件或外部桥接进程
- **`.rrd` 优先** — 文件输出是最主要、最可靠的路径
- **共享 `EncodedRerunMessage`** — 文件和实时后端消费同一种消息类型
- **实时故障隔离** — gRPC 故障不影响 `.rrd` 文件输出
- **Publisher 组件** — 在 Manager API 之上提供的 Inspector 驱动、无需代码的录制方式

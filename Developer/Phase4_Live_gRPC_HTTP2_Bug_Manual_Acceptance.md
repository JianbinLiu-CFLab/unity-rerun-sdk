---
title: Phase 4 Live gRPC HTTP2 Bug与人工验收记录
date: 2026-05-09
tags:
  - Unity2Rerun
  - Phase4
  - gRPC
  - validation
  - bug
status: accepted
related:
  - "[[04_Phase4_gRPC_Live_Transport_Viewer_Launch_PLAN]]"
  - "[[05_Phase5_Unity_Publishers_User_Documentation_PLAN]]"
---

# Phase 4 Live gRPC HTTP/2 Bug与人工验收记录

> [!success] 验收结论
> Phase 4 的 `FileOnly`、`FileAndLive`、Viewer auto launch、Cysharp HTTP/2 live gRPC 与 shutdown 假 warning 处理，在 2026-05-09 的人工验收中通过。后续仍需单独处理 `.rrd` footer / manifest，让 `rerun rrd verify` 通过。

## 背景

Phase 4 目标是把 Phase 3 已验证的 `.rrd` 编码路径扩展为 live gRPC：

- `.rrd` backend 继续写 RRD stream 内层 `SetStoreInfo` / `ArrowMsg` payload。
- gRPC backend 写外层 `LogMsg` oneof，再通过 `MessageProxyService.WriteMessages` client-stream 发送到 Rerun Viewer。
- `FileOnly`、`LiveOnly`、`FileAndLive` 三种输出模式需要共用同一套 `EncodedRerunMessage`。

本记录覆盖 Phase 4 后段人工验收中发现的 live gRPC 问题、修复过程和最终验收证据。

## 问题 1：Unity 中 live gRPC 退回 HTTP/1.1

### 现象

Unity Console 中出现：

```text
[RerunGrpcClient] Stream ended: PlatformNotSupportedException: gRPC requires extra configuration on .NET implementations that don't support gRPC over HTTP/2. An HTTP provider must be specified using GrpcChannelOptions.HttpHandler.
```

后续换成 `HttpClientHandler` 后，仍出现：

```text
[RerunGrpcClient] Stream ended: RpcException: Status(StatusCode="Internal", Detail="Bad gRPC response. Response protocol downgraded to HTTP/1.1."), StatusCode=Internal, Detail=Bad gRPC response. Response protocol downgraded to HTTP/1.1., reconnecting in 1000ms
```

同时 Unity 侧会看到：

```text
[RerunGrpcClient] WriteMessages stream opened to http://127.0.0.1:9876/proxy
[RerunGrpcClient] StoreInfo sent to live stream
[RerunGrpcClient] Data message sent to live stream
```

但 Rerun Viewer 中看不到 Unity 数据。

### 判断

这不是 Rerun Viewer 或 SDK protobuf 内容的问题。验证依据：

- 本地 Phase 4 spike 使用 `Grpc.Net.Client 2.76.0` 可向同一 Viewer 发送 `SetStoreInfo + ArrowMsg`。
- 手动运行 `rerun --connect rerun+http://127.0.0.1:9876/proxy` 可看到 spike 数据。
- Rerun CLI 确认 Viewer 端口正常监听：

```text
Listening for gRPC connections on 0.0.0.0:9876.
Connect by running `rerun --connect rerun+http://127.0.0.1:9876/proxy`
```

根因在 Unity runtime 的 HTTP provider：Unity 默认 `HttpClientHandler` 没有为 `Grpc.Net.Client` 提供可用的明文 HTTP/2 transport，导致实际响应退回 HTTP/1.1。

### 修复

引入 Cysharp `YetAnotherHttpHandler` 作为 Unity 下的 HTTP/2 provider：

- Unity 验证工程 `Unity2Rerun/Packages/manifest.json` 加入：
  - `com.cysharp.yetanotherhttphandler`
  - `com.cysharp.yetanotherhttphandler.dependencies`
  - UnityNuGet scoped registry，用于 NuGet 依赖包。
- `RerunGrpcClient.CreateHttpHandler()` 在 Unity 下优先通过反射查找 `Cysharp.Net.Http.YetAnotherHttpHandler`。
- 找到后设置 `Http2Only=true`，让 h2c / HTTP/2 路径明确接管 live gRPC。
- 找不到时 fallback 到 `HttpClientHandler`，但输出 warning，提示 Unity live gRPC 可能退回 HTTP/1.1。

相关文件：

- `Packages/dev.unity2rerun.sdk/Runtime/Transport/Grpc/RerunGrpcClient.cs`
- `Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/TransportTests.cs`
- `Unity2Rerun/Packages/manifest.json`

## 问题 2：`Http2Only` 属性类型判断错误

### 现象

装入 YetAnotherHttpHandler 后，Unity Console 出现：

```text
[RerunGrpcClient] Cysharp YetAnotherHttpHandler loaded, but Http2Only property was not found.
```

随后仍出现 HTTP/1.1 downgrade：

```text
Bad gRPC response. Response protocol downgraded to HTTP/1.1.
```

### 根因

SDK 反射代码只接受：

```csharp
Http2Only.PropertyType == typeof(bool)
```

但真实 `YetAnotherHttpHandler 1.11.5` 中的 API 是：

```csharp
public bool? Http2Only { get; set; }
```

属性存在，但被过窄的类型检查过滤掉了。

### 修复

`TryCreateCysharpHttp2Handler()` 同时接受：

- `bool`
- `bool?`

并补充测试模拟真实 `bool? Http2Only` API。

期望 Unity Console 出现：

```text
[RerunGrpcClient] Using Cysharp YetAnotherHttpHandler for HTTP/2 live gRPC
```

## 问题 3：关闭 Play 时出现 shutdown 假 warning

### 现象

停止 Unity Play 后出现：

```text
[RerunGrpcClient] Stream ended during shutdown: RpcException: Status(StatusCode="Cancelled", Detail="No grpc-status found on response."), StatusCode=Cancelled, Detail=No grpc-status found on response.
```

### 判断

这发生在 shutdown 路径上：

1. `RerunGrpcClient` 已经调用 `CompleteAsync()` 关闭 request stream。
2. 客户端等待 `ResponseAsync`。
3. YetAnotherHttpHandler / Rerun 在关闭阶段返回 `StatusCode.Cancelled` 且没有 `grpc-status`。

此时数据已经发送完成，`.rrd` 文件也能读取出完整 chunk。这不是数据丢失或 live 链路失败。

### 修复

新增 shutdown 异常分类：

- `_stopRequested=true` 时，`RpcException(StatusCode.Cancelled)` 作为预期关闭处理。
- 输出 info：

```text
[RerunGrpcClient] Live stream closed during shutdown (RpcException)
```

- 真正的 `Internal` / HTTP downgrade 仍保留 warning。

## 人工验收过程

### 1. Unity 包导入与编译

验收工程：`Unity2Rerun/`

检查项：

- Package Manager 中 `Unity2Rerun SDK` 可见。
- Sample 可导入。
- `YetAnotherHttpHandler` 与 `YetAnotherHttpHandler (Dependencies)` 可见。
- `Unity NuGet` scoped registry 包有黄色 unsigned warning，但没有 compile error。
- Console 中无 SDK 红色编译错误。

期间修复过的 Unity 编译 / warning 类问题：

- `Debug` 在 `UnityEngine.Debug` 与 `System.Diagnostics.Debug` 间歧义。
- nullable annotation 警告。
- `RerunProtobufEncoding` sign-extended bitwise warning。
- `RerunViewerLauncher` 非 nullable field warning。

### 2. FileOnly 验收

配置：

- `Output Mode = FileOnly`
- 输出路径改为项目内相对路径，落到 `build/RRD/`。

验证点：

- Unity Console 输出 recording started / stopped。
- Rerun Viewer 能打开 `.rrd`。
- Viewer 中能看到：
  - `logs/unity`
  - `metrics/fps`
  - `world/cube`
- Rerun Viewer / PowerShell 无明显 warning / error。
- 不出现重复 StoreInfo warning。

结果：通过。

### 3. FileAndLive + Auto Launch 验收

配置：

- `Output Mode = FileAndLive`
- `Auto Launch Viewer = true`
- endpoint：`rerun+http://127.0.0.1:9876/proxy`
- Rerun executable：自动解析到 `C:\Users\LJB\anaconda3\Scripts\rerun.exe`

预期日志：

```text
[Rerun] Launched Viewer: C:\Users\LJB\anaconda3\Scripts\rerun.exe --port 9876 --expect-data-soon
[Rerun] Rerun Viewer confirmed on http://127.0.0.1:9876/proxy
[RerunGrpcClient] Starting live stream loop to http://127.0.0.1:9876/proxy
[RerunGrpcClient] Using Cysharp YetAnotherHttpHandler for HTTP/2 live gRPC
[RerunGrpcClient] WriteMessages stream opened to http://127.0.0.1:9876/proxy
[RerunGrpcClient] StoreInfo sent to live stream
[RerunGrpcClient] Data message sent to live stream
```

结果：

- Viewer 能自动启动。
- live stream 能打开。
- StoreInfo 和 data message 能发送。
- 停止 Play 时不再把 `Cancelled / No grpc-status found` 当作 warning。

结果：通过。

### 4. 手动 Viewer 连接对照

手动运行：

```powershell
rerun --connect rerun+http://127.0.0.1:9876/proxy
```

对照结果：

- Phase 4 spike 可被 Viewer 显示，说明 Rerun server / Viewer display 链路本身正常。
- Unity live 的问题集中在 Unity HTTP/2 handler，而不是 Rerun server。

## 自动验证

### 单元测试

命令：

```powershell
dotnet test Packages/dev.unity2rerun.sdk/Tests/Runtime/Unity.RerunSDK.Core.Tests/Unity.RerunSDK.Core.Tests.csproj --no-restore
```

结果：

```text
34/34 passed
```

新增或关键覆盖：

- Cysharp handler 已加载时被优先选用。
- `Http2Only` 支持 `bool?`。
- shutdown `Cancelled / No grpc-status found` 被识别为预期关闭。
- `Internal / HTTP/1.1 downgrade` 仍不被吞掉。

### Phase 4 spike build

命令：

```powershell
dotnet build Spikes/Phase4.GrpcTransport/Phase4.GrpcTransport.csproj --no-restore
```

结果：

```text
Build succeeded
```

已知 warning：

```text
NU1510: PackageReference System.Threading.Channels is likely unnecessary
```

### diff 检查

命令：

```powershell
git diff --check
```

结果：通过。

## RRD 验收样本

样本文件：

```text
build/RRD/unity_recording_20260509_120411.rrd
```

文件信息：

- 大小：`2,675,909 bytes`
- 写入时间：`2026-05-09 12:04:11 -> 12:04:38`

`rerun rrd stats` 结果摘录：

```text
num_chunks = 1,036
num_entity_paths = 4
num_rows = 1,036
num_static = 1

/logs/unity: 21
/metrics/fps: 70
/world: 1
/world/cube: 944

Scalars:scalars: 70
TextLog:level: 21
TextLog:text: 21
Transform3D:quaternion: 944
Transform3D:translation: 944
ViewCoordinates:xyz: 1
```

`rerun rrd print` 统计：

```text
StoreInfo=1
Chunks=1036
```

判断：

- 没有重复 StoreInfo。
- TextLog、Scalars、Transform3D、ViewCoordinates 都进入文件。
- 文件可被 Rerun CLI 读取和 Viewer 打开。

## 已知技术债

> [!warning] RRD footer / manifest 尚未补齐
> 当前 `.rrd` 使用 no-footer 策略，Viewer 和 `rerun rrd stats/print` 能读取，但 `rerun rrd verify` 会报：

```text
Missing RRD footer / no RRD manifests
```

建议后续单独开 follow-up：

- 实现最小 RRD footer / manifests。
- 或按 Rerun 当前格式实现可被 `rerun rrd verify` 接受的 footer。
- 把 `rerun rrd verify` 加入自动验收矩阵。

## 后续建议

- Phase 5 开始前，把本记录作为 baseline gate 的人工验收证据。
- `FileAndLive` sample 文档中明确 Unity live gRPC 依赖 YetAnotherHttpHandler。
- `Troubleshooting` 中加入：
  - `Bad gRPC response. Response protocol downgraded to HTTP/1.1.`
  - `Cysharp YetAnotherHttpHandler loaded, but Http2Only property was not found.`
  - shutdown `Cancelled / No grpc-status found` 的解释。
- 提交时保留 Unity `.meta` 文件，尤其是 `Packages/dev.unity2rerun.sdk` 下的 asmdef、dll、script、folder `.meta`。

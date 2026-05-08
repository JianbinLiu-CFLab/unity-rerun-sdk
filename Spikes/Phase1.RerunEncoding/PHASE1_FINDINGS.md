---
title: Phase 1 Rerun Encoding Spike 结论
aliases:
  - PHASE1_FINDINGS
tags:
  - phase1
  - rerun
  - encoding
  - findings
status: done
updated: 2026-05-08
---

# Phase 1 Findings

> [!summary]
> 纯 C# Rerun encoding spike 已完成。可以生成被 Rerun Viewer 0.31.4 成功打开的最小 `.rrd` 文件，包含一条 TextLog。

## 验收结果

- [x] 能生成 `min_text_log.rrd`，被 Rerun Viewer / CLI 打开且无错误
- [x] Viewer 加载日志无 "Failed to load" 错误
- [x] 明确了 protobuf、Arrow IPC、`.rrd` framing 的技术边界

## 关键技术决策

### Protobuf 方案

**选择：从本地 `third-party/rerun` proto 文件生成 C# 类型，通过 Grpc.Tools + Google.Protobuf NuGet。**

关键注意点：
- RRD stream 中直接写 `re_protos::log_msg::v1alpha1::log_msg::Msg` 的内部消息（SetStoreInfo / ArrowMsg），**不使用**外层 `LogMsg` oneof wrapper。这是与 Rust encoder 行为一致的关键发现。
- `Tuid.time_ns` 的 `optional` 字段在 protobuf 中正常工作，wire type 是 fixed64 (=1)
- 生成代码在 .NET 10 下编译和序列化均正常
- Google.Protobuf 3.28.3 兼容性良好

### Arrow IPC 方案

**选择：使用 Apache.Arrow NuGet (19.0.0) 构建 RecordBatch 并通过 ArrowStreamWriter 生成 IPC 格式。**

关键注意点：
- **`row_id` 列必须设置 Arrow extension metadata**：`ARROW:extension:name=rerun.datatypes.TUID` 和 `ARROW:extension:metadata={"namespace":"row"}`
- **Tuid 字节序是 big-endian**（不是 little-endian）：`[time_ns: 8 bytes BE][inc: 8 bytes BE]`
- **`row_id` 列必须设置 `rerun:kind=control`**，否则 Sorbet 将其识别为 data 列
- 列顺序必须：row_id → index columns → data columns
- 需要在 RecordBatch 上设置 Sorbet metadata：`sorbet:version`、`rerun:id`、`rerun:entity_path`
- Apache.Arrow 19.0.0 的 NuGet 包在 .NET 中可用，但进入 Unity 的兼容性有待验证

### `.rrd` Stream / Framing

**结构已确认：**

1. **StreamHeader** (12 bytes): `RRF2` + version `[major, minor, patch, meta]` + encoding options `[compression, serializer, 0, 0]`
2. **Message** = MessageHeader (16 bytes: kind u64 + len u64) + protobuf payload
3. **End Message**: MessageHeader(kind=0) + RrdFooter protobuf + StreamFooter (32 bytes)

版本编码：`CrateVersion::to_bytes()` 返回 `[major, minor, patch, meta]` 直接 4 字节。

StreamFooter 结构：
- 1 entry (20 bytes): offset(8 LE) + len(8 LE) + crc32(4 LE, xxhash seed=7850921)
- Fixed part (12 bytes): `RRF2` + `FOOT` + num_entries(4 LE)

### No-Footer 模式

当前 Phase 1 使用 no-footer 模式：End message 中有空的 `RrdFooter`（没有 manifests）。
这是有效的 RRD，可以被 Viewer 打开，但缺少 manifest 会影响随机访问性能。

## 已知问题和后续

### Phase 2 前需要解决

1. **Apache.Arrow 库在 Unity 中的兼容性**：当前 NuGet 包是为 .NET 设计的，需要验证：
   - 是否可以在 Unity 中通过 asmdef 引用
   - AOT / IL2CPP 兼容性
   - 体积是否可接受（Arrow 库本身较大）
   
2. **Footer 完整支持**：Phase 2 应该实现完整的 footer (RrdFooter + manifest)

3. **组件列需要 List 类型**：当前 TextLog 的 text/level 列使用 StringType，但 Rerun 期望 component 列是 List<Utf8>。Viewer 能打开说明它对非 List 类型有容错，但后续需要修正为标准格式。

### 移植到 UPM 的建议

可以搬入 UPM 的代码：
- `RrdWriter.cs` 的 framing 逻辑
- Protobuf 生成方式（从 proto 生成 C# 类型）
- Arrow IPC writer 的核心模式

需要留在 spike 的代码：
- 硬编码的 TextLog schema builder → 需要在 UPM 中泛化

## 技术参考

- proto 文件：`third-party/rerun/crates/store/re_protos/proto/rerun/v1alpha1/`
- RRD framing：`third-party/rerun/crates/store/re_log_encoding/src/rrd/frames.rs`
- RRD encoder：`third-party/rerun/crates/store/re_log_encoding/src/rrd/encoder.rs`
- Sorbet schema：`third-party/rerun/crates/store/re_sorbet/src/`
- Tuid 定义：`third-party/rerun/crates/utils/re_tuid/src/lib.rs`
- TextLog archetype：`third-party/rerun/crates/store/re_sdk_types/definitions/rerun/archetypes/text_log.fbs`

## 文件清单

```
Spikes/Phase1.RerunEncoding/
├── Program.cs          # 主程序：构建 TextLog .rrd
├── RrdWriter.cs        # RRD stream writer + XxHash32
├── Phase1.RerunEncoding.csproj
├── Protos/             # 复制的 rerun proto 定义
│   └── rerun/v1alpha1/
│       ├── common.proto
│       └── log_msg.proto
└── out/
    ├── min_text_log.rrd      # 生成的文件 (2241 bytes)
    ├── oracle_text_log.rrd   # Python SDK oracle (7596 bytes)
    └── oracle_v2.rrd         # Python SDK oracle v2 (7531 bytes)
```

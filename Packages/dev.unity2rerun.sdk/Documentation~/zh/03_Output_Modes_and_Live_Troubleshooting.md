# 输出模式与实时传输故障排除

## 输出模式

| 模式 | `.rrd` 文件 | 实时 Viewer | 行为 |
|------|------------|------------|------|
| FileOnly | 是 | 否 | 默认。仅保存到磁盘 |
| LiveOnly | 否 | 必需 | 仅实时流。如果无 Viewer 运行则失败 |
| FileAndLive | 是 | 可选 | 同时文件和实时。实时失败不影响文件输出 |

## 实时传输设置

1. 安装 `YetAnotherHttpHandler`（见前置条件）
2. 将 `Output Mode` 设为 `File And Live`
3. 启用 `Auto Launch Viewer` 或手动启动 `rerun`
4. 确认 Console 显示：`Using Cysharp YetAnotherHttpHandler for HTTP/2 live gRPC`

## 常见错误

### Bad gRPC response. Response protocol downgraded to HTTP/1.1.

**原因：** Unity 内置的 HTTP 栈不支持 HTTP/2。

**修复：** 在 `Packages/manifest.json` 中安装 `YetAnotherHttpHandler`。

### Cysharp YetAnotherHttpHandler loaded, but Http2Only property was not found.

**原因：** SDK 与 `YetAnotherHttpHandler` 之间存在版本/API 不匹配，或切换包后 Unity 仍在使用旧编译缓存。

**修复：** 使用前置条件中验证过的 `1.11.5` Git 依赖，然后等待 Unity 重新编译。如果 warning 仍存在，重启 Editor。

### Cancelled / No grpc-status found（关闭时出现）

**原因：** gRPC 流在 Unity 关闭或停止时中断。这是预期行为。

**操作：** 不需要处理。这不是错误。

### rerun rrd verify 报 missing footer/manifests 失败

**原因：** SDK 尚未写入完整的 RRD footer manifest。这是一个已知限制，将在后续阶段处理。

**操作：** 使用 `rerun rrd stats <file>` 或直接在 Rerun Viewer 中打开文件，两者均正常工作。

## Player (IL2CPP) 故障排除

### Player 启动时 DLL 加载错误

**原因：** `link.xml` 缺少 assembly 引用或 plugin 平台设置不兼容。

**修复：** 确认 `Runtime/link.xml` 覆盖了全部 11 个 assembly。见 IL2CPP 构建指南。

### Player 中 Protobuf / Arrow 缺少方法

**原因：** IL2CPP stripping 移除了反射依赖类型。

**修复：** 确保消费项目根目录中存在 SDK 的 `link.xml`，或将其合并到项目级 `link.xml` 中。

### Player 中找不到 live HTTP/2 handler

**原因：** `YetAnotherHttpHandler` 是项目级 Git 依赖，不捆绑在 SDK 中。

**修复：** 在消费项目的 `Packages/manifest.json` 中安装 `YetAnotherHttpHandler`。见前置条件。

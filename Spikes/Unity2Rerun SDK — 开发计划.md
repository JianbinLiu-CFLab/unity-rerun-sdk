
## 1. 目的

开发一个 **Unity 原生 SDK**，实现在没有外部桥接进程的情况下，将运行时数据
（变换、点云、图像、标量、文本日志）从 Unity 引擎实时流式传输到
**Rerun** 可视化平台。

该 SDK 应让 Unity 开发者感觉像 `Unity2Foxglove` 中的 `[FoxRun]`
一样自然：最少量的样板代码、无外部依赖、兼容 Editor Play Mode 以及
IL2CPP 独立构建。

## 2. 可以从 Unity2Foxglove 复用的部分

现有代码库在多个层面提供了坚实的基础：

### 2.1 核心传输与线程
- `Unity2Foxglove` 中的进程内 **WebSocket 服务器**（`WsServer`、
  `WsConnection`）几乎可以原样复用。Rerun 支持我们可以直接对接的
  WebSocket 桥接。
- 异步发送队列、客户端跟踪以及广播基础设施已经过调试，并且是
  IL2CPP-safe 的。

### 2.2 基于 Source Generator 的零反射发布
- `[FoxRun]` 特性与其 **Roslyn Source Generator** 是经过验证的模式。
  我们可以复刻相同的流水线：
  - 将 `[FoxRun]` 替换为 `[RerunLog]`（或类似名称）。
  - 修改共享 emitter（`FoxgloveSourceEmitter`）以输出 Rerun 兼容的
    数据类型。
  - 保留双路径策略（Editor ISG + Build 回退）以及 Phase 31A 中实现的
    自动化 IL2CPP link.xml 生成。

### 2.3 MCAP 读写
- Rerun 原生支持 **MCAP** 文件。我们现有的 `McapWriter`、
  `McapReader` 以及压缩支持（LZ4/Zstd）经过适配后，可以写入同时兼容
  Foxglove 与 Rerun 的录制文件。
- 回放引擎（`McapReplayEngine`）可以扩展，以在 Unity 内部提供
  “录制查看器”，或从 Rerun 的 `.rrd` 及 MCAP 日志中驱动场景重建。

### 2.4 项目结构与构建流水线
- Package 布局（`Runtime`、`Editor`、`Tests`、`Samples~/...`）、
  `.asmdef` 配置以及 IL2CPP 构建加固都可直接移植。
- 相同的 CI 配置（`docs-check`、`test-runner`）和 `dotnet run`
  测试套件也可以复制使用。

### 2.5 文档与社区模式
- 多语言文档结构（`Documentation~/en/`、`zh/`）、示例项目以及
  道德性 AI 声明均可作为新仓库的模板。

## 3. 关键差异：Foxglove 与 Rerun

| 方面       | Foxglove (WebSocket)           | Rerun (WebSocket 桥接)                |
|------------|--------------------------------|---------------------------------------|
| 数据模型   | 类似 ROS 的平铺式 topic       | 实体组件系统 (ECS)                    |
| 主要协议   | Foxglove WebSocket (二进制 JSON) | Rerun WebSocket 桥接 (基于 Arrow)    |
| 时间线     | 单一全局时钟                  | 每个实体独立的时间线，支持时间范围    |
| 录制格式   | MCAP                          | `.rrd` (自有格式) + MCAP 导入        |
| 客户端标识 | 字符串 ID                     | Application ID + 按实体的路径        |

最关键的架构变化是：从 **平铺的 topic 命名空间** 转向
**包含时间线的 ECS 层级结构**。这是一个深层的语义转变，而非简单的
换皮。

## 4. 建议的架构

### 4.1 实体组件抽象
我们在 SDK 内部引入一套最小化的 ECS 表示：

- `RerunEntity` – 场景中的逻辑对象（例如，一台机器人，一个相机）。
- `RerunComponent` – 附着在实体上的一组数据（例如，
  `Transform3D`、`Points3D`、`Image`）。
- `RerunTimeline` – 每个实体可拥有一条命名时间线（默认：“stable”）。
- `RerunRecorder` – 一个管理器，负责从各实体收集组件并将它们刷入
  Rerun WebSocket 桥接。

此抽象与 Rerun 自身 SDK（C++、Python、Rust）的设计相呼应，但以纯 C#
实现，并允许 Unity 的 GameObject 选择性地充当 entity。

### 4.2 Schema 映射
我们将以 `RerunComponent` 子类的形式实现以下 Rerun 原型：

- **Transform3D**: 映射到 `UnityEngine.Transform`（位置、旋转、缩放）。
- **Points3D**: 用于点云、激光雷达数据。
- **Image**: 用于相机纹理、渲染目标。
- **Scalar**: 用于时间序列图。
- **TextLog**: 用于日志消息。
- **BoundingBox3D**、**Mesh3D**、**LineStrips3D**（后期阶段）。

每个组件根据 Rerun WebSocket 桥接规范，序列化为基于 Arrow 的二进制
记录（通过 WebSocket 消息传输 Arrow IPC 格式）。

### 4.3 Source Generator: `[RerunRecord]`
我们复用 Roslyn ISG 流水线，通过修改后的模板自动检测带有
`[RerunRecord]`（或 `[RerunScalar]`、`[RerunPosition]` 等）标记者。
生成器生成的代码在运行时会：

- 创建对应的 `RerunEntity` 与 `RerunComponent`。
- 以可配置的频率（默认 10 Hz）推送更新。
- 支持双向参数控制，使 Rerun 蓝图能够修改 Unity 字段（与
  `[FoxRun]` 的双向特性类似）。

### 4.4 录制与回放
- **录制**：`RerunRecorder` 在与 Rerun 查看器连接时通过 WebSocket
  流式传输，离线时直接写入 `.rrd` 文件。我们也可以支持 MCAP 导出
  以实现跨工具兼容。
- **回放**：加载 `.rrd` 文件并在 Unity 内回放，类似于 MCAP 回放引擎，
  但拥有 ECS 感知的场景重建能力。

## 5. 实施阶段

### 阶段 1: 核心桥接与最小查看器连接
- 实现 `RerunWebSocketBridge`（客户端 → Rerun 服务器）。
- 实现 `Entity`、`Component`、`Timeline` 抽象。
- 将一个 Unity GameObject 的静态 `Transform3D` 组件流式传输到 Rerun
  查看器，并在 3D 空间中看到它。
- 暂不引入 Source Generator（仅提供手动 API）。

### 阶段 2: 必要原型与 Source Generator
- 实现 `Transform3D`、`Scalar`、`TextLog`、`Image` 组件。
- 移植 Roslyn Source Generator 以支持 `[RerunRecord]` 并生成对应的
  实体/组件连接代码。
- 添加 IL2CPP 保留（`Rerun_link.xml` 自动生成）。

### 阶段 3: 录制与 MCAP 互操作
- 按照 Rerun 的文件格式实现 `.rrd` 写入器。
- 确保 `Unity2Foxglove` 的 MCAP 录制可以转换/导入，并通过 Rerun
  时间线系统回放。
- 在 Unity 中添加一个最小化回放系统。

### 阶段 4: 高级类型与性能
- `Points3D`、`Mesh3D`、`LineStrips3D` 组件。
- 批量消息发送以减少开销。
- 帧同步与时间对齐测试。

### 阶段 5: 生态系统与文档
- 官方示例项目（URP、HDRP 兼容）。
- 多语言文档 (en/zh)。
- 针对无头 Rerun 桥接实例的自动化测试 CI。

## 6. 最终目标

**让 Rerun 成为 Unity 中的一等公民。** 开发者应当可以：

1. 将 `RerunManager` 放入 Unity 场景。
2. 用 `[RerunRecord]` 标记几个字段。
3. 点击 Play。
4. 在 Rerun 查看器中看到实时、多数据流的 3D 加标量可视化，
   没有外部进程，不需要 ROS 安装，不需要 Python 脚本。

会话结束后，可以保存一个 `.rrd` 录制文件，轻松分享、回放与分析——
就像 `Unity2Foxglove` 在 MCAP 与 Foxglove 上所做的那样自然。

## 7. 为什么这很重要

Rerun 正迅速成为“物理 AI”——机器人、空间计算和具身智能——的
可视化基石。Unity 是全球最大的实时 3D 创作工具。一个高质量、低摩擦、
将两者连接起来的 SDK 将是开源生态中的独特资产。今天，这样的桥梁还
不存在。你现有的工作已经证明，你有能力建造它。

---

*本计划复用了 `JianbinLiu-CFLab/unity-foxglove-sdk` 中的架构、工具
及来之不易的经验。请参阅该仓库了解基础的技术细节。*
# Publisher 组件

Publisher 组件让你通过 Unity Inspector 录制 Rerun 数据——无需编写代码。

## RerunManager

核心组件。每个场景一个，通常挂在一个名为 `Rerun` 的专用 GameObject 上。

| 字段 | 说明 |
|------|------|
| Application Id | 在 Rerun Viewer 中显示的应用名称 |
| Output Mode | FileOnly / LiveOnly / FileAndLive |
| Output Path | `.rrd` 输出路径。可用 `{TIMESTAMP}` 自动命名 |
| Record On Start | 自动在 Play 时开始录制 |
| Write View Coordinates | 在 `world` entity 上写入 `RIGHT_HAND_Y_UP` |

## RerunTransformPublisher

将 Transform 的位置和旋转发布到 `world/<entity>`。

| 字段 | 说明 |
|------|------|
| Target | 要发布的 Transform。为空时使用自身 |
| Entity Path | 自定义路径。为空时自动从 GameObject 生成 |
| Publish Rate Hz | 0 = 每帧，10 = 10 Hz |

## RerunScalarPublisher

将数值发布到 `<entity>`。

| 字段 | 说明 |
|------|------|
| Source | 内置数据源（Fps、DeltaTime、TransformPosition、Constant 等） |
| Constant Value | Source 为 Constant 时的固定值 |
| Target | position/rotation 类 source 的目标 Transform |

## RerunTextLogPublisher

将文本日志发布到 `logs/<entity>`。

| 字段 | 说明 |
|------|------|
| Message | 日志正文 |
| Level | INFO、WARN、ERROR、DEBUG、TRACE |
| Repeat | 持续发布（关闭则为一次性） |
| Append Frame Count | 在消息后追加帧号 |

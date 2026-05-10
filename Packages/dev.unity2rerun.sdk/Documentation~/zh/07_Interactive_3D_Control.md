# Interactive 3D Control

Phase 8 增加一个可见的 Unity2Rerun 闭环 demo：

- `LogEncodedImage` 把 JPEG/PNG 字节发布为 Rerun `EncodedImage`。
- `LogBox3D` / `LogBoxes3D` 用 half-extent 发布 Rerun `Boxes3D`。
- `LogLineStrips3D` 把轨迹发布为 Rerun `LineStrips3D`。
- `RerunInteractiveControlBridge` 提供本地 sidecar 控制面板，用来反向控制 Unity。

## Sidecar Control

Sidecar 只绑定 `127.0.0.1`。默认端口是 `18765`；如果端口被占用，server 会 fallback 到随机可用 loopback 端口，并在日志中写出实际 URL。

保持 `RerunManager` 上的 `Run In Background` 开启。sidecar 命令先由后台 HTTP 线程接收，再在 Unity `Update` 中应用；浏览器或 Rerun Viewer 获得焦点时，Unity 也必须继续 tick。

Phase 8 的 sidecar 是 Windows Editor sample 能力。它不修改 Rerun Viewer，也不开放远程控制。

## Image Publishing

`RerunCameraImagePublisher` 捕获相机画面，编码为 JPEG 或 PNG，然后调用：

```csharp
manager.LogEncodedImage("camera/main", bytes, "image/jpeg");
```

sample 默认配置为 `640x480`、JPEG quality `70`、`10Hz`，并带最大 encoded payload 限制。

## 3D Publishing

Unity cube 需要使用 half-extent：

```csharp
var halfSize = Abs(transform.lossyScale) * 0.5f;
manager.LogTransform("world/cube", transform);
manager.LogBox3D("world/cube", Vector3.zero, halfSize, Quaternion.identity, Color.green);
```

sample 会把 Transform3D 和 Boxes3D 写在同一个 `world/cube` entity 上。这个模式和 Foxglove scene 更接近：transform 负责移动 entity，cube primitive 保持在 entity-local frame 的原点。`lossyScale * 0.5f` 是 Unity cube sample 的实用默认值。

trajectory 会保留最近的点，并作为一个 `LineStrips3D` entity 发布。

## Performance Notes

使用 Unity Profiler markers：

- `RerunCameraImagePublisher.Update`
- `RerunCameraImagePublisher.ImageEncode`
- `RerunInteractive3DPublisher.Spatial`

在 Phase 8 acceptance note 中记录平均/峰值单帧耗时、GC alloc 和 encoded payload size。

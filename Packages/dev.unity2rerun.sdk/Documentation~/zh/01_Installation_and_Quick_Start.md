# 安装与快速上手

## 通过 UPM 安装

1. 打开 Unity，进入 `Window > Package Manager`
2. 点击 `+` > `Add package from disk...`
3. 选择 `Packages/dev.unity2rerun.sdk/package.json`
4. 点击 `Import`

## 快速上手（无需写代码）

1. 创建空的 GameObject，命名为 `Rerun`
2. 添加 `RerunManager` 组件（保持默认设置）
3. 添加 `RerunTextLogPublisher`，设置 `_message` 为 "Hello Rerun"
4. 进入 Play Mode，等待 1 秒，然后停止
5. 在 Console 中查看 `.rrd` 输出路径
6. 用 `rerun <path>` 打开，或拖入 Rerun Viewer

## 快速上手（代码）

```csharp
using Unity.RerunSDK.Unity;
using UnityEngine;

public class MinimalRecorder : MonoBehaviour
{
    private RerunManager _mgr;

    void Start()
    {
        _mgr = GetComponent<RerunManager>();
        _mgr.StartRecording();
    }

    void Update()
    {
        if (!_mgr.IsRecording) return;
        _mgr.SetTimeSequence("frame", Time.frameCount);
        _mgr.LogText("logs/unity", $"第 {Time.frameCount} 帧");
        _mgr.LogScalar("metrics/fps", 1.0 / Time.deltaTime);
        _mgr.LogTransform("world/cube", transform);
    }

    void OnDestroy()
    {
        _mgr?.StopRecording();
    }
}
```

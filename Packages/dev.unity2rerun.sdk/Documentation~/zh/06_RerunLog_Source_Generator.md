# RerunLog Source Generator

`[RerunLog]`、`[RerunScalar]`、`[RerunTransform]` 让 `partial MonoBehaviour` 不用手写 `RerunManager.Log*()`，也能自动发布 TextLog、Scalar 和 Transform3D。

## 要求

- 标注所在类型必须是 `partial`。
- 标注所在类型必须最终继承 `UnityEngine.MonoBehaviour`，可以是间接继承。
- 场景里必须有一个可见的 `RerunManager`。
- 可以在用户脚本里正常定义 `OnEnable`、`OnDisable`、`OnDestroy`。`RerunManager` 会在录制期间发现 active 的 generated log source。

## 示例

```csharp
using Unity.RerunSDK.Unity;
using UnityEngine;

[RerunTransform("world/player", RateHz = 30f)]
public partial class PlayerDebug : MonoBehaviour
{
    [RerunLog("logs/player", RateHz = 1f)]
    private string _status = "ready";

    [RerunScalar("metrics/player_speed", RateHz = 10f)]
    public float Speed { get; private set; }

    private void Update()
    {
        Speed = Time.deltaTime > 0f ? 1f / Time.deltaTime : 0f;
        _status = $"frame {Time.frameCount}";
    }
}
```

## 生成了什么

生成的 partial class 会实现 `IRerunGeneratedLogSource`：

```csharp
int RerunLog_EntryCount { get; }
RerunGeneratedLogEntry RerunLog_GetEntry(int index);
void RerunLog_Publish(int index, RerunManager manager);
```

`RerunLog_` 前缀是有意保留的，用来提醒这是 generated-only bridge，不是普通用户 API。

## Editor 和 Player 路径

- Editor 里 Unity 会加载 `Editor/SourceGenerators/analyzers/dotnet/cs/RerunLogSourceGenerator.dll`。
- Player build 前，`RerunLogBuildPreprocess` 会写出物理 fallback 文件到 `Assets/Scripts/Generated/RerunLog/`。
- 同一步会写出 `Assets/RerunLog_link.xml`，让 IL2CPP 保留被检测到的用户类型。

## 排查

- `RERUNLOG001`：class 声明加上 `partial`。
- `RERUNLOG002`：`[RerunLog]` 用 `string`，`[RerunScalar]` 用数字，`[RerunTransform]` 用 `Transform` 或 `GameObject`。
- `RERUNLOG003`：修 entity path。空 path 或 `logs//bad` 这种空 segment 不合法。
- `RERUNLOG004`：多变量 field 拆成一行一个 field。
- `RERUNLOG005`：把 attribute 移到有效的 `MonoBehaviour` 类型或支持的成员上。


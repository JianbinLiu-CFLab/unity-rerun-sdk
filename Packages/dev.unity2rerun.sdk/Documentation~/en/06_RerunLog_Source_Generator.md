# RerunLog Source Generator

`[RerunLog]`, `[RerunScalar]`, and `[RerunTransform]` let a `partial MonoBehaviour` publish values without writing `RerunManager.Log*()` calls by hand.

## Requirements

- The containing type must be `partial`.
- The containing type must inherit `UnityEngine.MonoBehaviour`, directly or through a base class.
- The scene must contain a visible `RerunManager`.
- User lifecycle methods such as `OnEnable`, `OnDisable`, and `OnDestroy` are allowed. `RerunManager` discovers active generated log sources while recording.

## Example

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

## What Gets Generated

The generated partial class implements `IRerunGeneratedLogSource`:

```csharp
int RerunLog_EntryCount { get; }
RerunGeneratedLogEntry RerunLog_GetEntry(int index);
void RerunLog_Publish(int index, RerunManager manager);
```

The `RerunLog_` prefix is intentional. It marks generated-only bridge members and avoids confusing them with public user APIs.

## Editor And Player Paths

- In the Editor, Unity loads `Editor/SourceGenerators/analyzers/dotnet/cs/RerunLogSourceGenerator.dll`.
- Before Player builds, `RerunLogBuildPreprocess` writes physical fallback files under `Assets/Scripts/Generated/RerunLog/`.
- The same build step writes `Assets/RerunLog_link.xml` so IL2CPP preserves detected user types.

## Troubleshooting

- `RERUNLOG001`: add `partial` to the class declaration.
- `RERUNLOG002`: use `string` for `[RerunLog]`, numeric values for `[RerunScalar]`, and `Transform` or `GameObject` for `[RerunTransform]`.
- `RERUNLOG003`: fix the entity path. Empty paths and empty segments like `logs//bad` are invalid.
- `RERUNLOG004`: split multi-variable fields into one field per declaration.
- `RERUNLOG005`: move the attribute to a valid `MonoBehaviour` type or supported member.

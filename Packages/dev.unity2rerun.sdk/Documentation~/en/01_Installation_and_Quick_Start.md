# Installation & Quick Start

## Install via UPM

1. Open Unity, go to `Window > Package Manager`
2. Click `+` > `Add package from disk...`
3. Select `Packages/dev.unity2rerun.sdk/package.json`
4. Click `Import`

## Quick Start (No Code)

1. Create an empty GameObject, name it `Rerun`
2. Add `RerunManager` component (keep default settings)
3. Add `RerunTextLogPublisher` and set `_message` to "Hello Rerun"
4. Enter Play Mode, wait 1 second, then stop
5. Check Console for the `.rrd` output path
6. Open with: `rerun <path>` or drag into Rerun Viewer

## Quick Start (Code)

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
        _mgr.LogText("logs/unity", $"Frame {Time.frameCount}");
        _mgr.LogScalar("metrics/fps", 1.0 / Time.deltaTime);
        _mgr.LogTransform("world/cube", transform);
    }

    void OnDestroy()
    {
        _mgr?.StopRecording();
    }
}
```

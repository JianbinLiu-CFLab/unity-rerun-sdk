# 前置条件

## Unity

- Unity 6000.0 LTSC 或更高版本（开发环境为 6000.3.14f1 LTSC；兼容 6000.0.74f1 LTSC）
- Windows（Editor + Standalone Player）。macOS/Linux 是预期目标，但尚未验证。
- IL2CPP Player 支持：Phase 6+

## Rerun Viewer

- [Rerun 0.31.4+](https://rerun.io)
- 通过 `pip install rerun-sdk` 安装，或从 [GitHub Releases](https://github.com/rerun-io/rerun/releases) 下载
- 确保 `rerun`（或 `rerun.exe`）在 PATH 中

## 实时传输 (gRPC)

实时传输需要 Unity 中的 HTTP/2 gRPC 支持。安装 **Cysharp YetAnotherHttpHandler** 及其 native dependency package。

在 `Packages/manifest.json` 中添加：

```json
{
  "scopedRegistries": [
    {
      "name": "Unity NuGet",
      "url": "https://unitynuget-registry.openupm.com",
      "scopes": ["org.nuget"]
    }
  ],
  "dependencies": {
    "com.cysharp.yetanotherhttphandler.dependencies": "https://github.com/Cysharp/YetAnotherHttpHandler.git?path=src/YetAnotherHttpHandler.Dependencies#1.11.5",
    "com.cysharp.yetanotherhttphandler": "https://github.com/Cysharp/YetAnotherHttpHandler.git?path=src/YetAnotherHttpHandler#1.11.5"
  }
}
```

## 已知限制

- `.rrd` 文件尚未包含 footer manifest（`rerun rrd verify` 会失败）
  文件本身仍然有效，可以在 Rerun Viewer 和 `rerun rrd stats/print` 中正常使用
- `LiveOnly` 模式需要 Viewer 正在运行；建议使用自动启动

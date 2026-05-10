---
title: Phase 6 Validation Runbook Results
date: 2026-05-09
tags:
  - unity2rerun
  - phase6
  - validation
  - il2cpp
aliases:
  - Phase 6 验证过程记录
---

# Phase 6 验证过程与结果

关联计划：[[Phase6_IL2CPP_Build_Import_Release_Gate]]

> [!success] 结论
> Phase 6 的主要验收项已通过：脚本 dry-run、Unity Editor FileOnly、RRD verify、FileAndLive live transport、Windows IL2CPP build 均成功。  
> 严格意义上的独立 Player runtime smoke 还可以后续再补一次，即直接启动 `Unity2RerunDemo.exe` 后检查 `Player.log` 和 Player 写出的 `.rrd`。

## 环境

| 项目 | 结果 |
|---|---|
| 日期 | 2026-05-09 |
| OS | Windows 10 64-bit |
| Unity | 6000.3.14f1 |
| Unity 路径 | `C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe` |
| Rerun CLI | `rerun-cli 0.31.4` |
| .NET SDK | `10.0.203` |
| Python | `3.13.9` |
| Unity 项目 | `D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\Unity2Rerun` |
| SDK 工作区 | `D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox` |

## 0. 基础命令确认

在仓库根目录执行：

```powershell
cd "D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox"
rerun --version
```

结果：

```text
rerun-cli 0.31.4 ... built 2026-04-28T15:20:27.8495246Z
```

结论：Rerun CLI 可用，版本为 `0.31.4`。

## 1. IL2CPP build script dry-run

执行：

```powershell
python Scripts\build_unity_il2cpp.py --dry-run --unity-path "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe"
```

输出确认：

```text
[build_unity_il2cpp] Unity:    C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe
[build_unity_il2cpp] Project:  Unity2Rerun
[build_unity_il2cpp] Log:      build\Unity\win64-il2cpp-20260509-202135\Unity2Rerun_IL2CPP_build.log
[build_unity_il2cpp] Output:   build\Unity\win64-il2cpp-20260509-202135\WindowsIL2CPP\Unity2RerunDemo.exe
[build_unity_il2cpp] Dry run only; Unity was not started.
```

结论：脚本能正确解析 Unity 路径、Unity 项目、build log 路径和 IL2CPP 输出路径。

## 2. Unity Editor FileOnly 验收

操作过程：

1. 打开 Unity 项目 `Unity2Rerun`。
2. 使用 `Assets/Scenes/SampleScene.unity`。
3. 场景中保留 `Rerun` GameObject，并配置 `RerunManager`。
4. 创建 `SampleCube`，挂载 Publisher 相关组件。
5. 设置 FileOnly 或 FileAndLive 中的文件输出路径到 `build/RRD/`。
6. Play 一段时间后 Stop，生成 `.rrd`。

在 Rerun Viewer 中确认：

- 能打开生成的 `.rrd`。
- 能看到 `logs/unity`。
- 能看到 `metrics/fps`。
- 能看到 `world/cube`。
- TextLog 中能看到 `Phase6 editor smoke`。
- Rerun Viewer 没有明显 warning/error。
- 没有重复 `StoreInfo` 相关 warning。

本轮复核的 RRD：

```text
build\Unity\win64-il2cpp-20260509-205516\build\RRD\phase6_editor_fileonly_20260509_214015.rrd
```

文件大小：`1,251,678 bytes`

执行：

```powershell
rerun rrd verify --check-footers false "build\Unity\win64-il2cpp-20260509-205516\build\RRD\phase6_editor_fileonly_20260509_214015.rrd"
```

结果：

```text
1 file verified without error.
```

> [!note]
> Codex shell 中本次 `rerun rrd verify` 前出现过 `re_analytics` 的 access denied 日志，但 verify 结果为通过，不影响 `.rrd` 文件有效性。

## 3. FileAndLive / live transport 验收

Unity 配置：

| 字段 | 值 |
|---|---|
| Output Mode | `FileAndLive` |
| Auto Launch Viewer | enabled |
| Live Endpoint | `rerun+http://127.0.0.1:9876/proxy` |
| HTTP/2 provider | Cysharp `YetAnotherHttpHandler` |

Unity Console 关键日志：

```text
[Rerun] Viewer ready on http://127.0.0.1:9876/proxy
[RerunGrpcClient] Starting live stream loop to http://127.0.0.1:9876/proxy
[RerunGrpcClient] Using Cysharp YetAnotherHttpHandler for HTTP/2 live gRPC
[RerunGrpcClient] WriteMessages stream opened to http://127.0.0.1:9876/proxy
[RerunGrpcClient] StoreInfo sent to live stream
[RerunGrpcClient] Data message sent to live stream
[RerunGrpcClient] WriteMessages request stream completed
```

结论：

- Viewer auto launch 可用。
- Unity 到 Rerun Viewer 的 live gRPC stream 可打开。
- `StoreInfo` 和数据消息均发送。
- Stop 时 request stream 能正常 completed。
- 之前的 HTTP/1.1 downgrade / `PlatformNotSupportedException` 问题已通过 `YetAnotherHttpHandler` 路径解决。

## 4. Windows IL2CPP build 验收

执行命令：

```powershell
python Scripts\build_unity_il2cpp.py --unity-path "C:\Program Files\Unity\Hub\Editor\6000.3.14f1\Editor\Unity.exe"
```

输出目录：

```text
build\Unity\win64-il2cpp-20260509-205516\WindowsIL2CPP
```

关键文件：

| 文件 | 大小 |
|---|---:|
| `WindowsIL2CPP\Unity2RerunDemo.exe` | 667,648 bytes |
| `WindowsIL2CPP\GameAssembly.dll` | 39,477,248 bytes |
| `WindowsIL2CPP\UnityPlayer.dll` | 36,280,752 bytes |
| `Unity2Rerun_IL2CPP_build.log` | 607,267 bytes |

build log 关键结果：

```text
Build Finished, Result: Success.
[RerunBuild] Build succeeded: D:\BaiduSyncdisk\Obsidian Vault\Unity2Rerun\00 Inbox\build\Unity\win64-il2cpp-20260509-205516\WindowsIL2CPP\Unity2RerunDemo.exe
[build_unity_il2cpp] Unity exited after 27:05.
[build_unity_il2cpp] Build command completed successfully.
```

结论：

- Windows IL2CPP Player build 成功。
- Unity batchmode 最终 return code 为 `0`。
- build script 的 fail-fast 路径没有触发。
- build log 中没有 C# compile error、protobuf/Arrow/HTTP2 blocker。

观察到但不阻塞：

```text
[Licensing::Module] Error: Failed to handshake to channel: "LicenseClient-LJB"
[Licensing::Module] Error: Access token is unavailable; failed to update
```

这两条出现在 build log 前段，但最终 build 成功，当前按 Unity licensing 噪声记录，不作为 Phase 6 blocker。

## 验收矩阵

| Gate | 结果 | 证据 |
|---|---|---|
| Rerun CLI 可用 | Pass | `rerun-cli 0.31.4` |
| build script dry-run | Pass | 正确解析 Unity/project/log/output |
| Unity Editor compile/import | Pass | Console 无红错后进入手工 smoke |
| Editor FileOnly `.rrd` | Pass | Viewer 可打开，`rerun rrd verify` 通过 |
| TextLog | Pass | `logs/unity`，body 含 `Phase6 editor smoke` |
| Scalar | Pass | `metrics/fps` 曲线可见 |
| Transform3D | Pass | `world/cube` 可见 |
| FileAndLive live stream | Pass | `WriteMessages stream opened` / `request stream completed` |
| HTTP/2 Unity gRPC | Pass | 使用 `YetAnotherHttpHandler`，无 downgrade warning |
| Windows IL2CPP build | Pass | `Build Finished, Result: Success` |
| 独立 Player runtime smoke | Follow-up | 需要单独启动 `Unity2RerunDemo.exe` 后检查 Player `.rrd` 和 `Player.log` |

## 后续建议

- Phase 6 可以视为 build/import/release gate 通过。
- 若要做更严格 release tag，建议补一次独立 Player runtime smoke：
  1. 启动 `build\Unity\win64-il2cpp-20260509-205516\WindowsIL2CPP\Unity2RerunDemo.exe`。
  2. 等待 5-10 秒后退出。
  3. 检查 Player 输出 `.rrd`。
  4. 运行 `rerun rrd verify --check-footers false <player-output.rrd>`。
  5. 检查 `Player.log` 中没有 asmdef、DLL、protobuf、Arrow、HTTP/2 相关错误。


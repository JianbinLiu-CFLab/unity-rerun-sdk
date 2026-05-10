# IL2CPP 构建指南

## 前置条件

- Unity 6000.0 LTSC 或更高版本（开发环境为 6000.3.14f1 LTSC；兼容 6000.0.74f1 LTSC）
- 通过 Unity Hub 安装 Windows Build Support (IL2CPP)
- Cysharp `YetAnotherHttpHandler`（Git 依赖，见前置条件）
- `Scripts/build_unity_il2cpp.py`（Python 3.9+）

## 构建

```powershell
python Scripts/build_unity_il2cpp.py --unity-path "<Unity.exe 的完整路径>"
```

## Player.log 位置

`%USERPROFILE%\AppData\LocalLow\<公司名>\<产品名>\Player.log`

## 常见问题

### asmdef / DLL 加载错误

- 确认 `Runtime/Plugins` 中所有 DLL 与 `Runtime/Unity.RerunSDK.asmdef` 的 references 一致
- 确认 `Runtime/link.xml` 保留了所有必需的 assembly

### IL2CPP stripping / missing method

- 确保消费项目中存在 SDK 的 `link.xml`
- 如果使用自定义 `link.xml`，请合并 SDK 的保留规则

### Protobuf / Arrow 缺少方法

- Google.Protobuf 和 Apache.Arrow 需要反射支持
- 两者均已在 SDK 的 `link.xml` 中保留

### RRD footer verify 失败

- `rerun rrd verify` 需要 SDK 尚未写入的 footer manifest
- 使用 `rerun rrd stats <file>` 或直接在 Rerun Viewer 中打开文件
- 完整 footer 支持将在 Phase 9 中处理

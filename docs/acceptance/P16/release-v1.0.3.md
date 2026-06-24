# LuluPet v1.0.3 发布前检查清单

## 版本目标

`v1.0.3` 是多屏边界修复发布，主要包含：

- 修复多屏上下错位时，拖拽和 Walk 会进入真实屏幕外虚拟空白区域的问题。
- 拖拽、普通 Walk 和窗口 Clamp 改为基于所有物理屏幕矩形选择最近可用边界。
- 保留跨屏拖动、负坐标保存恢复和当前屏幕绕屏行为。
- 补充错位副屏边界单元测试。

## 本地检查

| 项目 | 结果 |
|---|---|
| `dotnet test tests/LuluPet.Core.Tests/LuluPet.Core.Tests.csproj --no-restore /nr:false /m:1` | 通过，返回 `exit:0`，本地环境不输出测试明细 |
| Core `Desktop` 边界类 Roslyn 轻量编译 | 通过 |
| `git diff --check` | 通过 |
| Linux WPF build | 不适用，缺 `Microsoft.NET.Sdk.WindowsDesktop`，完整发布以 Windows CI 为准 |

## Windows CI 检查

| 项目 | 结果 |
|---|---|
| `dotnet restore` | 待 GitHub Actions 验证 |
| `dotnet test --configuration Release --no-restore` | 待 GitHub Actions 验证 |
| `dotnet publish ... -o publish/win-x64` | 待 GitHub Actions 验证 |
| artifact zip 包含 `LuluPet.exe`、`Assets/`、`settings.json` | 待 GitHub Actions 验证 |
| Release asset `LuluPet-win-x64-v1.0.3.zip` | tag 发布后验证 |
| Release asset SHA256 | tag 发布后验证 |

## Windows 手动验收

| 场景 | 结果 |
|---|---|
| 单屏拖拽不会越出屏幕 | 待验收 |
| 单屏 Walk 到边缘后回到 Idle | 待验收 |
| 左右双屏对齐时可跨屏拖动 | 待验收 |
| 副屏在主屏左侧时可保存和恢复负 `Left` | 待验收 |
| 副屏在主屏上方时可保存和恢复负 `Top` | 待验收 |
| 副屏向下错位时，拖拽不能停在外接矩形上方空白区 | 待验收 |
| 副屏向下错位时，Walk 不能进入外接矩形上方空白区 | 待验收 |
| 副屏向上错位时，拖拽和 Walk 不能进入外接矩形下方空白区 | 待验收 |
| 提醒触发后的绕屏仍只绕当前物理屏幕一周 | 待验收 |
| 气泡在绕屏过程中持续显示当前提醒内容 | 待验收 |
| `F` 面板位置和交互不受多屏边界修复影响 | 待验收 |

## 发布命令

```bash
git tag v1.0.3
git push origin v1.0.3
```

GitHub Actions 会创建：

- `LuluPet-win-x64-v1.0.3.zip`
- `LuluPet-win-x64-v1.0.3.zip.sha256.txt`

## 回滚

- 代码回滚：`git revert <bad-commit>`，等待 CI 重新通过。
- Release 回滚：标记 `v1.0.3` 为异常或移除异常资产，并推荐上一稳定版本 `v1.0.2`。
- 数据回滚：关闭 LuluPet，备份或恢复 `%LOCALAPPDATA%\LuluPet\data\lulupet.db` 后再运行上一稳定版本。

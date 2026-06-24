# LuluPet v1.0.2 发布前检查清单

## 版本目标

`v1.0.2` 是 PLAN2 功能增强发布，主要包含：

- `F` 快捷键打开右侧气泡式提醒面板。
- 循环番茄钟、喝水提醒、起立休息提醒。
- 提醒触发时显示气泡，并让噜噜绕当前屏幕一周。
- 托盘悬停显示今日陪伴时长。
- 多屏拖拽、Walk 和负坐标保存恢复。
- 修复 WPF/WinForms 类型名冲突和 DPI 边界换算问题。

## 本地检查

| 项目 | 结果 |
|---|---|
| `dotnet test tests/LuluPet.Core.Tests/LuluPet.Core.Tests.csproj --no-restore` | 通过，返回 `exit:0` |
| XAML XML 解析 | 通过：`MainWindow.xaml`、`ToggleSwitch.xaml`、`ReminderBubblePanel.xaml` |
| Core 边界/陪伴类轻量编译 | 通过 |
| Linux WPF build | 不适用，缺 `Microsoft.NET.Sdk.WindowsDesktop` |

## Windows CI 检查

| 项目 | 结果 |
|---|---|
| `dotnet restore` | 待 GitHub Actions 验证 |
| `dotnet test --configuration Release --no-restore` | 待 GitHub Actions 验证 |
| `dotnet publish ... -o publish/win-x64` | 待 GitHub Actions 验证 |
| artifact zip 包含 `LuluPet.exe`、`Assets/`、`settings.json` | 待 GitHub Actions 验证 |
| Release asset `LuluPet-win-x64-v1.0.2.zip` | tag 发布后验证 |
| Release asset SHA256 | tag 发布后验证 |

## Windows 手动验收

| 场景 | 结果 |
|---|---|
| 启动后透明桌宠正常显示 | 待验收 |
| `F` 打开右侧提醒面板，不遮挡噜噜 | 待验收 |
| 提醒面板底部与噜噜图像底部平齐 | 待验收 |
| 番茄钟 Focus/Rest 循环 | 待验收 |
| 喝水提醒按间隔触发 | 待验收 |
| 起立休息提醒按间隔触发 | 待验收 |
| 提醒气泡在绕屏过程中保持显示 | 待验收 |
| 绕屏只绕当前屏幕/副屏一周 | 待验收 |
| 托盘悬停显示 `噜噜今天已经陪伴：xxh xxmin` | 待验收 |
| 隐藏到托盘后今日陪伴继续累计 | 待验收 |
| 重启后今日陪伴恢复累计 | 待验收 |
| 多屏拖拽不越界 | 待验收 |
| Walk 跨屏不越界 | 待验收 |
| 副屏负坐标保存和恢复 | 待验收 |
| 点击穿透开启后，`F` 面板仍可交互并能关闭 | 待验收 |

## 发布命令

```bash
git tag v1.0.2
git push origin v1.0.2
```

GitHub Actions 会创建：

- `LuluPet-win-x64-v1.0.2.zip`
- `LuluPet-win-x64-v1.0.2.zip.sha256.txt`

## 回滚

- 代码回滚：`git revert <bad-commit>`，等待 CI 重新通过。
- Release 回滚：标记 `v1.0.2` 为异常或移除异常资产，并推荐上一稳定版本 `v1.0.1`。
- 数据回滚：关闭 LuluPet，备份或恢复 `%LOCALAPPDATA%\LuluPet\data\lulupet.db` 后再运行上一稳定版本。

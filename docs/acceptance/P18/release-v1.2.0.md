# LuluPet v1.2.0 发布前检查清单

## 版本目标

`v1.2.0` 是 `v1.1.1` 之后的交互增强发布，主要包含：

- 新增托盘菜单 `散步模式`，位于 `点击穿透` 和 `开机启动` 之间。
- 设置面板新增 `散步模式` 开关，并持久化为 `interaction.walkMode`。
- 关闭散步模式时，噜噜不再随机 walk，会保持在当前位置。
- 提醒触发的绕屏逻辑不受散步模式影响，仍按原规则绕当前屏幕一周。
- 提醒绕屏过程中，左键点击、左键拖动或右键点击噜噜会停止本次绕屏并恢复 `Idle`。
- 剪切板历史监听在启动后持续生效，不依赖打开剪切板面板；Win32 通知失败时有低频轮询兜底。

## 本地检查

| 项目 | 结果 |
|---|---|
| `git diff --check` | 通过 |
| `git tag --list 'v1.2.0'` | 本地无已存在 tag |
| Linux WPF build | 不适用，缺 `Microsoft.NET.Sdk.WindowsDesktop`，完整发布以 Windows CI 为准 |
| NuGet restore / Core test | 当前沙箱网络受限，待 GitHub Actions 验证 |

## Windows CI 检查

| 项目 | 结果 |
|---|---|
| `dotnet restore` | 待 GitHub Actions 验证 |
| `dotnet test --configuration Release --no-restore` | 待 GitHub Actions 验证 |
| `dotnet publish ... -o publish/win-x64` | 待 GitHub Actions 验证 |
| artifact zip 包含 `LuluPet.exe`、`Assets/`、`settings.json` | 待 GitHub Actions 验证 |
| Release asset `LuluPet-win-x64-v1.2.0.zip` | tag 发布后验证 |
| Release asset SHA256 | tag 发布后验证 |
| GitHub Release 正文读取 `docs/releases/v1.2.0.md` | tag 发布后验证 |

## Windows 手动验收

| 场景 | 结果 |
|---|---|
| 启动后宠物可见，托盘菜单状态正常 | 待验收 |
| 托盘菜单显示 `点击穿透`、`散步模式`、`开机启动` 的顺序 | 待验收 |
| 默认关闭散步模式时，噜噜不会随机 walk | 待验收 |
| 开启散步模式后，噜噜会按原逻辑随机 walk | 待验收 |
| 关闭散步模式时，提醒仍会触发绕当前屏幕一周 | 待验收 |
| 提醒绕屏过程中左键点击噜噜，会停止绕屏并恢复 `Idle` | 待验收 |
| 提醒绕屏过程中左键拖动噜噜，会停止绕屏并进入拖动交互 | 待验收 |
| 提醒绕屏过程中右键点击噜噜，会停止绕屏并执行喂食交互 | 待验收 |
| 未打开剪切板面板时复制文本，再打开 `C` 面板能看到历史 | 待验收 |
| 重启后 `interaction.walkMode` 设置按 `settings.json` 恢复 | 待验收 |

## 发布命令

```bash
git tag v1.2.0
git push origin v1.2.0
```

GitHub Actions 会创建：

- `LuluPet-win-x64-v1.2.0.zip`
- `LuluPet-win-x64-v1.2.0.zip.sha256.txt`

## 回滚

- 代码回滚：`git revert <bad-commit>`，等待 CI 重新通过。
- Release 回滚：标记 `v1.2.0` 为异常或移除异常资产，并推荐上一稳定版本 `v1.1.1`。
- 数据回滚：关闭 LuluPet，备份或恢复 `%LOCALAPPDATA%\LuluPet\data\lulupet.db`、`clipboard-history.json` 和 `Transit/` 后再运行上一稳定版本。

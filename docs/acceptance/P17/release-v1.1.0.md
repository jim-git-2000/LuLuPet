# LuluPet v1.1.0 发布前检查清单

## 版本目标

`v1.1.0` 是桌面工具增强发布，相比 `v1.0.3` 主要包含：

- `C` 剪切板历史面板，保存最近 100 条文本并支持点击写回系统剪切板。
- 剪切板历史本地持久化，重启后恢复。
- `D` 文件中转站面板，支持拖入暂存、双击打开、右键复制文件。
- 文件中转站支持 `Ctrl+V` 粘贴当前复制的文件或未保存的剪切板图片。
- 托盘新增教程面板；设置改为与 C/D/F 相同风格的右侧气泡面板。
- 动画显示层优化：渲染节拍驱动、帧预加载冻结、图片缩放模式优化。
- 修复 Win11 文件复制剪切板兼容性、截图 PNG 保存错误、启动托盘显示/隐藏状态错误。

## 本地检查

| 项目 | 结果 |
|---|---|
| `git diff --check` | 通过 |
| `dotnet test tests/LuluPet.Core.Tests/LuluPet.Core.Tests.csproj --no-restore` | 本地返回 `exit:0`，显示 `Build succeeded` |
| Linux WPF build | 不适用，缺 `Microsoft.NET.Sdk.WindowsDesktop`，完整发布以 Windows CI 为准 |

## Windows CI 检查

| 项目 | 结果 |
|---|---|
| `dotnet restore` | 待 GitHub Actions 验证 |
| `dotnet test --configuration Release --no-restore` | 待 GitHub Actions 验证 |
| `dotnet publish ... -o publish/win-x64` | 待 GitHub Actions 验证 |
| artifact zip 包含 `LuluPet.exe`、`Assets/`、`settings.json` | 待 GitHub Actions 验证 |
| Release asset `LuluPet-win-x64-v1.1.0.zip` | tag 发布后验证 |
| Release asset SHA256 | tag 发布后验证 |
| GitHub Release 正文读取 `docs/releases/v1.1.0.md` | tag 发布后验证 |

## Windows 手动验收

| 场景 | 结果 |
|---|---|
| 启动后宠物可见，托盘 `隐藏` 可点击、`显示` 不可点击 | 待验收 |
| `F` 打开提醒面板 | 待验收 |
| `C` 打开剪切板历史面板 | 待验收 |
| 复制文本后 C 面板出现记录 | 待验收 |
| 点击剪切板历史后文本写回系统剪切板 | 待验收 |
| 重启后剪切板历史恢复最近 100 条 | 待验收 |
| `D` 打开文件中转站面板 | 待验收 |
| 拖入文件会复制到中转站 | 待验收 |
| D 面板中 `Ctrl+V` 粘贴复制的文件 | 待验收 |
| D 面板中 `Ctrl+V` 粘贴截图，生成可打开且内容正确的 PNG | 待验收 |
| 双击中转站文件可打开 | 待验收 |
| 右键中转站文件可复制并粘贴到资源管理器 | 待验收 |
| 设置面板可修改文件中转站文件夹 | 待验收 |
| 托盘教程面板可打开并可滚动阅读 | 待验收 |
| 点击穿透开启后 F/C/D 快捷键不生效 | 待验收 |
| 动画播放无明显卡顿或首帧解码停顿 | 待验收 |

## 发布命令

```bash
git tag v1.1.0
git push origin v1.1.0
```

GitHub Actions 会创建：

- `LuluPet-win-x64-v1.1.0.zip`
- `LuluPet-win-x64-v1.1.0.zip.sha256.txt`

## 回滚

- 代码回滚：`git revert <bad-commit>`，等待 CI 重新通过。
- Release 回滚：标记 `v1.1.0` 为异常或移除异常资产，并推荐上一稳定版本 `v1.0.3`。
- 数据回滚：关闭 LuluPet，备份或恢复 `%LOCALAPPDATA%\LuluPet\data\lulupet.db`、`clipboard-history.json` 和 `Transit/` 后再运行上一稳定版本。

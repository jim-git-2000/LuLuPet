# LuluPet v1.1.1 发布前检查清单

## 版本目标

`v1.1.1` 是 `v1.1.0` 之后的补丁发布，主要包含：

- 修复缩放过程中透明窗口频繁重绘导致的闪烁。
- 修复拖动到屏幕边缘后继续拖动时，窗口越界再被拉回导致的闪烁。
- 单击宠物时随机播放 `Happy` 或 `Surprised` 动画，资源缺失时自动回退。
- 保留 walk 的左右镜像逻辑，继续按水平移动方向翻转。
- 整理 `assets/pet` 序列帧命名，每个动作目录从 `0001` 开始连续编号。

## 本地检查

| 项目 | 结果 |
|---|---|
| `git diff --check` | 通过 |
| `dotnet test tests/LuluPet.Core.Tests/LuluPet.Core.Tests.csproj --no-restore -m:1 /nodeReuse:false` | 通过 |
| `git tag --list 'v1.1.1'` | 本地无已存在 tag |
| `assets/pet/*` 连续命名检查 | 通过 |
| Linux WPF build | 不适用，缺 `Microsoft.NET.Sdk.WindowsDesktop`，完整发布以 Windows CI 为准 |

## Windows CI 检查

| 项目 | 结果 |
|---|---|
| `dotnet restore` | 待 GitHub Actions 验证 |
| `dotnet test --configuration Release --no-restore` | 待 GitHub Actions 验证 |
| `dotnet publish ... -o publish/win-x64` | 待 GitHub Actions 验证 |
| artifact zip 包含 `LuluPet.exe`、`Assets/`、`settings.json` | 待 GitHub Actions 验证 |
| Release asset `LuluPet-win-x64-v1.1.1.zip` | tag 发布后验证 |
| Release asset SHA256 | tag 发布后验证 |
| GitHub Release 正文读取 `docs/releases/v1.1.1.md` | tag 发布后验证 |

## Windows 手动验收

| 场景 | 结果 |
|---|---|
| 启动后宠物可见，托盘菜单状态正常 | 待验收 |
| 拖动宠物到屏幕左/右/上/下边缘后继续拖动，无明显闪烁 | 待验收 |
| 在设置面板连续调整缩放，无明显闪烁 | 待验收 |
| 单击宠物会随机出现 `Happy` 或 `Surprised` | 待验收 |
| `Surprised` 缺失时单击仍可回退到 `Happy` | 待验收 |
| walk 移动时左右朝向仍跟随水平移动方向 | 待验收 |
| `assets/pet/*` 在发布包中完整复制 | 待验收 |

## 资源检查

| 动作 | 当前帧范围 |
|---|---|
| `angry` | `0001` 到 `0024` |
| `drag` | `0001` 到 `0024` |
| `eat` | `0001` 到 `0024` |
| `happy` | `0001` 到 `0020` |
| `idle` | `0001` 到 `0028` |
| `sleep` | `0001` 到 `0024` |
| `surprised` | `0001` 到 `0024` |
| `walk` | `0001` 到 `0006` |

## 发布命令

```bash
git tag v1.1.1
git push origin v1.1.1
```

GitHub Actions 会创建：

- `LuluPet-win-x64-v1.1.1.zip`
- `LuluPet-win-x64-v1.1.1.zip.sha256.txt`

## 回滚

- 代码回滚：`git revert <bad-commit>`，等待 CI 重新通过。
- Release 回滚：标记 `v1.1.1` 为异常或移除异常资产，并推荐上一稳定版本 `v1.1.0`。
- 数据回滚：关闭 LuluPet，备份或恢复 `%LOCALAPPDATA%\LuluPet\data\lulupet.db`、`clipboard-history.json` 和 `Transit/` 后再运行上一稳定版本。

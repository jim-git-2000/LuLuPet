# LuluPet PLAN2：提醒面板、今日陪伴与多屏增强

> 本文是 `docs/PLAN.md` 之后的功能增强计划。目标是在现有 LuluPet 桌宠基础上加入 **F 快捷键提醒面板**、**循环番茄钟**、**喝水提醒**、**起立休息提醒**、**托盘今日陪伴时长** 和 **多屏拖拽 / Walk**。本文默认继续沿用 **C# + .NET 8 + WPF + Win32 API**，并保持“核心逻辑放在 `LuluPet.Core`，桌面表现放在 `LuluPet.App`，平台能力放在 `LuluPet.Win32`”的拆分方式。

## Executive Summary

我要做的是 LuluPet 的第二阶段实用功能增强。第一阶段已经完成透明窗口、拖拽、托盘、动画、气泡对白、状态机、设置中心、SQLite 持久化和发布流程；PLAN2 不重写这些基础能力，而是在现有结构上继续扩展。

本阶段的核心交互是：当 LuluPet 主窗口处于激活状态时，按 `F` 弹出一个和现有对话气泡使用同类视觉语言的轻量面板。这个面板不是传统设置窗口，也不是系统通知弹窗，而是“气泡背景的交互框”。框内提供番茄钟开关、番茄钟时间设置、休息时间设置、喝水提醒开关与间隔设置、起立休息提醒开关与间隔设置。番茄钟开启后会在“专注 -> 提醒结束 -> 休息 -> 提醒再次开始”的流程中循环，喝水提醒和起立休息提醒按各自间隔独立触发。

我会继续坚持可回滚、可验收、可测试的方式推进，把 PLAN2 拆成若干 Phase。每个 Phase 都要能独立构建，Core 层逻辑要能在 Ubuntu 环境运行单元测试，WPF 视觉和多屏行为通过 Windows artifact 手动验收。

## 当前基线

当前仓库已经具备以下能力：

| 能力 | 当前状态 | PLAN2 如何复用 |
|---|---|---|
| 透明主窗口 | 已完成 | 新增气泡式提醒面板仍挂在 WPF 主窗口体系下 |
| PNG 动画 | 已完成 Idle/Walk/Sleep/Happy/Eat/Drag/Angry/Surprised | 提醒触发时复用 Happy 或 Surprised，缺失时回退 Idle |
| 气泡对白 | 已完成 `SpeechBubble` 控件和 `bubble_default.png` | 提醒面板视觉基于同类气泡背景，不做系统通知 |
| 托盘 | 已完成 `NotifyIcon`、显示/隐藏/设置/穿透/自启/退出 | 增加今日陪伴时长 Tooltip 和番茄钟状态可选菜单 |
| 设置 | 已完成 `settings.json`、`AppSettings`、`JsonSettingsStore` | 增加 `reminders` 配置节并做默认值补全 |
| SQLite | 已完成 pet profile/status/interaction log | 增加今日陪伴统计表，提醒事件写入 interaction log |
| Walk 边界 | 当前基于 `SystemParameters.WorkArea` | 改为多屏物理边界集合 |

## 功能目标

### 1. F 快捷键气泡式提醒面板

当主窗口激活时，按 `F` 打开提醒面板。这里的“激活”定义为 WPF 主窗口当前获得焦点或可接收键盘事件，不注册全局热键，不拦截其他应用里的 `F`。

提醒面板必须满足：

| 项目 | 要求 |
|---|---|
| 入口 | 主窗口激活时按 `F` |
| 重复打开 | 如果面板已显示，再按 `F` 关闭；或点击面板关闭按钮关闭 |
| 视觉 | 使用和现有气泡同类的背景、圆角、边框和轻量阴影 |
| 位置 | 默认显示在宠物上方或右上方，不能超出当前可见桌面边界 |
| 形态 | 主窗口内浮层优先，不另开普通设置窗口 |
| 操作 | 面板内控件可点击、可输入数值 |
| 穿透兼容 | 点击穿透开启时，打开面板前临时关闭穿透或阻止穿透影响面板交互 |

面板不是 `SettingsWindow` 的替代品。`SettingsWindow` 继续负责缩放、透明度、音量、点击穿透、开机启动等通用设置；提醒面板只负责番茄钟和提醒。

### 2. 循环番茄钟

番茄钟是一个循环状态机，不是一次性倒计时。开启后流程如下：

```text
关闭
  -> 用户点击开关开启
专注中
  -> 专注时长到
提醒：番茄钟结束，该休息了
休息中
  -> 休息时长到
提醒：休息结束，新的番茄钟开始
专注中
  -> 继续循环
```

番茄钟面板字段：

| 字段 | 默认值 | 范围 | 行为 |
|---|---:|---:|---|
| 开关 | 关闭 | 开/关 | 点击后左右滑动，开启即开始专注倒计时 |
| 番茄钟时间 | 25 分钟 | 1~240 分钟 | 修改后保存，下一轮生效；如果当前未开启，开启时立即使用 |
| 休息时间 | 5 分钟 | 1~120 分钟 | 修改后保存，下一次休息阶段生效 |
| 剩余时间 | 运行时显示 | 只读 | 格式 `专注中 24:59` 或 `休息中 04:59` |

提醒文案：

| 事件 | 文案 | 动画 |
|---|---|---|
| 专注结束 | `番茄钟结束啦，休息一下吧。` | 优先 `Happy`，否则 `Idle` |
| 休息结束 | `休息结束，新的番茄钟开始啦。` | 优先 `Surprised`，否则 `Happy`，再否则 `Idle` |
| 手动开启 | `番茄钟开始，噜噜陪你专注。` | `Happy` 或 `Idle` |
| 手动关闭 | `番茄钟已关闭。` | `Idle` |

### 3. 喝水提醒

喝水提醒是独立的循环提醒，不依赖番茄钟是否开启。

面板字段：

| 字段 | 默认值 | 范围 | 行为 |
|---|---:|---:|---|
| 开关 | 开启 | 开/关 | 点击后左右滑动 |
| 提醒间隔 | 45 分钟 | 1~240 分钟 | 开启后按间隔循环提醒 |
| 下次提醒 | 运行时显示 | 只读 | 显示剩余分钟，未开启时显示 `未开启` |

提醒文案：

```text
噜噜提醒你喝水啦。
```

触发后写入交互日志，类型为 `reminder_water`。

### 4. 起立休息提醒

起立休息提醒也是独立循环提醒，用于久坐提醒。

面板字段：

| 字段 | 默认值 | 范围 | 行为 |
|---|---:|---:|---|
| 开关 | 开启 | 开/关 | 点击后左右滑动 |
| 提醒间隔 | 60 分钟 | 1~240 分钟 | 开启后按间隔循环提醒 |
| 下次提醒 | 运行时显示 | 只读 | 显示剩余分钟，未开启时显示 `未开启` |

提醒文案：

```text
起来活动一下，伸个懒腰吧。
```

触发后写入交互日志，类型为 `reminder_stand`。

### 5. 今日陪伴托盘提示

鼠标移动到右下角托盘区域的 LuluPet 图标上时，托盘提示文本显示：

```text
噜噜今天已经陪伴：xxh xxmin
```

规则：

| 项目 | 规则 |
|---|---|
| 统计口径 | LuluPet 进程当天处于运行状态的累计时间 |
| 隐藏到托盘 | 仍然计入陪伴 |
| 日期边界 | 按本地日期切换 |
| 刷新频率 | 至少每分钟刷新一次 |
| 重启恢复 | 从 SQLite 恢复当天累计时长 |
| 文本长度 | 保持短文本，避免 `NotifyIcon.Text` 长度限制导致异常 |

### 6. 多屏拖拽和 Walk

当前代码使用 `SystemParameters.WorkArea`，更接近主屏工作区。PLAN2 要改为多屏物理边界集合，而不是把所有屏幕合并成一个外接大矩形。

目标行为：

| 场景 | 目标 |
|---|---|
| 拖拽 | 允许把噜噜从主屏拖到副屏 |
| 副屏在左侧 | 允许保存和恢复负 `Left` 坐标 |
| 副屏在上方 | 允许保存和恢复负 `Top` 坐标 |
| Walk | 可以从一个屏幕 walk 到另一个屏幕 |
| 边界 | 严格按每个物理屏幕真实边界约束，错位多屏不能进入虚拟空白区域 |
| 防丢失 | 窗口不能完全离开所有屏幕 |

## 推荐实现架构

```text
src/LuluPet.Core/
  Reminders/
    ReminderSettings.cs
    ReminderScheduler.cs
    ReminderEvent.cs
    ReminderKind.cs
    PomodoroPhase.cs
  Companion/
    CompanionTimeTracker.cs
    CompanionDayStats.cs
  Desktop/
    DesktopBounds.cs
    IDesktopBoundsProvider.cs

src/LuluPet.App/
  Controls/
    ToggleSwitch.xaml
    ToggleSwitch.xaml.cs
    ReminderBubblePanel.xaml
    ReminderBubblePanel.xaml.cs
  Services/
    WpfDesktopBoundsProvider.cs

src/LuluPet.Core/Config/
  AppSettings.cs
  JsonSettingsStore.cs

src/LuluPet.Core/Storage/
  SqlitePetRepository.cs
```

### Core 层职责

Core 层只处理可测试逻辑：

| 模块 | 职责 |
|---|---|
| `ReminderScheduler` | 推进番茄钟、喝水提醒、起立休息提醒 |
| `ReminderSettings` | 保存提醒设置，供 `AppSettings` 引用 |
| `CompanionTimeTracker` | 计算今日陪伴累计时间和跨日期切换 |
| `DesktopBounds` | 描述单个屏幕边界和多屏物理边界选择，不依赖 WPF 控件 |

Core 层不能引用 WPF 类型。时间推进使用显式 `Tick(TimeSpan elapsed, DateTimeOffset now)`，避免在单元测试里依赖真实计时器。

### App 层职责

App 层负责 UI 和计时器接线：

| 模块 | 职责 |
|---|---|
| `MainWindow` | 处理 `F`、显示/隐藏提醒面板、接收提醒事件 |
| `ReminderBubblePanel` | 气泡式提醒交互框 |
| `ToggleSwitch` | 滑动开关控件 |
| `DispatcherTimer` | 每秒推进番茄钟，每分钟刷新托盘文本 |
| `WpfDesktopBoundsProvider` | 从 `Forms.Screen.AllScreens` 获取每个物理屏幕边界 |

提醒触发时，统一调用现有 `ShowSpeech` 和 `TryPlayAction`，不要新增系统通知依赖。

### 存储层职责

SQLite 增加每日陪伴统计，不改破坏性迁移。

建议新增表：

```sql
CREATE TABLE IF NOT EXISTS companion_day_stats (
    local_date TEXT PRIMARY KEY,
    total_seconds INTEGER NOT NULL DEFAULT 0,
    updated_at_utc TEXT NOT NULL
);
```

建议新增 repository 方法：

```csharp
public CompanionDayStats LoadCompanionDayStats(DateOnly localDate);

public void SaveCompanionDayStats(CompanionDayStats stats);

public void AddCompanionSeconds(DateOnly localDate, long seconds, DateTimeOffset updatedAtUtc);
```

提醒事件继续写入 `interaction_log`：

| 事件 | interaction_type |
|---|---|
| 番茄钟开始 | `pomodoro_start` |
| 番茄钟关闭 | `pomodoro_stop` |
| 专注结束 | `pomodoro_focus_done` |
| 休息结束 | `pomodoro_rest_done` |
| 喝水提醒 | `reminder_water` |
| 起立休息提醒 | `reminder_stand` |

## 配置结构

`settings.json` 增加 `reminders`：

```json
{
  "reminders": {
    "pomodoroEnabled": false,
    "pomodoroMinutes": 25,
    "pomodoroRestMinutes": 5,
    "waterReminderEnabled": true,
    "waterReminderMinutes": 45,
    "standReminderEnabled": true,
    "standReminderMinutes": 60
  }
}
```

`AppSettings` 增加：

```csharp
public ReminderSettings Reminders { get; set; } = new();
```

`ReminderSettings` 建议字段：

```csharp
public sealed class ReminderSettings
{
    public bool PomodoroEnabled { get; set; }

    public int PomodoroMinutes { get; set; } = 25;

    public int PomodoroRestMinutes { get; set; } = 5;

    public bool WaterReminderEnabled { get; set; } = true;

    public int WaterReminderMinutes { get; set; } = 45;

    public bool StandReminderEnabled { get; set; } = true;

    public int StandReminderMinutes { get; set; } = 60;
}
```

约束规则：

| 字段 | 最小值 | 最大值 | 非法值回退 |
|---|---:|---:|---:|
| `PomodoroMinutes` | 1 | 240 | 25 |
| `PomodoroRestMinutes` | 1 | 120 | 5 |
| `WaterReminderMinutes` | 1 | 240 | 45 |
| `StandReminderMinutes` | 1 | 240 | 60 |

如果旧版 `settings.json` 没有 `reminders` 节点，加载时必须自动补默认值，并且不能丢失已有 `window`、`appearance`、`audio`、`interaction`、`startup` 配置。

## UI 设计要求

提醒面板必须和桌宠本身放在同一个视觉系统里。不要做成传统复杂设置页。

### 面板布局

建议结构：

```text
┌──────────────────────────────┐
│ 专注提醒                      │
│                              │
│ 番茄钟             [  ON  ]   │
│ 专注  [ 25 ] 分钟             │
│ 休息  [  5 ] 分钟             │
│ 状态：专注中 24:59            │
│                              │
│ 喝水提醒           [  ON  ]   │
│ 每    [ 45 ] 分钟             │
│                              │
│ 起立休息           [  ON  ]   │
│ 每    [ 60 ] 分钟             │
└──────────────────────────────┘
```

### 控件规则

| 控件 | 要求 |
|---|---|
| 开关 | 使用左右滑动式 Toggle，不使用普通 CheckBox 文案替代 |
| 数字输入 | 使用 `TextBox` 或 `NumericUpDown` 风格控件，只允许整数分钟 |
| 关闭 | 可使用小的 `x` 图标按钮或点击 `F` 再次关闭 |
| 状态文本 | 简短显示，不写大段说明 |
| 面板大小 | 默认宽度 260~320px，高度随内容固定，不遮挡宠物主体太多 |

### 交互规则

| 操作 | 行为 |
|---|---|
| 按 `F` | 面板未显示则显示，已显示则隐藏 |
| 开启番茄钟 | 立即进入专注阶段并开始倒计时 |
| 关闭番茄钟 | 清空运行中倒计时，保留设置值 |
| 修改专注时间 | 保存设置，当前阶段不强制重置；下一轮生效 |
| 修改休息时间 | 保存设置，下一次休息阶段生效 |
| 开启喝水提醒 | 从当前时间重新计算下一次提醒 |
| 修改喝水间隔 | 保存设置，并重新计算下一次提醒 |
| 开启起立休息 | 从当前时间重新计算下一次提醒 |
| 修改起立间隔 | 保存设置，并重新计算下一次提醒 |

## ReminderScheduler 行为

`ReminderScheduler` 建议维护以下状态：

| 状态 | 说明 |
|---|---|
| `PomodoroPhase` | `Off` / `Focus` / `Rest` |
| `PomodoroRemaining` | 当前阶段剩余时间 |
| `WaterRemaining` | 距离下次喝水提醒的剩余时间 |
| `StandRemaining` | 距离下次起立提醒的剩余时间 |

事件输出建议：

```csharp
public sealed record ReminderEvent(
    ReminderKind Kind,
    string Message,
    string InteractionType,
    string PreferredAction);
```

`ReminderKind`：

```csharp
public enum ReminderKind
{
    PomodoroStarted,
    PomodoroStopped,
    PomodoroFocusDone,
    PomodoroRestDone,
    Water,
    Stand
}
```

关键规则：

1. 番茄钟关闭时，不推进番茄钟倒计时。
2. 番茄钟开启后先进入 `Focus`。
3. `Focus` 到时后产生 `PomodoroFocusDone`，随后进入 `Rest`。
4. `Rest` 到时后产生 `PomodoroRestDone`，随后进入下一轮 `Focus`。
5. 喝水和起立提醒互不影响，也不影响番茄钟。
6. 如果多个提醒同一秒触发，按顺序处理：番茄钟、喝水、起立。气泡显示最后一个事件即可，但所有事件都要写日志。
7. 电脑睡眠或程序卡顿导致一次 Tick 跨过多个周期时，只触发每类提醒一次，并重置到下一周期，避免一次弹出大量提醒。

## 多屏边界实现细节

### 边界来源

使用：

```csharp
System.Windows.Forms.Screen.AllScreens
```

把所有屏幕的 `Bounds` 转换为 WPF DIP 后保留为物理屏幕矩形集合：

```text
screen[0] = left/top/right/bottom
screen[1] = left/top/right/bottom
...
```

不要继续用单一 `SystemParameters.WorkArea` 作为拖拽和 Walk 的唯一边界，也不要只用所有屏幕的外接矩形；上下错位的副屏会在外接矩形里产生真实屏幕外的空白区域。

### Clamp 规则

保留防丢失，但允许跨屏：

| 项目 | 规则 |
|---|---|
| 候选屏幕 | 根据候选窗口位置选择可完整容纳、中心点所在、重叠面积最大或距离最近的物理屏幕 |
| 最小 Left | `selectedScreen.Left` |
| 最大 Left | `selectedScreen.Right - windowWidth` |
| 最小 Top | `selectedScreen.Top` |
| 最大 Top | `selectedScreen.Bottom - windowHeight` |

这样窗口可以跨屏拖动，但最终位置必须完整落在某一个真实屏幕矩形内，不会进入多屏外接矩形的空白区域。

### Walk 规则

Walk 中计算下一位置时：

1. 先计算下一帧候选位置。
2. 根据候选位置从所有物理屏幕矩形中选择目标屏幕。
3. 把下一帧位置夹到该物理屏幕的移动边界内。
4. 如果触碰该屏幕边界，则 `ForceState(PetState.Idle)`。
5. 如果用户改了显示器布局，下一次 `Clamp` 时把窗口拉回最近的真实屏幕内。

## Phase 开发计划

### Phase P11｜提醒配置模型与调度器

**目标**

先完成 Core 层，不碰 WPF 视觉。确保番茄钟、喝水提醒、起立提醒的时间逻辑可测试。

**任务清单**

1. 在 `AppSettings` 中新增 `ReminderSettings`。
2. 在 `JsonSettingsStore` 中补全 `reminders` 默认值和范围约束。
3. 新增 `ReminderScheduler`、`ReminderEvent`、`ReminderKind`、`PomodoroPhase`。
4. 支持开启/关闭番茄钟、喝水提醒、起立提醒。
5. 支持番茄钟 Focus/Rest 循环。
6. 添加 Core 单元测试。

**验收标准**

- 旧版 `settings.json` 能正常加载并自动补 `reminders`。
- 非法分钟数会被约束到有效范围。
- 番茄钟开启后先进入 Focus，Focus 到时进入 Rest，Rest 到时回到 Focus。
- 喝水提醒和起立提醒能独立按间隔触发。
- 同一次大跨度 Tick 不会爆发大量重复提醒。

**建议测试**

```bash
dotnet test tests/LuluPet.Core.Tests/LuluPet.Core.Tests.csproj
```

### Phase P12｜F 快捷键与气泡式提醒面板

**目标**

实现主窗口激活时按 `F` 弹出气泡式提醒面板，并能修改设置、开启/关闭提醒。

**任务清单**

1. 在 `MainWindow.OnKeyDown` 中处理 `Key.F`。
2. 新增 `ReminderBubblePanel` 控件，视觉接近现有 `SpeechBubble`。
3. 新增滑动开关控件 `ToggleSwitch`，用于番茄钟、喝水提醒、起立休息提醒。
4. 面板包含番茄钟开关、专注分钟、休息分钟、喝水开关和间隔、起立开关和间隔。
5. 输入变化立即更新 `_settings.Reminders` 并保存。
6. 面板显示运行状态：番茄钟阶段、剩余时间、喝水/起立下次提醒。
7. 处理点击穿透兼容，确保面板打开时可交互。

**验收标准**

- 主窗口激活时按 `F` 能显示/隐藏面板。
- 主窗口未激活时按 `F` 不影响其他程序。
- 三个滑动开关都能正常改变状态。
- 分钟输入只接受有效整数，非法输入不会写入坏配置。
- 关闭并重启后设置仍保留。

**手动验收**

- Windows artifact 运行后点击宠物让主窗口获得焦点。
- 按 `F`，确认气泡式面板出现。
- 修改番茄钟时间、休息时间、喝水间隔、起立间隔。
- 重启程序后再次按 `F`，确认设置恢复。

### Phase P13｜提醒运行接线与日志

**目标**

把 `ReminderScheduler` 接入 `MainWindow` 的运行时计时器，让提醒真实触发气泡和动画。

**任务清单**

1. 在 `MainWindow` 新增 `_reminderTimer`，建议每秒 Tick。
2. Tick 中推进 `ReminderScheduler`。
3. 触发提醒时调用 `ShowSpeech`。
4. 触发提醒时优先播放事件的 `PreferredAction`，失败时回退 `Happy` / `Idle`。
5. 提醒事件写入 `interaction_log`。
6. 番茄钟开启/关闭时也写日志。
7. 面板打开时实时刷新状态文本。

**验收标准**

- 开启番茄钟后能看到倒计时变化。
- 专注时间到后显示 `番茄钟结束啦，休息一下吧。`。
- 休息时间到后显示 `休息结束，新的番茄钟开始啦。`。
- 喝水提醒到时显示 `噜噜提醒你喝水啦。`。
- 起立提醒到时显示 `起来活动一下，伸个懒腰吧。`。
- SQLite `interaction_log` 中存在对应事件类型。

**开发注意**

为了方便手动验收，允许临时把提醒间隔设置为 1 分钟；不要在代码里写死短间隔。

### Phase P14｜今日陪伴时长与托盘 Tooltip

**目标**

托盘悬停显示当天陪伴时长，并能在程序重启后继续累计。

**任务清单**

1. 新增 `CompanionTimeTracker`。
2. SQLite 增加 `companion_day_stats` 表。
3. `SqlitePetRepository` 增加加载和累加每日陪伴秒数的方法。
4. `MainWindow` 新增陪伴计时器，至少每分钟保存一次。
5. 更新 `NotifyIcon.Text` 为 `噜噜今天已经陪伴：xxh xxmin`。
6. 跨本地日期时自动切换到新日期统计。

**验收标准**

- 托盘悬停显示指定格式。
- 程序隐藏到托盘后仍继续累计。
- 重启程序后当天时长继续累加。
- 日期切换后从新一天重新统计。
- `NotifyIcon.Text` 设置异常时不会导致程序崩溃。

### Phase P15｜多屏拖拽与 Walk

**目标**

允许拖拽和 Walk 跨多个显示器，保存和恢复负坐标。

**任务清单**

1. 新增 `DesktopBounds` 和多屏边界提供器。
2. 把 `ClampWindowToWorkArea` 改为多屏物理边界 Clamp。
3. 把 `MoveWhileWalking` 的边界计算改为基于候选位置的物理屏幕边界。
4. 保存窗口位置时不拒绝负坐标。
5. 显示器布局变化后能够把窗口拉回可见区域。

**验收标准**

- 双屏环境可把噜噜从主屏拖到副屏。
- 副屏位于主屏左侧时，窗口位置可保存为负 `Left` 并重启恢复。
- Walk 能跨屏移动。
- 多屏错位时，拖拽和 Walk 不会进入真实屏幕外的虚拟空白区域。
- 单屏环境行为不退化。

### Phase P16｜文档、验收记录与发布

**目标**

整理 README、验收记录和发布说明，保证后续可以按 PLAN2 复盘。

**任务清单**

1. 更新 README 的操作逻辑，加入 `F` 提醒面板说明。
2. 记录新增 `settings.json` 字段。
3. 记录 SQLite 新增表。
4. 在 `docs/acceptance/P11` 到 `P16` 下保存测试和手动验收证据。
5. 生成 Windows artifact，并在 Windows 双屏环境做最终验收。

**验收标准**

- README 和 PLAN2 描述一致。
- `dotnet test` 通过。
- GitHub Actions Windows 构建通过。
- Windows 真机确认提醒、托盘和多屏行为。

## Codex 执行提示词模板

后续每个 Phase 可以按下面格式执行：

```text
请完成 docs/PLAN2.md 中的 Phase P{X}：{阶段名称}

要求：
1. 只做本 Phase，不进入下一 Phase。
2. 保持现有功能可编译、可测试。
3. Core 层逻辑必须添加或更新单元测试。
4. WPF UI 改动保持现有 LuluPet 视觉风格，不做无关重设计。
5. 不删除或破坏 PLAN.md 已有功能。
6. 返回变更文件列表、本地验证命令、风险和建议提交信息。

验收：
- 按 PLAN2 中本 Phase 的验收标准逐项说明。
- 如果因为 Linux 环境无法运行 WPF，请说明哪些内容需要 Windows artifact 手动验收。
```

## 测试策略

| 测试层级 | 环境 | 内容 |
|---|---|---|
| Core 单元测试 | Ubuntu / CI | 设置补全、提醒调度、陪伴时长、多屏边界计算 |
| Storage 单元测试 | Ubuntu / CI | SQLite 新表、每日陪伴累加、交互日志写入 |
| WPF 静态检查 | Ubuntu / CI | XAML、代码结构、资源路径 |
| Windows 构建 | GitHub Actions `windows-latest` | restore/test/publish |
| Windows 手动验收 | 真机 | F 面板、Toggle、倒计时、气泡提醒、托盘 Tooltip、多屏拖拽和 Walk |

## 风险与注意事项

| 风险 | 处理 |
|---|---|
| `NotifyIcon.Text` 长度限制 | 文案保持短格式，设置失败时捕获异常 |
| 点击穿透影响面板 | 面板打开期间确保可点击，关闭后恢复穿透设置 |
| WPF 主窗口未获得焦点导致 `F` 无效 | 这是预期，不做全局热键；可通过点击宠物后按 `F` |
| 电脑睡眠后 Tick 跨度过大 | 每类提醒最多补发一次，然后重置下一周期 |
| 多屏存在负坐标 | 配置和 SQLite 允许保存负坐标 |
| 屏幕布局变化 | Clamp 到新的虚拟边界，避免窗口完全丢失 |
| 旧配置缺少 reminders | `JsonSettingsStore` 自动补默认值 |
| 旧数据库缺少新表 | `CREATE TABLE IF NOT EXISTS` 无破坏升级 |

## 最终验收清单

| 项目 | 验收结果 |
|---|---|
| 主窗口激活时按 `F` 能打开气泡式提醒面板 | 待验收 |
| 再按 `F` 能关闭面板 | 待验收 |
| 番茄钟开关是左右滑动式 Toggle | 待验收 |
| 番茄钟专注时长可设置并持久化 | 待验收 |
| 番茄钟休息时长可设置并持久化 | 待验收 |
| 番茄钟开启后 Focus/Rest 循环 | 待验收 |
| Focus 结束显示气泡提醒 | 待验收 |
| Rest 结束显示再次开始提醒 | 待验收 |
| 喝水提醒可开启/关闭 | 待验收 |
| 喝水提醒间隔可设置并持久化 | 待验收 |
| 起立休息提醒可开启/关闭 | 待验收 |
| 起立休息提醒间隔可设置并持久化 | 待验收 |
| 托盘悬停显示 `噜噜今天已经陪伴：xxh xxmin` | 待验收 |
| 今日陪伴重启后继续累计 | 待验收 |
| 多屏拖拽允许跨屏 | 待验收 |
| Walk 允许跨屏 | 待验收 |
| 副屏负坐标可保存和恢复 | 待验收 |
| 单屏环境不退化 | 待验收 |

## 建议提交信息

```text
docs(plan): add reminder and multi-monitor roadmap
```

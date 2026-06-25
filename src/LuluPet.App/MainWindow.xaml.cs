using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LuluPet.Core;
using LuluPet.Core.Animation;
using LuluPet.Core.Behavior;
using LuluPet.Core.Clipboard;
using LuluPet.Core.Companion;
using LuluPet.Core.Config;
using LuluPet.Core.Desktop;
using LuluPet.Core.Dialogues;
using LuluPet.Core.Reminders;
using LuluPet.Core.Storage;
using LuluPet.App.Services;
using LuluPet.Win32;
using Forms = System.Windows.Forms;
using WpfClipboard = System.Windows.Clipboard;
using WpfTextDataFormat = System.Windows.TextDataFormat;

namespace LuluPet.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly JsonSettingsStore _settingsStore;
    private readonly AppSettings _settings;
    private readonly DialogueLineProvider _dialogueLineProvider;
    private readonly SqlitePetRepository _petRepository;
    private readonly FrameAnimationPlayer _animationPlayer = new();
    private readonly PetStateMachine _stateMachine = new();
    private readonly DispatcherTimer _stateTimer = new();
    private readonly DispatcherTimer _periodicSpeechTimer = new();
    private readonly DispatcherTimer _bubbleHideTimer = new();
    private readonly DispatcherTimer _reminderTimer = new();
    private readonly DispatcherTimer _companionTimer = new();
    private readonly ClipboardHistory _clipboardHistory;
    private readonly ClipboardHistoryStore _clipboardHistoryStore;
    private readonly FileTransitService _fileTransitService;
    private readonly ReminderScheduler _reminderScheduler;
    private readonly IDesktopBoundsProvider _desktopBoundsProvider;
    private readonly Dictionary<string, BitmapImage> _frameCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _showMenuItem;
    private readonly Forms.ToolStripMenuItem _hideMenuItem;
    private readonly Forms.ToolStripMenuItem _settingsMenuItem;
    private readonly Forms.ToolStripMenuItem _tutorialMenuItem;
    private readonly Forms.ToolStripMenuItem _clickThroughMenuItem;
    private readonly Forms.ToolStripMenuItem _autoStartMenuItem;
    private DateTimeOffset _lastAnimationTick;
    private DateTimeOffset _lastStateTick;
    private DateTimeOffset _lastReminderTick;
    private DateTimeOffset _lastCompanionTick;
    private TimeSpan? _lastAnimationRenderingTime;
    private DateTimeOffset? _interactionAnimationUntil;
    private CompanionTimeTracker? _companionTracker;
    private ClipboardMonitor? _clipboardMonitor;
    private nint _windowHandle;
    private PetState _initialPetState = PetState.Idle;
    private System.Windows.Point _dragStartScreenPoint;
    private double _dragStartLeft;
    private double _dragStartTop;
    private double _walkVelocityX = WalkPixelsPerSecond;
    private double _walkVelocityY;
    private readonly List<System.Windows.Point> _screenLapTargets = new();
    private int _screenLapTargetIndex;
    private int _visualSettingsLayoutVersion;
    private string? _screenLapSpeechText;
    private bool _isClampingWindow;
    private bool _isApplyingVisualSettings;
    private bool _isVisualSettingsLayoutPending;
    private bool _isPotentialDrag;
    private bool _isDraggingWindow;
    private bool _isDragAnimationActive;
    private bool _isScreenLapActive;
    private bool _dragStartedOnPet;
    private bool _isWalkSpeechVisible;
    private bool _isAnimationRenderingSubscribed;
    private bool _isExitRequested;
    private ToolPanelKind _activeToolPanel = ToolPanelKind.None;

    private const double WalkPixelsPerSecond = 36;
    private const double MinWalkPixelsPerSecond = 22;
    private const double MaxWalkPixelsPerSecond = 64;
    private const double ScreenLapPixelsPerSecond = 120;
    private const double ScreenLapTargetTolerance = 4;
    private const double BaseWindowWidth = 340;
    private const double BaseWindowHeight = 390;
    private const double BasePetImageSize = 300;
    private const double BaseSpeechBubbleWidth = 230;
    private const double BaseSpeechBubbleHeight = 96;
    private const double ToolPanelColumnWidth = 324;
    private const double DragThreshold = 4;
    private const double WindowPositionEpsilon = 0.1;
    private const int ClipboardWriteMaxAttempts = 6;
    private const int ClipboardWriteRetryDelayMilliseconds = 25;
    private static readonly TimeSpan InteractionAnimationDuration = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan BubbleVisibleDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan PeriodicSpeechInterval = TimeSpan.FromSeconds(30);
    private const string WalkSpeechText = "（桌面巡逻中）";
    private const string AngrySpeechText = "人，再敲打你哦！";
    private const string EatSpeechText = "开饭啦！";

    public MainWindow()
    {
        _settingsStore = new JsonSettingsStore(
            Path.Combine(AppContext.BaseDirectory, "settings.json"));
        _settings = _settingsStore.Load();
        _dialogueLineProvider = new DialogueLineProvider(
            Path.Combine(AppContext.BaseDirectory, "Assets", "dialogues", "lines.json"));
        _petRepository = new SqlitePetRepository(LuluPetDataPaths.GetDefaultDatabasePath());
        _clipboardHistory = new ClipboardHistory(_settings.ToolPanels.ClipboardHistoryLimit);
        _clipboardHistoryStore = new ClipboardHistoryStore(LuluPetDataPaths.GetDefaultClipboardHistoryPath());
        _clipboardHistory.ReplaceWith(_clipboardHistoryStore.Load());
        _fileTransitService = new FileTransitService(_settings.ToolPanels.FileTransitFolderPath);
        _reminderScheduler = new ReminderScheduler(_settings.Reminders);

        InitializeComponent();
        _desktopBoundsProvider = new WpfDesktopBoundsProvider(this);
        ApplyWindowIcon();
        _showMenuItem = new Forms.ToolStripMenuItem("显示", null, (_, _) => ShowPetWindow());
        _hideMenuItem = new Forms.ToolStripMenuItem("隐藏", null, (_, _) => HidePetWindow());
        _settingsMenuItem = new Forms.ToolStripMenuItem("设置", null, (_, _) => ShowTrayToolPanel(ToolPanelKind.Settings));
        _tutorialMenuItem = new Forms.ToolStripMenuItem("教程", null, (_, _) => ShowTrayToolPanel(ToolPanelKind.Tutorial));
        _clickThroughMenuItem = new Forms.ToolStripMenuItem("点击穿透", null, (_, _) => ToggleClickThrough())
        {
            CheckOnClick = false
        };
        _autoStartMenuItem = new Forms.ToolStripMenuItem("开机启动", null, (_, _) => ToggleAutoStart())
        {
            CheckOnClick = false
        };
        _notifyIcon = CreateNotifyIcon();

        InitializeStorage();
        InitializeCompanionTime();
        RestoreWindowPosition();
        ApplyVisualSettings();
        InitializeAnimation();
        InitializeBehavior();
        InitializeSpeech();
        InitializeReminderPanel();
        InitializeClipboardPanel();
        InitializeFileTransitPanel();
        InitializeSettingsPanel();
        InitializeTutorialPanel();
        InitializeWin32();
        UpdateTrayMenuState();
    }

    protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        if (e.Handled)
        {
            return;
        }

        if (IsFromToolPanel(e.OriginalSource))
        {
            return;
        }

        if (e.ClickCount >= 2 && IsFromPetImage(e.OriginalSource))
        {
            HandlePetDoubleClick();
            e.Handled = true;
            return;
        }

        BeginPotentialDrag(e);
        e.Handled = true;
    }

    protected override void OnMouseRightButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);

        if (e.Handled || !IsFromPetImage(e.OriginalSource))
        {
            return;
        }

        TriggerEat();
        e.Handled = true;
    }

    protected override void OnMouseMove(System.Windows.Input.MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!_isPotentialDrag || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var currentScreenPoint = PointToScreen(e.GetPosition(this));
        var deltaX = currentScreenPoint.X - _dragStartScreenPoint.X;
        var deltaY = currentScreenPoint.Y - _dragStartScreenPoint.Y;

        if (!_isDraggingWindow
            && Math.Abs(deltaX) < DragThreshold
            && Math.Abs(deltaY) < DragThreshold)
        {
            return;
        }

        _isDraggingWindow = true;
        UpdatePetFacing(deltaX);
        StartDragAnimation();

        var candidateLeft = _dragStartLeft + deltaX;
        var candidateTop = _dragStartTop + deltaY;
        var movementBounds = GetWindowMovementBounds(candidateLeft, candidateTop, GetWindowWidth(), GetWindowHeight());
        SetWindowPosition(
            movementBounds.ClampLeft(candidateLeft),
            movementBounds.ClampTop(candidateTop));
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (!_isPotentialDrag)
        {
            return;
        }

        ReleaseMouseCapture();

        if (_isDraggingWindow)
        {
            SaveWindowPosition();
            StopDragAnimation();
        }
        else if (_dragStartedOnPet)
        {
            HandlePetClick();
        }

        _isPotentialDrag = false;
        _isDraggingWindow = false;
        _dragStartedOnPet = false;
        e.Handled = true;
    }

    protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.I)
        {
            _stateMachine.ForceState(PetState.Idle);
            PlayStateAnimation(PetState.Idle);
        }

        if (e.Key == Key.W)
        {
            var wasWalking = _stateMachine.CurrentState == PetState.Walk;
            _stateMachine.ForceState(PetState.Walk);
            PlayStateAnimation(PetState.Walk);

            if (wasWalking)
            {
                BeginWalkMotion();
                ShowSpeech(WalkSpeechText, autoHide: false);
                _isWalkSpeechVisible = true;
            }
        }

        if (e.Key == Key.S)
        {
            _stateMachine.ForceState(PetState.Sleep);
            PlayStateAnimation(PetState.Sleep);
        }

        if (e.Key == Key.F)
        {
            ToggleToolPanel(ToolPanelKind.Reminder);
            e.Handled = true;
        }

        if (e.Key == Key.C)
        {
            ToggleToolPanel(ToolPanelKind.Clipboard);
            e.Handled = true;
        }

        if (e.Key == Key.D)
        {
            ToggleToolPanel(ToolPanelKind.FileTransit);
            e.Handled = true;
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExitRequested)
        {
            e.Cancel = true;
            HidePetWindow();
            return;
        }

        StopAnimationRendering();
        _stateTimer.Stop();
        _periodicSpeechTimer.Stop();
        _bubbleHideTimer.Stop();
        _reminderTimer.Stop();
        _companionTimer.Stop();
        _clipboardMonitor?.Dispose();
        SaveCompanionElapsed();
        SaveClipboardHistory();
        SaveWindowPosition();
        DisposeTrayIcon();
        base.OnClosing(e);
    }

    private Forms.NotifyIcon CreateNotifyIcon()
    {
        var exitMenuItem = new Forms.ToolStripMenuItem("退出", null, (_, _) => ExitApplication());
        var contextMenu = new Forms.ContextMenuStrip();
        contextMenu.Items.Add(_showMenuItem);
        contextMenu.Items.Add(_hideMenuItem);
        contextMenu.Items.Add(_settingsMenuItem);
        contextMenu.Items.Add(_tutorialMenuItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(_clickThroughMenuItem);
        contextMenu.Items.Add(_autoStartMenuItem);
        contextMenu.Items.Add(new Forms.ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);

        var notifyIcon = new Forms.NotifyIcon
        {
            ContextMenuStrip = contextMenu,
            Icon = LoadTrayIcon(),
            Text = "LuluPet",
            Visible = true
        };

        notifyIcon.DoubleClick += (_, _) => ShowPetWindow();
        return notifyIcon;
    }

    private Icon LoadTrayIcon()
    {
        var executableIcon = TryLoadIconFromExecutable();
        if (executableIcon is not null)
        {
            return executableIcon;
        }

        var iconPath = FindFirstExistingIconPath("tray_light.ico", "app.ico");
        if (iconPath is null)
        {
            return SystemIcons.Application;
        }

        try
        {
            return new Icon(iconPath);
        }
        catch (ArgumentException)
        {
            return SystemIcons.Application;
        }
        catch (IOException)
        {
            return SystemIcons.Application;
        }
    }

    private static Icon? TryLoadIconFromExecutable()
    {
        var executablePath = Environment.ProcessPath
            ?? Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrWhiteSpace(executablePath) || !File.Exists(executablePath))
        {
            return null;
        }

        try
        {
            return System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
        }
        catch (Exception exception) when (exception is ArgumentException
            or IOException
            or UnauthorizedAccessException
            or InvalidOperationException
            or Win32Exception)
        {
            Debug.WriteLine($"Failed to extract tray icon from executable '{executablePath}': {exception.Message}");
            return null;
        }
    }

    private static string? FindFirstExistingIconPath(params string[] fileNames)
    {
        foreach (var fileName in fileNames)
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icons", fileName);
            if (File.Exists(iconPath))
            {
                return iconPath;
            }
        }

        return null;
    }

    private void ApplyWindowIcon()
    {
        var iconPath = FindFirstExistingIconPath("app.ico");
        if (iconPath is null)
        {
            return;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(iconPath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();
            Icon = image;
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or NotSupportedException
            or InvalidOperationException)
        {
            Debug.WriteLine($"Failed to load window icon '{iconPath}': {exception.Message}");
        }
    }

    private void ShowPetWindow()
    {
        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Keyboard.Focus(this);
        ApplyWindowStyles();

        if (_animationPlayer.IsPlaying)
        {
            StartAnimationRendering();
        }

        _lastStateTick = DateTimeOffset.UtcNow;
        _stateTimer.Start();
        _periodicSpeechTimer.Start();
        UpdateTrayMenuState();
    }

    private void HidePetWindow()
    {
        SaveWindowPosition();
        StopAnimationRendering();
        _stateTimer.Stop();
        _periodicSpeechTimer.Stop();
        _bubbleHideTimer.Stop();
        HideSpeechBubble();
        Hide();
        UpdateTrayMenuState();
    }

    private void ExitApplication()
    {
        _isExitRequested = true;
        Close();
    }

    private void UpdateTrayMenuState()
    {
        _showMenuItem.Enabled = !IsVisible;
        _hideMenuItem.Enabled = IsVisible;
        _clickThroughMenuItem.Checked = _settings.Interaction.ClickThrough;
        _autoStartMenuItem.Checked = _settings.Startup.AutoStart;
        SettingsPanel.ApplySettings(_settings);
        UpdateCompanionTrayText();
    }

    private void DisposeTrayIcon()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private void InitializeAnimation()
    {
        _animationPlayer.FrameChanged += (_, args) => SetPetFrame(args.FramePath);

        LoadAction("Idle", "idle", fps: 8);
        LoadAction("Walk", "walk", fps: 12);
        LoadAction("Sleep", "sleep", fps: 8);
        LoadAction("Happy", "happy", fps: 10);
        LoadAction("Eat", "eat", fps: 10);
        LoadAction("Drag", "drag", fps: 12);
        LoadAction("Angry", "angry", fps: 10);
        LoadAction("Surprised", "surprised", fps: 10);

        _lastAnimationTick = DateTimeOffset.UtcNow;

        if (TryPlayAction("Idle") || TryPlayAction("Walk") || TryPlayAction("Sleep"))
        {
            StartAnimationRendering();
        }
        else
        {
            MissingAssetFallback.Visibility = Visibility.Visible;
        }

        Loaded += (_, _) => Keyboard.Focus(this);
    }

    private void InitializeBehavior()
    {
        _stateMachine.StateChanged += (_, args) => OnPetStateChanged(args);

        _stateTimer.Interval = TimeSpan.FromMilliseconds(100);
        _stateTimer.Tick += (_, _) => TickBehavior();
        _lastStateTick = DateTimeOffset.UtcNow;
        _stateTimer.Start();

        if (_initialPetState != PetState.Idle)
        {
            _stateMachine.ForceState(_initialPetState);
            PlayStateAnimation(_initialPetState);
        }
    }

    private void InitializeSpeech()
    {
        SpeechBubble.TrySetBackgroundImage(
            Path.Combine(AppContext.BaseDirectory, "Assets", "ui", "bubble_default.png"));

        _periodicSpeechTimer.Interval = PeriodicSpeechInterval;
        _periodicSpeechTimer.Tick += (_, _) => ShowRandomSpeech();
        _periodicSpeechTimer.Start();

        _bubbleHideTimer.Interval = BubbleVisibleDuration;
        _bubbleHideTimer.Tick += (_, _) => HideSpeechBubble();
    }

    private void InitializeReminderPanel()
    {
        ReminderPanel.ApplySettings(_settings.Reminders);
        ReminderPanel.CloseRequested += (_, _) => HideActiveToolPanel();
        ReminderPanel.PomodoroEnabledChanged += (_, value) =>
        {
            _settings.Reminders.PomodoroEnabled = value;
            var reminderEvent = _reminderScheduler.SetPomodoroEnabled(value);
            SaveReminderSettings(applyToScheduler: false);
            if (reminderEvent is not null)
            {
                HandleReminderEvent(reminderEvent);
            }
        };
        ReminderPanel.PomodoroMinutesChanged += (_, value) =>
        {
            _settings.Reminders.PomodoroMinutes = value;
            SaveReminderSettings();
        };
        ReminderPanel.PomodoroRestMinutesChanged += (_, value) =>
        {
            _settings.Reminders.PomodoroRestMinutes = value;
            SaveReminderSettings();
        };
        ReminderPanel.WaterReminderEnabledChanged += (_, value) =>
        {
            _settings.Reminders.WaterReminderEnabled = value;
            SaveReminderSettings();
        };
        ReminderPanel.WaterReminderMinutesChanged += (_, value) =>
        {
            _settings.Reminders.WaterReminderMinutes = value;
            SaveReminderSettings();
        };
        ReminderPanel.StandReminderEnabledChanged += (_, value) =>
        {
            _settings.Reminders.StandReminderEnabled = value;
            SaveReminderSettings();
        };
        ReminderPanel.StandReminderMinutesChanged += (_, value) =>
        {
            _settings.Reminders.StandReminderMinutes = value;
            SaveReminderSettings();
        };

        _reminderTimer.Interval = TimeSpan.FromSeconds(1);
        _reminderTimer.Tick += (_, _) => TickReminders();
        _lastReminderTick = DateTimeOffset.UtcNow;
        _reminderTimer.Start();
        UpdateReminderPanelState();
    }

    private void InitializeClipboardPanel()
    {
        ClipboardPanel.CloseRequested += (_, _) => HideActiveToolPanel();
        ClipboardPanel.TextSelected += (_, text) => CopyClipboardHistoryText(text);
        UpdateClipboardPanelState();
    }

    private void InitializeFileTransitPanel()
    {
        FileTransitPanel.CloseRequested += (_, _) => HideActiveToolPanel();
        FileTransitPanel.OpenFolderRequested += (_, _) => OpenFileTransitFolder();
        FileTransitPanel.FilesDropped += (_, paths) => AddFilesToTransit(paths);
        FileTransitPanel.PasteRequested += (_, _) => PasteClipboardToTransit();
        FileTransitPanel.FileOpenRequested += (_, path) => OpenFileTransitFile(path);
        FileTransitPanel.FileCopyRequested += (_, path) => CopyFileTransitFile(path);
        UpdateFileTransitPanelState();
    }

    private void InitializeSettingsPanel()
    {
        SettingsPanel.CloseRequested += (_, _) => HideActiveToolPanel();
        SettingsPanel.ScaleChanged += value =>
        {
            _settings.Appearance.Scale = value;
            ApplyVisualSettings();
            SaveSettings();
        };
        SettingsPanel.OpacityChanged += value =>
        {
            _settings.Appearance.Opacity = value;
            ApplyVisualSettings();
            SaveSettings();
        };
        SettingsPanel.VolumeChanged += value =>
        {
            _settings.Audio.Volume = value;
            SaveSettings();
        };
        SettingsPanel.ClickThroughChanged += SetClickThrough;
        SettingsPanel.AutoStartChanged += SetAutoStart;
        SettingsPanel.FileTransitFolderChanged += SetFileTransitFolder;
        SettingsPanel.ApplySettings(_settings);
    }

    private void InitializeTutorialPanel()
    {
        TutorialPanel.CloseRequested += (_, _) => HideActiveToolPanel();
    }

    private void InitializeCompanionTime()
    {
        var localDate = GetCurrentLocalDate();
        var totalSeconds = 0L;

        try
        {
            totalSeconds = _petRepository.LoadCompanionDayStats(localDate).TotalSeconds;
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Failed to load companion time stats: {exception.Message}");
        }

        _companionTracker = new CompanionTimeTracker(localDate, totalSeconds);
        _lastCompanionTick = DateTimeOffset.UtcNow;
        UpdateCompanionTrayText();

        _companionTimer.Interval = TimeSpan.FromMinutes(1);
        _companionTimer.Tick += (_, _) => TickCompanionTime();
        _companionTimer.Start();
    }

    private void InitializeWin32()
    {
        SourceInitialized += (_, _) =>
        {
            _windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            ApplyWindowStyles();
            StartClipboardMonitor();
            ClampWindowToWorkArea();
        };

        LocationChanged += (_, _) =>
        {
            if (!_isApplyingVisualSettings && !_isVisualSettingsLayoutPending)
            {
                ClampWindowToWorkArea();
            }
        };
        SizeChanged += (_, _) =>
        {
            if (!_isApplyingVisualSettings && !_isVisualSettingsLayoutPending)
            {
                ClampWindowToWorkArea();
            }
        };
        Loaded += (_, _) =>
        {
            ClampWindowToWorkArea();
            ApplyAutoStartSetting();
            UpdateTrayMenuState();
        };
    }

    private void LoadAction(string actionName, string directoryName, int fps)
    {
        var directoryPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "pet",
            directoryName);

        var frames = AnimationFrameLoader.LoadPngFrames(directoryPath);
        if (frames.Count == 0)
        {
            Debug.WriteLine($"Animation action '{actionName}' has no frames in {directoryPath}.");
            return;
        }

        foreach (var framePath in frames)
        {
            _ = TryGetPetFrame(framePath, out _);
        }

        _animationPlayer.LoadAction(actionName, frames, fps);
    }

    private bool TryPlayAction(string actionName)
    {
        if (!_animationPlayer.CanPlay(actionName))
        {
            return false;
        }

        _animationPlayer.Play(actionName);
        return true;
    }

    private void StartAnimationRendering()
    {
        if (_isAnimationRenderingSubscribed)
        {
            return;
        }

        _lastAnimationTick = DateTimeOffset.UtcNow;
        _lastAnimationRenderingTime = null;
        CompositionTarget.Rendering += CompositionTarget_Rendering;
        _isAnimationRenderingSubscribed = true;
    }

    private void StopAnimationRendering()
    {
        if (!_isAnimationRenderingSubscribed)
        {
            return;
        }

        CompositionTarget.Rendering -= CompositionTarget_Rendering;
        _lastAnimationRenderingTime = null;
        _isAnimationRenderingSubscribed = false;
    }

    private void CompositionTarget_Rendering(object? sender, EventArgs e)
    {
        if (e is RenderingEventArgs renderingArgs)
        {
            var renderingTime = renderingArgs.RenderingTime;
            if (_lastAnimationRenderingTime is null)
            {
                _lastAnimationRenderingTime = renderingTime;
                return;
            }

            var elapsed = renderingTime - _lastAnimationRenderingTime.Value;
            _lastAnimationRenderingTime = renderingTime;
            _animationPlayer.Tick(elapsed);
            return;
        }

        TickAnimation();
    }

    private void TickAnimation()
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = now - _lastAnimationTick;
        _lastAnimationTick = now;
        _animationPlayer.Tick(elapsed);
    }

    private void TickBehavior()
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = now - _lastStateTick;
        _lastStateTick = now;

        _stateMachine.Tick(elapsed);

        if (_isScreenLapActive)
        {
            MoveAlongScreenLap(elapsed);
        }
        else if (_stateMachine.CurrentState == PetState.Walk)
        {
            MoveWhileWalking(elapsed);
        }

        if (_interactionAnimationUntil is not null && now >= _interactionAnimationUntil.Value)
        {
            _interactionAnimationUntil = null;
            PlayStateAnimation(_stateMachine.CurrentState);
        }
    }

    private void TickReminders()
    {
        var now = DateTimeOffset.UtcNow;
        var elapsed = now - _lastReminderTick;
        _lastReminderTick = now;

        foreach (var reminderEvent in _reminderScheduler.Tick(elapsed))
        {
            HandleReminderEvent(reminderEvent);
        }

        UpdateReminderPanelState();
    }

    private void TickCompanionTime()
    {
        SaveCompanionElapsed();
        UpdateCompanionTrayText();
    }

    private void SaveCompanionElapsed()
    {
        if (_companionTracker is null)
        {
            return;
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var elapsed = nowUtc - _lastCompanionTick;
        _lastCompanionTick = nowUtc;
        var localDate = GetCurrentLocalDate();

        if (localDate != _companionTracker.LocalDate)
        {
            var existingTotalSeconds = 0L;

            try
            {
                existingTotalSeconds = _petRepository.LoadCompanionDayStats(localDate).TotalSeconds;
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"Failed to load new companion day stats: {exception.Message}");
            }

            _companionTracker.Reset(localDate, existingTotalSeconds);
        }

        var addedSeconds = _companionTracker.Tick(elapsed, localDate);
        if (addedSeconds <= 0)
        {
            return;
        }

        try
        {
            _petRepository.AddCompanionSeconds(localDate, addedSeconds, nowUtc);
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Failed to save companion time stats: {exception.Message}");
        }
    }

    private void HandleReminderEvent(ReminderEvent reminderEvent)
    {
        RecordInteraction(reminderEvent.InteractionType, reminderEvent.Message);

        if (ShouldWalkScreenLap(reminderEvent.Kind))
        {
            ShowSpeech(reminderEvent.Message, autoHide: false);
            BeginScreenLap(reminderEvent.Message);
        }
        else
        {
            ShowSpeech(reminderEvent.Message);
            PlayReminderAnimation(reminderEvent.PreferredAction);
        }

        UpdateReminderPanelState();
    }

    private void PlayReminderAnimation(string preferredAction)
    {
        _interactionAnimationUntil = DateTimeOffset.UtcNow + InteractionAnimationDuration;

        if (TryPlayAction(preferredAction)
            || TryPlayAction("Happy")
            || TryPlayAction("Idle"))
        {
            return;
        }

        PlayStateAnimation(_stateMachine.CurrentState);
    }

    private void OnPetStateChanged(PetStateChangedEventArgs args)
    {
        Debug.WriteLine($"LuluPet state: {args.PreviousState} -> {args.CurrentState}");
        PlayStateAnimation(args.CurrentState);
        SavePetStatus();

        if (args.CurrentState == PetState.Walk)
        {
            BeginWalkMotion();
            ShowSpeech(WalkSpeechText, autoHide: false);
            _isWalkSpeechVisible = true;
        }
        else if (args.PreviousState == PetState.Walk && _isWalkSpeechVisible)
        {
            HideSpeechBubble();
        }
    }

    private void PlayStateAnimation(PetState state)
    {
        if (_isDragAnimationActive || _isScreenLapActive || IsInteractionAnimationActive())
        {
            return;
        }

        if (TryPlayAction(state.ToString()))
        {
            return;
        }

        if (state != PetState.Idle)
        {
            TryPlayAction(PetState.Idle.ToString());
        }
    }

    private void MoveWhileWalking(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return;
        }

        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var nextLeft = Left + _walkVelocityX * elapsed.TotalSeconds;
        var nextTop = Top + _walkVelocityY * elapsed.TotalSeconds;
        var movementBounds = GetWindowMovementBounds(nextLeft, nextTop, width, height);
        var reachedBoundary = false;

        if (nextLeft <= movementBounds.Left)
        {
            nextLeft = movementBounds.Left;
            reachedBoundary = true;
        }
        else if (nextLeft >= movementBounds.Right)
        {
            nextLeft = movementBounds.Right;
            reachedBoundary = true;
        }

        if (nextTop <= movementBounds.Top)
        {
            nextTop = movementBounds.Top;
            reachedBoundary = true;
        }
        else if (nextTop >= movementBounds.Bottom)
        {
            nextTop = movementBounds.Bottom;
            reachedBoundary = true;
        }

        UpdatePetFacing(_walkVelocityX);
        Left = nextLeft;
        Top = nextTop;

        if (reachedBoundary)
        {
            _stateMachine.ForceState(PetState.Idle);
        }
        else
        {
            ClampWindowToWorkArea(Left, Top);
        }
    }

    private void BeginScreenLap(string speechText)
    {
        if (!TryBuildScreenLapTargets(out var targets))
        {
            ShowSpeech(speechText);
            PlayReminderAnimation("Happy");
            return;
        }

        _isPotentialDrag = false;
        _isDraggingWindow = false;
        _isDragAnimationActive = false;
        _dragStartedOnPet = false;
        ReleaseMouseCapture();

        _screenLapTargets.Clear();
        _screenLapTargets.AddRange(targets);
        _screenLapTargetIndex = 0;
        _screenLapSpeechText = speechText;
        _isScreenLapActive = true;
        _interactionAnimationUntil = null;
        _stateMachine.ForceState(PetState.Idle);
        ShowSpeech(speechText, autoHide: false);

        if (!TryPlayAction(PetState.Walk.ToString()))
        {
            TryPlayAction(PetState.Idle.ToString());
        }

        UpdatePetFacing(_screenLapTargets[0].X - Left);
    }

    private void MoveAlongScreenLap(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero || _screenLapTargetIndex >= _screenLapTargets.Count)
        {
            return;
        }

        var target = _screenLapTargets[_screenLapTargetIndex];
        if (!string.IsNullOrWhiteSpace(_screenLapSpeechText))
        {
            ShowSpeech(_screenLapSpeechText, autoHide: false);
        }

        var deltaX = target.X - Left;
        var deltaY = target.Y - Top;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        var step = ScreenLapPixelsPerSecond * elapsed.TotalSeconds;

        if (distance <= Math.Max(step, ScreenLapTargetTolerance))
        {
            Left = target.X;
            Top = target.Y;
            _screenLapTargetIndex++;

            if (_screenLapTargetIndex >= _screenLapTargets.Count)
            {
                EndScreenLap();
                return;
            }

            var nextTarget = _screenLapTargets[_screenLapTargetIndex];
            UpdatePetFacing(nextTarget.X - Left);
            return;
        }

        var ratio = step / distance;
        Left += deltaX * ratio;
        Top += deltaY * ratio;
        UpdatePetFacing(deltaX);
    }

    private void EndScreenLap()
    {
        var speechText = _screenLapSpeechText;
        _screenLapTargets.Clear();
        _screenLapTargetIndex = 0;
        _screenLapSpeechText = null;
        _isScreenLapActive = false;
        _stateMachine.ForceState(PetState.Idle);
        PlayStateAnimation(_stateMachine.CurrentState);

        if (!string.IsNullOrWhiteSpace(speechText))
        {
            ShowSpeech(speechText);
        }
    }

    private bool TryBuildScreenLapTargets(out IReadOnlyList<System.Windows.Point> targets)
    {
        var bounds = GetCurrentScreenMovementBounds();
        var left = bounds.Left;
        var top = bounds.Top;
        var right = bounds.Right;
        var bottom = bounds.Bottom;

        if (right <= left || bottom <= top)
        {
            targets = Array.Empty<System.Windows.Point>();
            return false;
        }

        var currentX = Math.Clamp(Left, left, right);
        var currentY = Math.Clamp(Top, top, bottom);
        var nearestEdge = GetNearestEdge(currentX, currentY, bounds);
        var lapTargets = new List<System.Windows.Point>(capacity: 6);

        switch (nearestEdge)
        {
            case ScreenEdge.Left:
                AddScreenLapTarget(lapTargets, left, currentY);
                AddScreenLapTarget(lapTargets, left, top);
                AddScreenLapTarget(lapTargets, right, top);
                AddScreenLapTarget(lapTargets, right, bottom);
                AddScreenLapTarget(lapTargets, left, bottom);
                AddScreenLapTarget(lapTargets, left, currentY);
                break;
            case ScreenEdge.Top:
                AddScreenLapTarget(lapTargets, currentX, top);
                AddScreenLapTarget(lapTargets, right, top);
                AddScreenLapTarget(lapTargets, right, bottom);
                AddScreenLapTarget(lapTargets, left, bottom);
                AddScreenLapTarget(lapTargets, left, top);
                AddScreenLapTarget(lapTargets, currentX, top);
                break;
            case ScreenEdge.Right:
                AddScreenLapTarget(lapTargets, right, currentY);
                AddScreenLapTarget(lapTargets, right, bottom);
                AddScreenLapTarget(lapTargets, left, bottom);
                AddScreenLapTarget(lapTargets, left, top);
                AddScreenLapTarget(lapTargets, right, top);
                AddScreenLapTarget(lapTargets, right, currentY);
                break;
            case ScreenEdge.Bottom:
                AddScreenLapTarget(lapTargets, currentX, bottom);
                AddScreenLapTarget(lapTargets, left, bottom);
                AddScreenLapTarget(lapTargets, left, top);
                AddScreenLapTarget(lapTargets, right, top);
                AddScreenLapTarget(lapTargets, right, bottom);
                AddScreenLapTarget(lapTargets, currentX, bottom);
                break;
        }

        targets = lapTargets;
        return lapTargets.Count > 0;
    }

    private MovementBounds GetWindowMovementBounds()
    {
        return GetWindowMovementBounds(Left, Top, GetWindowWidth(), GetWindowHeight());
    }

    private MovementBounds GetWindowMovementBounds(double width, double height)
    {
        return GetWindowMovementBounds(Left, Top, width, height);
    }

    private MovementBounds GetWindowMovementBounds(
        double candidateLeft,
        double candidateTop,
        double width,
        double height)
    {
        return DesktopBounds.GetNearestMovementBounds(
            _desktopBoundsProvider.GetScreenBounds(),
            candidateLeft,
            candidateTop,
            width,
            height);
    }

    private MovementBounds GetCurrentScreenMovementBounds()
    {
        var width = GetWindowWidth();
        var height = GetWindowHeight();
        var centerX = Left + width / 2;
        var centerY = Top + height / 2;
        return _desktopBoundsProvider
            .GetBoundsForPoint(centerX, centerY)
            .GetMovementBounds(width, height);
    }

    private double GetWindowWidth()
    {
        return ActualWidth > 0 ? ActualWidth : Width;
    }

    private double GetWindowHeight()
    {
        return ActualHeight > 0 ? ActualHeight : Height;
    }

    private static ScreenEdge GetNearestEdge(
        double x,
        double y,
        MovementBounds bounds)
    {
        var nearestEdge = ScreenEdge.Left;
        var nearestDistance = Math.Abs(x - bounds.Left);

        var topDistance = Math.Abs(y - bounds.Top);
        if (topDistance < nearestDistance)
        {
            nearestEdge = ScreenEdge.Top;
            nearestDistance = topDistance;
        }

        var rightDistance = Math.Abs(bounds.Right - x);
        if (rightDistance < nearestDistance)
        {
            nearestEdge = ScreenEdge.Right;
            nearestDistance = rightDistance;
        }

        var bottomDistance = Math.Abs(bounds.Bottom - y);
        if (bottomDistance < nearestDistance)
        {
            nearestEdge = ScreenEdge.Bottom;
        }

        return nearestEdge;
    }

    private static void AddScreenLapTarget(List<System.Windows.Point> targets, double x, double y)
    {
        var point = new System.Windows.Point(x, y);
        if (targets.Count > 0)
        {
            var previous = targets[^1];
            if (Math.Abs(previous.X - point.X) < 0.1 && Math.Abs(previous.Y - point.Y) < 0.1)
            {
                return;
            }
        }

        targets.Add(point);
    }

    private static bool ShouldWalkScreenLap(ReminderKind reminderKind)
    {
        return reminderKind is ReminderKind.PomodoroFocusDone
            or ReminderKind.PomodoroRestDone
            or ReminderKind.Water
            or ReminderKind.Stand;
    }

    private void BeginWalkMotion()
    {
        ChooseWalkMotion();
    }

    private void ChooseWalkMotion()
    {
        var x = Random.Shared.NextDouble() * 2 - 1;
        if (Math.Abs(x) < 0.25)
        {
            x = Math.CopySign(0.25, x == 0 ? 1 : x);
        }

        var y = (Random.Shared.NextDouble() * 2 - 1) * 0.55;
        var length = Math.Sqrt(x * x + y * y);
        var speed = MinWalkPixelsPerSecond
            + Random.Shared.NextDouble() * (MaxWalkPixelsPerSecond - MinWalkPixelsPerSecond);

        _walkVelocityX = x / length * speed;
        _walkVelocityY = y / length * speed;
    }

    private void BeginPotentialDrag(System.Windows.Input.MouseButtonEventArgs e)
    {
        _isPotentialDrag = true;
        _isDraggingWindow = false;
        _dragStartedOnPet = IsFromPetImage(e.OriginalSource);
        _dragStartScreenPoint = PointToScreen(e.GetPosition(this));
        _dragStartLeft = Left;
        _dragStartTop = Top;
        CaptureMouse();
    }

    private void StartDragAnimation()
    {
        if (_isDragAnimationActive)
        {
            return;
        }

        if (TryPlayAction("Drag"))
        {
            _isDragAnimationActive = true;
        }
    }

    private void StopDragAnimation()
    {
        if (!_isDragAnimationActive)
        {
            return;
        }

        _isDragAnimationActive = false;
        PlayStateAnimation(_stateMachine.CurrentState);
    }

    private void HandlePetClick()
    {
        _stateMachine.ForceState(PetState.Idle);
        _interactionAnimationUntil = DateTimeOffset.UtcNow + InteractionAnimationDuration;

        PlayRandomClickAnimation();

        var message = ShowRandomSpeech();
        RecordInteraction("click", message);
    }

    private void PlayRandomClickAnimation()
    {
        var primaryAction = Random.Shared.Next(2) == 0 ? "Happy" : "Surprised";
        var fallbackAction = primaryAction == "Happy" ? "Surprised" : "Happy";

        if (!TryPlayAction(primaryAction) && !TryPlayAction(fallbackAction))
        {
            TryPlayAction("Idle");
        }
    }

    private void TriggerEat()
    {
        _stateMachine.ForceState(PetState.Idle);
        _interactionAnimationUntil = DateTimeOffset.UtcNow + InteractionAnimationDuration;

        if (!TryPlayAction("Eat"))
        {
            TryPlayAction("Happy");
        }

        var message = ShowSpeech(EatSpeechText);
        RecordInteraction("feed", message);
    }

    private void HandlePetDoubleClick()
    {
        _isPotentialDrag = false;
        _isDraggingWindow = false;
        _isDragAnimationActive = false;
        _dragStartedOnPet = false;
        ReleaseMouseCapture();

        _stateMachine.ForceState(PetState.Idle);
        _interactionAnimationUntil = DateTimeOffset.UtcNow + InteractionAnimationDuration;

        if (!TryPlayAction("Angry") && !TryPlayAction("Surprised"))
        {
            TryPlayAction("Idle");
        }

        var message = ShowSpeech(AngrySpeechText);
        RecordInteraction("double_click", message);
    }

    private bool IsInteractionAnimationActive()
    {
        return _interactionAnimationUntil is not null
            && DateTimeOffset.UtcNow < _interactionAnimationUntil.Value;
    }

    private string? ShowRandomSpeech()
    {
        var line = _dialogueLineProvider.GetRandomLine();
        if (string.IsNullOrWhiteSpace(line) || IsReservedSpeech(line))
        {
            return null;
        }

        return ShowSpeech(line);
    }

    private static bool IsReservedSpeech(string line)
    {
        var normalizedLine = line.Trim();
        return string.Equals(normalizedLine, WalkSpeechText, StringComparison.Ordinal)
            || string.Equals(normalizedLine, AngrySpeechText, StringComparison.Ordinal)
            || string.Equals(normalizedLine, EatSpeechText, StringComparison.Ordinal);
    }

    private string ShowSpeech(string line, bool autoHide = true)
    {
        _isWalkSpeechVisible = !autoHide && line == WalkSpeechText;
        SpeechBubble.SetText(line);
        SpeechBubble.Visibility = Visibility.Visible;

        _bubbleHideTimer.Stop();
        if (autoHide)
        {
            _bubbleHideTimer.Start();
        }

        return line;
    }

    private void HideSpeechBubble()
    {
        _bubbleHideTimer.Stop();
        _isWalkSpeechVisible = false;
        SpeechBubble.Visibility = Visibility.Collapsed;
    }

    private void ToggleToolPanel(ToolPanelKind panel)
    {
        if (_settings.Interaction.ClickThrough)
        {
            return;
        }

        if (_activeToolPanel == panel)
        {
            HideActiveToolPanel();
            return;
        }

        ShowToolPanel(panel);
    }

    private void ShowToolPanel(ToolPanelKind panel)
    {
        if (_settings.Interaction.ClickThrough)
        {
            return;
        }

        PrepareToolPanel(panel);
        HideAllToolPanels();
        HideSpeechBubble();

        var element = GetToolPanelElement(panel);
        element.Visibility = Visibility.Visible;
        _activeToolPanel = panel;
        ApplyVisualSettings();
        FocusToolPanel(panel);
    }

    private void HideActiveToolPanel()
    {
        if (_activeToolPanel == ToolPanelKind.None)
        {
            return;
        }

        HideAllToolPanels();
        _activeToolPanel = ToolPanelKind.None;
        ApplyVisualSettings();
        Keyboard.Focus(this);
    }

    private void HideAllToolPanels()
    {
        ReminderPanel.Visibility = Visibility.Collapsed;
        ClipboardPanel.Visibility = Visibility.Collapsed;
        FileTransitPanel.Visibility = Visibility.Collapsed;
        SettingsPanel.Visibility = Visibility.Collapsed;
        TutorialPanel.Visibility = Visibility.Collapsed;
    }

    private void PrepareToolPanel(ToolPanelKind panel)
    {
        if (panel != ToolPanelKind.Reminder)
        {
            if (panel == ToolPanelKind.Clipboard)
            {
                UpdateClipboardPanelState();
            }
            else if (panel == ToolPanelKind.FileTransit)
            {
                UpdateFileTransitPanelState();
            }
            else if (panel == ToolPanelKind.Settings)
            {
                SettingsPanel.ApplySettings(_settings);
            }

            return;
        }

        ReminderSettings.Normalize(_settings.Reminders);
        ReminderPanel.ApplySettings(_settings.Reminders);
        UpdateReminderPanelState();
    }

    private FrameworkElement GetToolPanelElement(ToolPanelKind panel)
    {
        return panel switch
        {
            ToolPanelKind.Reminder => ReminderPanel,
            ToolPanelKind.Clipboard => ClipboardPanel,
            ToolPanelKind.FileTransit => FileTransitPanel,
            ToolPanelKind.Settings => SettingsPanel,
            ToolPanelKind.Tutorial => TutorialPanel,
            _ => throw new ArgumentOutOfRangeException(nameof(panel), panel, null)
        };
    }

    private void FocusToolPanel(ToolPanelKind panel)
    {
        var element = GetToolPanelElement(panel);
        element.Focus();
        Keyboard.Focus(element);
    }

    private void UpdateReminderPanelState()
    {
        ReminderPanel.ApplyRuntimeState(
            _reminderScheduler.PomodoroPhase,
            _reminderScheduler.PomodoroRemaining,
            _reminderScheduler.WaterRemaining,
            _reminderScheduler.StandRemaining);
    }

    private void UpdateClipboardPanelState()
    {
        ClipboardPanel.ApplyHistory(_clipboardHistory.Snapshot(), _clipboardHistory.Capacity);
    }

    private void UpdateFileTransitPanelState()
    {
        try
        {
            FileTransitPanel.ApplySnapshot(_fileTransitService.LoadSnapshot());
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException)
        {
            Debug.WriteLine($"Failed to load file transit folder: {exception.Message}");
        }
    }

    private void OpenFileTransitFolder()
    {
        try
        {
            _fileTransitService.OpenFolder();
            UpdateFileTransitPanelState();
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or UnauthorizedAccessException
            or NotSupportedException
            or InvalidOperationException
            or System.Security.SecurityException)
        {
            Debug.WriteLine($"Failed to open file transit folder: {exception.Message}");
        }
    }

    private void OpenFileTransitFile(string path)
    {
        try
        {
            _fileTransitService.OpenFile(path);
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or UnauthorizedAccessException
            or NotSupportedException
            or InvalidOperationException
            or System.Security.SecurityException)
        {
            Debug.WriteLine($"Failed to open file transit file: {exception.Message}");
            FileTransitPanel.SetStatus("打开失败");
        }
    }

    private void CopyFileTransitFile(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                FileTransitPanel.SetStatus("文件不存在");
                return;
            }

            SetClipboardFileCopy(path);
            FileTransitPanel.SetStatus("已复制文件");
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or UnauthorizedAccessException
            or NotSupportedException
            or InvalidOperationException
            or ExternalException
            or System.Security.SecurityException)
        {
            Debug.WriteLine($"Failed to copy file transit file: {exception.Message}");
            FileTransitPanel.SetStatus("复制失败");
        }
    }

    private void AddFilesToTransit(IReadOnlyList<string> paths)
    {
        try
        {
            var copiedCount = _fileTransitService.AddFiles(paths);
            UpdateFileTransitPanelState();
            FileTransitPanel.SetStatus(copiedCount > 0
                ? $"已暂存 {copiedCount} 个文件"
                : "未发现可暂存的文件");
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or UnauthorizedAccessException
            or NotSupportedException
            or InvalidOperationException
            or System.Security.SecurityException)
        {
            Debug.WriteLine($"Failed to add files to transit folder: {exception.Message}");
            FileTransitPanel.SetStatus("暂存失败");
        }
    }

    private void PasteClipboardToTransit()
    {
        try
        {
            var filePaths = GetClipboardFileDropList();
            if (filePaths.Count > 0)
            {
                var copiedCount = _fileTransitService.AddFiles(filePaths);
                UpdateFileTransitPanelState();
                FileTransitPanel.SetStatus(copiedCount > 0
                    ? $"已粘贴 {copiedCount} 个文件"
                    : "未发现可粘贴的文件");
                return;
            }

            var imageBytes = GetClipboardImagePngBytes();
            if (imageBytes is not null)
            {
                AddClipboardImageToTransit(imageBytes);
                UpdateFileTransitPanelState();
                FileTransitPanel.SetStatus("已粘贴剪切板图片");
                return;
            }

            FileTransitPanel.SetStatus("剪切板没有可暂存内容");
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or UnauthorizedAccessException
            or NotSupportedException
            or InvalidOperationException
            or ExternalException
            or System.Security.SecurityException)
        {
            Debug.WriteLine($"Failed to paste clipboard content to file transit: {exception.Message}");
            FileTransitPanel.SetStatus("粘贴失败");
        }
    }

    private void AddClipboardImageToTransit(byte[] imageBytes)
    {
        var fileName = $"clipboard-image-{DateTime.Now:yyyyMMdd-HHmmss}.png";
        _fileTransitService.AddFileBytes(fileName, imageBytes);
    }

    private static IReadOnlyList<string> GetClipboardFileDropList()
    {
        try
        {
            return ReadClipboardWithRetry(ReadWpfClipboardFileDropList);
        }
        catch (Exception exception) when (exception is ExternalException or InvalidOperationException)
        {
            return ReadClipboardWithRetry(ReadFormsClipboardFileDropList);
        }
    }

    private static IReadOnlyList<string> ReadWpfClipboardFileDropList()
    {
        return WpfClipboard.ContainsFileDropList()
            ? NormalizeClipboardFileDropList(WpfClipboard.GetFileDropList())
            : Array.Empty<string>();
    }

    private static IReadOnlyList<string> ReadFormsClipboardFileDropList()
    {
        return Forms.Clipboard.ContainsFileDropList()
            ? NormalizeClipboardFileDropList(Forms.Clipboard.GetFileDropList())
            : Array.Empty<string>();
    }

    private static IReadOnlyList<string> NormalizeClipboardFileDropList(StringCollection fileDropList)
    {
        return fileDropList
            .Cast<string>()
            .Where(File.Exists)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static byte[]? GetClipboardImagePngBytes()
    {
        try
        {
            var imageBytes = ReadClipboardWithRetry(ReadFormsClipboardImagePngBytes);
            if (imageBytes is not null)
            {
                return imageBytes;
            }
        }
        catch (Exception exception) when (exception is ExternalException
            or InvalidOperationException
            or ArgumentException)
        {
            Debug.WriteLine($"Failed to read clipboard image through Windows Forms: {exception.Message}");
        }

        return ReadClipboardWithRetry(ReadWpfClipboardImagePngBytes);
    }

    private static byte[]? ReadFormsClipboardImagePngBytes()
    {
        if (!Forms.Clipboard.ContainsImage())
        {
            return null;
        }

        using var image = Forms.Clipboard.GetImage();
        if (image is null)
        {
            return null;
        }

        using var bitmap = new Bitmap(
            image.Width,
            image.Height,
            global::System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(System.Drawing.Color.Transparent);
            graphics.DrawImage(image, 0, 0, image.Width, image.Height);
        }

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static byte[]? ReadWpfClipboardImagePngBytes()
    {
        if (!WpfClipboard.ContainsImage())
        {
            return null;
        }

        var image = WpfClipboard.GetImage();
        if (image is null)
        {
            return null;
        }

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    private void CopyClipboardHistoryText(string text)
    {
        try
        {
            WriteClipboardWithRetry(() => WpfClipboard.SetText(text, WpfTextDataFormat.UnicodeText));
            ClipboardPanel.SetStatus("已复制到剪切板");
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or ExternalException)
        {
            Debug.WriteLine($"Failed to copy clipboard history text: {exception.Message}");
            ClipboardPanel.SetStatus("复制失败");
        }
    }

    private static void SetClipboardFileCopy(string path)
    {
        var fileDropList = new StringCollection
        {
            path
        };

        try
        {
            var dataObject = new Forms.DataObject();
            dataObject.SetFileDropList(fileDropList);
            dataObject.SetData(
                "Preferred DropEffect",
                new MemoryStream(BitConverter.GetBytes((int)Forms.DragDropEffects.Copy)));

            WriteClipboardWithRetry(() => Forms.Clipboard.SetDataObject(dataObject, true));
        }
        catch (Exception exception) when (exception is ExternalException
            or InvalidOperationException
            or ArgumentException
            or NotSupportedException)
        {
            WriteClipboardWithRetry(() => Forms.Clipboard.SetFileDropList(fileDropList));
        }
    }

    private static void WriteClipboardWithRetry(Action writeClipboard)
    {
        for (var attempt = 1; attempt <= ClipboardWriteMaxAttempts; attempt++)
        {
            try
            {
                writeClipboard();
                return;
            }
            catch (ExternalException) when (attempt < ClipboardWriteMaxAttempts)
            {
                System.Threading.Thread.Sleep(ClipboardWriteRetryDelayMilliseconds);
            }
            catch (InvalidOperationException) when (attempt < ClipboardWriteMaxAttempts)
            {
                System.Threading.Thread.Sleep(ClipboardWriteRetryDelayMilliseconds);
            }
        }
    }

    private static T ReadClipboardWithRetry<T>(Func<T> readClipboard)
    {
        for (var attempt = 1; attempt <= ClipboardWriteMaxAttempts; attempt++)
        {
            try
            {
                return readClipboard();
            }
            catch (ExternalException) when (attempt < ClipboardWriteMaxAttempts)
            {
                System.Threading.Thread.Sleep(ClipboardWriteRetryDelayMilliseconds);
            }
            catch (InvalidOperationException) when (attempt < ClipboardWriteMaxAttempts)
            {
                System.Threading.Thread.Sleep(ClipboardWriteRetryDelayMilliseconds);
            }
        }

        return readClipboard();
    }

    private void StartClipboardMonitor()
    {
        if (_clipboardMonitor is not null || _windowHandle == nint.Zero)
        {
            return;
        }

        _clipboardMonitor = new ClipboardMonitor();
        _clipboardMonitor.TextChanged += ClipboardMonitor_TextChanged;
        _clipboardMonitor.Start(_windowHandle);
    }

    private void ClipboardMonitor_TextChanged(object? sender, string text)
    {
        if (!_clipboardHistory.AddText(text, DateTimeOffset.Now))
        {
            return;
        }

        if (_activeToolPanel == ToolPanelKind.Clipboard)
        {
            UpdateClipboardPanelState();
        }

        SaveClipboardHistory();
    }

    private void SaveClipboardHistory()
    {
        try
        {
            _clipboardHistoryStore.Save(_clipboardHistory.Snapshot());
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or System.Security.SecurityException)
        {
            Debug.WriteLine($"Failed to save clipboard history: {exception.Message}");
        }
    }

    private void SetPetFrame(string framePath)
    {
        if (TryGetPetFrame(framePath, out var cachedImage))
        {
            PetImage.Source = cachedImage;
            MissingAssetFallback.Visibility = Visibility.Collapsed;
            return;
        }

        MissingAssetFallback.Visibility = Visibility.Visible;
    }

    private bool TryGetPetFrame(string framePath, out BitmapImage image)
    {
        image = default!;
        if (!File.Exists(framePath))
        {
            return false;
        }

        if (_frameCache.TryGetValue(framePath, out var cachedImage) && cachedImage is not null)
        {
            image = cachedImage;
            return true;
        }

        try
        {
            image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(framePath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();

            _frameCache[framePath] = image;
            return true;
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or NotSupportedException
            or InvalidOperationException)
        {
            Debug.WriteLine($"Failed to load pet frame '{framePath}': {exception.Message}");
            image = default!;
            return false;
        }
    }

    private void UpdatePetFacing(double horizontalDirection)
    {
        if (Math.Abs(horizontalDirection) < 0.1)
        {
            return;
        }

        PetMirrorTransform.ScaleX = horizontalDirection < 0 ? -1 : 1;
    }

    private void RestoreWindowPosition()
    {
        Left = _settings.Window.Left;
        Top = _settings.Window.Top;
        ClampWindowToWorkArea();
    }

    private void SaveWindowPosition()
    {
        ClampWindowToWorkArea();
        _settings.Window.Left = Left;
        _settings.Window.Top = Top;
        SaveSettings();
        SavePetStatus();
    }

    private void InitializeStorage()
    {
        try
        {
            var databaseExists = File.Exists(_petRepository.DatabasePath);
            _petRepository.Initialize();

            if (databaseExists)
            {
                var status = _petRepository.LoadStatus();
                _settings.Window.Left = status.WindowLeft;
                _settings.Window.Top = status.WindowTop;
                _initialPetState = Enum.TryParse<PetState>(status.State, ignoreCase: true, out var savedState)
                    ? savedState
                    : PetState.Idle;
            }
            else
            {
                _petRepository.SaveStatus(new PetStatus(
                    PetState.Idle.ToString(),
                    _settings.Window.Left,
                    _settings.Window.Top,
                    DateTimeOffset.UtcNow));
            }

            Debug.WriteLine($"LuluPet database: {_petRepository.DatabasePath}");
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Failed to initialize LuluPet database: {exception.Message}");
        }
    }

    private void SavePetStatus()
    {
        try
        {
            _petRepository.SaveStatus(new PetStatus(
                _stateMachine.CurrentState.ToString(),
                Left,
                Top,
                DateTimeOffset.UtcNow));
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Failed to save pet status: {exception.Message}");
        }
    }

    private void RecordInteraction(string interactionType, string? message)
    {
        try
        {
            _petRepository.RecordInteraction(new PetInteraction(
                interactionType,
                message,
                _animationPlayer.CurrentAction,
                Left,
                Top,
                DateTimeOffset.UtcNow));
        }
        catch (Exception exception)
        {
            Debug.WriteLine($"Failed to record pet interaction: {exception.Message}");
        }
    }

    private void ToggleClickThrough()
    {
        SetClickThrough(!_settings.Interaction.ClickThrough);
    }

    private void ToggleAutoStart()
    {
        SetAutoStart(!_settings.Startup.AutoStart);
    }

    private void ShowTrayToolPanel(ToolPanelKind panel)
    {
        if (_settings.Interaction.ClickThrough)
        {
            SetClickThrough(false);
        }

        ShowPetWindow();
        ShowToolPanel(panel);
    }

    private void SetClickThrough(bool enabled)
    {
        if (enabled)
        {
            HideActiveToolPanel();
        }

        _settings.Interaction.ClickThrough = enabled;
        ApplyWindowStyles();
        SaveSettings();
        UpdateTrayMenuState();
    }

    private void SetAutoStart(bool enabled)
    {
        _settings.Startup.AutoStart = enabled;
        ApplyAutoStartSetting();
        SaveSettings();
        UpdateTrayMenuState();
    }

    private void SetFileTransitFolder(string folderPath)
    {
        _settings.ToolPanels.FileTransitFolderPath = folderPath;
        _fileTransitService.SetFolderPath(folderPath);
        SaveSettings();
        UpdateFileTransitPanelState();
        UpdateTrayMenuState();
    }

    private void ApplyVisualSettings()
    {
        var scale = _settings.Appearance.Scale;
        var layoutVersion = ++_visualSettingsLayoutVersion;
        var petColumnWidth = BaseWindowWidth * scale;
        var toolPanelColumnWidth = _activeToolPanel != ToolPanelKind.None ? ToolPanelColumnWidth : 0;
        var windowWidth = petColumnWidth + toolPanelColumnWidth;
        var windowHeight = BaseWindowHeight * scale;
        if (_activeToolPanel != ToolPanelKind.None)
        {
            windowHeight = Math.Max(windowHeight, 390);
        }

        try
        {
            _isApplyingVisualSettings = true;
            _isVisualSettingsLayoutPending = true;
            Width = windowWidth;
            Height = windowHeight;
            PetColumn.Width = new GridLength(petColumnWidth);
            ToolPanelColumn.Width = new GridLength(toolPanelColumnWidth);
            PetImage.Width = BasePetImageSize * scale;
            PetImage.Height = BasePetImageSize * scale;
            SpeechBubble.Width = BaseSpeechBubbleWidth * scale;
            SpeechBubble.Height = BaseSpeechBubbleHeight * scale;
            SpeechBubble.ApplyScale(scale);
            ReminderPanel.ApplyScale(scale);
            ClipboardPanel.ApplyScale(scale);
            FileTransitPanel.ApplyScale(scale);
            SettingsPanel.ApplyScale(scale);
            TutorialPanel.ApplyScale(scale);
            Opacity = _settings.Appearance.Opacity;
        }
        finally
        {
            _isApplyingVisualSettings = false;
        }

        ClampWindowToWorkArea(Left, Top, windowWidth, windowHeight);
        Dispatcher.BeginInvoke(
            () =>
            {
                if (layoutVersion != _visualSettingsLayoutVersion)
                {
                    return;
                }

                _isVisualSettingsLayoutPending = false;
                ClampWindowToWorkArea();
            },
            DispatcherPriority.Loaded);
    }

    private void ApplyWindowStyles()
    {
        if (_windowHandle == nint.Zero)
        {
            return;
        }

        try
        {
            WindowStyleService.ApplyPetWindowStyles(_windowHandle, _settings.Interaction.ClickThrough);
        }
        catch (Win32Exception exception)
        {
            Debug.WriteLine($"Failed to apply pet window styles: {exception.Message}");
        }
    }

    private void ApplyAutoStartSetting()
    {
        try
        {
            StartupRegistryService.SetEnabled(
                ProjectInfo.ProductName,
                Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty,
                _settings.Startup.AutoStart);
        }
        catch (Exception exception) when (exception is ArgumentException
            or UnauthorizedAccessException
            or IOException
            or System.Security.SecurityException)
        {
            Debug.WriteLine($"Failed to update auto-start registry value: {exception.Message}");
        }
    }

    private void ClampWindowToWorkArea()
    {
        ClampWindowToWorkArea(Left, Top, GetWindowWidth(), GetWindowHeight());
    }

    private void ClampWindowToWorkArea(double candidateLeft, double candidateTop)
    {
        ClampWindowToWorkArea(candidateLeft, candidateTop, GetWindowWidth(), GetWindowHeight());
    }

    private void ClampWindowToWorkArea(
        double candidateLeft,
        double candidateTop,
        double width,
        double height)
    {
        if (_isClampingWindow)
        {
            return;
        }

        if (PresentationSource.FromVisual(this)?.CompositionTarget is null)
        {
            return;
        }

        var movementBounds = GetWindowMovementBounds(candidateLeft, candidateTop, width, height);
        var clampedLeft = movementBounds.ClampLeft(candidateLeft);
        var clampedTop = movementBounds.ClampTop(candidateTop);

        if (Math.Abs(clampedLeft - Left) < WindowPositionEpsilon
            && Math.Abs(clampedTop - Top) < WindowPositionEpsilon)
        {
            return;
        }

        try
        {
            _isClampingWindow = true;
            SetWindowPosition(clampedLeft, clampedTop);
        }
        finally
        {
            _isClampingWindow = false;
        }
    }

    private void SetWindowPosition(double left, double top)
    {
        if (Math.Abs(left - Left) >= WindowPositionEpsilon)
        {
            Left = left;
        }

        if (Math.Abs(top - Top) >= WindowPositionEpsilon)
        {
            Top = top;
        }
    }

    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private void SaveReminderSettings(bool applyToScheduler = true)
    {
        ReminderSettings.Normalize(_settings.Reminders);
        if (applyToScheduler)
        {
            _reminderScheduler.ApplySettings(_settings.Reminders);
        }

        ReminderPanel.ApplySettings(_settings.Reminders);
        SaveSettings();
        UpdateReminderPanelState();
    }

    private void UpdateCompanionTrayText()
    {
        if (_companionTracker is null)
        {
            return;
        }

        try
        {
            _notifyIcon.Text = _companionTracker.FormatTrayText();
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException)
        {
            Debug.WriteLine($"Failed to update tray text: {exception.Message}");
        }
    }

    private static DateOnly GetCurrentLocalDate()
    {
        return DateOnly.FromDateTime(DateTimeOffset.Now.LocalDateTime);
    }

    private static bool IsFromPetImage(object originalSource)
    {
        return IsFromNamedElement(originalSource, "PetImage");
    }

    private static bool IsFromToolPanel(object originalSource)
    {
        return IsFromNamedElement(originalSource, "ReminderPanel")
            || IsFromNamedElement(originalSource, "ClipboardPanel")
            || IsFromNamedElement(originalSource, "FileTransitPanel")
            || IsFromNamedElement(originalSource, "SettingsPanel")
            || IsFromNamedElement(originalSource, "TutorialPanel");
    }

    private static bool IsFromNamedElement(object originalSource, string elementName)
    {
        if (originalSource is not DependencyObject current)
        {
            return false;
        }

        while (current is not null)
        {
            if (current is FrameworkElement element && element.Name == elementName)
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private enum ScreenEdge
    {
        Left,
        Top,
        Right,
        Bottom
    }
}

internal enum ToolPanelKind
{
    None,
    Reminder,
    Clipboard,
    FileTransit,
    Settings,
    Tutorial
}

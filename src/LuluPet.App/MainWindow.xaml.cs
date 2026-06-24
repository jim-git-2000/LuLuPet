using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LuluPet.Core;
using LuluPet.Core.Animation;
using LuluPet.Core.Behavior;
using LuluPet.Core.Config;
using LuluPet.Core.Dialogues;
using LuluPet.Core.Storage;
using LuluPet.Win32;
using Forms = System.Windows.Forms;

namespace LuluPet.App;

public partial class MainWindow : System.Windows.Window
{
    private readonly JsonSettingsStore _settingsStore;
    private readonly AppSettings _settings;
    private readonly DialogueLineProvider _dialogueLineProvider;
    private readonly SqlitePetRepository _petRepository;
    private readonly FrameAnimationPlayer _animationPlayer = new();
    private readonly PetStateMachine _stateMachine = new();
    private readonly DispatcherTimer _animationTimer = new();
    private readonly DispatcherTimer _stateTimer = new();
    private readonly DispatcherTimer _periodicSpeechTimer = new();
    private readonly DispatcherTimer _bubbleHideTimer = new();
    private readonly Dictionary<string, BitmapImage> _frameCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _showMenuItem;
    private readonly Forms.ToolStripMenuItem _hideMenuItem;
    private readonly Forms.ToolStripMenuItem _settingsMenuItem;
    private readonly Forms.ToolStripMenuItem _clickThroughMenuItem;
    private readonly Forms.ToolStripMenuItem _autoStartMenuItem;
    private DateTimeOffset _lastAnimationTick;
    private DateTimeOffset _lastStateTick;
    private DateTimeOffset? _interactionAnimationUntil;
    private nint _windowHandle;
    private PetState _initialPetState = PetState.Idle;
    private SettingsWindow? _settingsWindow;
    private System.Windows.Point _dragStartScreenPoint;
    private double _dragStartLeft;
    private double _dragStartTop;
    private double _walkVelocityX = WalkPixelsPerSecond;
    private double _walkVelocityY;
    private TimeSpan _walkRetargetRemaining;
    private bool _isClampingWindow;
    private bool _isPotentialDrag;
    private bool _isDraggingWindow;
    private bool _isDragAnimationActive;
    private bool _dragStartedOnPet;
    private bool _isWalkSpeechVisible;
    private bool _isExitRequested;

    private const double WalkPixelsPerSecond = 36;
    private const double MinWalkPixelsPerSecond = 22;
    private const double MaxWalkPixelsPerSecond = 64;
    private const double DragThreshold = 4;
    private static readonly TimeSpan InteractionAnimationDuration = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan BubbleVisibleDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan PeriodicSpeechInterval = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MinWalkRetargetInterval = TimeSpan.FromMilliseconds(900);
    private static readonly TimeSpan MaxWalkRetargetInterval = TimeSpan.FromMilliseconds(2400);
    private const string WalkSpeechText = "（桌面巡逻中）";
    private const string AngrySpeechText = "人，再敲打你哦！";

    public MainWindow()
    {
        _settingsStore = new JsonSettingsStore(
            Path.Combine(AppContext.BaseDirectory, "settings.json"));
        _settings = _settingsStore.Load();
        _dialogueLineProvider = new DialogueLineProvider(
            Path.Combine(AppContext.BaseDirectory, "Assets", "dialogues", "lines.json"));
        _petRepository = new SqlitePetRepository(LuluPetDataPaths.GetDefaultDatabasePath());

        InitializeComponent();
        ApplyWindowIcon();
        _showMenuItem = new Forms.ToolStripMenuItem("显示", null, (_, _) => ShowPetWindow());
        _hideMenuItem = new Forms.ToolStripMenuItem("隐藏", null, (_, _) => HidePetWindow());
        _settingsMenuItem = new Forms.ToolStripMenuItem("设置", null, (_, _) => ShowSettingsWindow());
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
        RestoreWindowPosition();
        ApplyVisualSettings();
        InitializeAnimation();
        InitializeBehavior();
        InitializeSpeech();
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
        Left = _dragStartLeft + deltaX;
        Top = _dragStartTop + deltaY;
        ClampWindowToWorkArea();
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

    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExitRequested)
        {
            e.Cancel = true;
            HidePetWindow();
            return;
        }

        _animationTimer.Stop();
        _stateTimer.Stop();
        _periodicSpeechTimer.Stop();
        _bubbleHideTimer.Stop();
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
            _lastAnimationTick = DateTimeOffset.UtcNow;
            _animationTimer.Start();
        }

        _lastStateTick = DateTimeOffset.UtcNow;
        _stateTimer.Start();
        _periodicSpeechTimer.Start();
        UpdateTrayMenuState();
    }

    private void HidePetWindow()
    {
        SaveWindowPosition();
        _animationTimer.Stop();
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
        _settingsWindow?.ApplySettings(_settings);
    }

    private void DisposeTrayIcon()
    {
        _settingsWindow?.Close();
        _settingsWindow = null;
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

        _animationTimer.Interval = TimeSpan.FromMilliseconds(33);
        _animationTimer.Tick += (_, _) => TickAnimation();
        _lastAnimationTick = DateTimeOffset.UtcNow;

        if (TryPlayAction("Idle") || TryPlayAction("Walk") || TryPlayAction("Sleep"))
        {
            _animationTimer.Start();
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

    private void InitializeWin32()
    {
        SourceInitialized += (_, _) =>
        {
            _windowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            ApplyWindowStyles();
            ClampWindowToWorkArea();
        };

        LocationChanged += (_, _) => ClampWindowToWorkArea();
        SizeChanged += (_, _) => ClampWindowToWorkArea();
        Loaded += (_, _) =>
        {
            ClampWindowToWorkArea();
            ApplyAutoStartSetting();
        };
    }

    private void LoadAction(string actionName, string directoryName, int fps)
    {
        var directoryPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "pet",
            directoryName);

        if (!_animationPlayer.TryLoadActionFromDirectory(actionName, directoryPath, fps))
        {
            Debug.WriteLine($"Animation action '{actionName}' has no frames in {directoryPath}.");
        }
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

        if (_stateMachine.CurrentState == PetState.Walk)
        {
            MoveWhileWalking(elapsed);
        }

        if (_interactionAnimationUntil is not null && now >= _interactionAnimationUntil.Value)
        {
            _interactionAnimationUntil = null;
            PlayStateAnimation(_stateMachine.CurrentState);
        }
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
        if (_isDragAnimationActive || IsInteractionAnimationActive())
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

        _walkRetargetRemaining -= elapsed;
        if (_walkRetargetRemaining <= TimeSpan.Zero)
        {
            RetargetWalkMotion();
        }

        var workArea = SystemParameters.WorkArea;
        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var nextLeft = Left + _walkVelocityX * elapsed.TotalSeconds;
        var nextTop = Top + _walkVelocityY * elapsed.TotalSeconds;
        var minLeft = workArea.Left;
        var minTop = workArea.Top;
        var maxLeft = Math.Max(workArea.Left, workArea.Right - width);
        var maxTop = Math.Max(workArea.Top, workArea.Bottom - height);

        if (nextLeft <= minLeft)
        {
            nextLeft = minLeft;
            _walkVelocityX = Math.Abs(_walkVelocityX);
        }
        else if (nextLeft >= maxLeft)
        {
            nextLeft = maxLeft;
            _walkVelocityX = -Math.Abs(_walkVelocityX);
        }

        if (nextTop <= minTop)
        {
            nextTop = minTop;
            _walkVelocityY = Math.Abs(_walkVelocityY);
        }
        else if (nextTop >= maxTop)
        {
            nextTop = maxTop;
            _walkVelocityY = -Math.Abs(_walkVelocityY);
        }

        UpdatePetFacing(_walkVelocityX);
        Left = nextLeft;
        Top = nextTop;
        ClampWindowToWorkArea();
    }

    private void BeginWalkMotion()
    {
        RetargetWalkMotion();
    }

    private void RetargetWalkMotion()
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
        _walkRetargetRemaining = RandomDuration(MinWalkRetargetInterval, MaxWalkRetargetInterval);
    }

    private static TimeSpan RandomDuration(TimeSpan minDuration, TimeSpan maxDuration)
    {
        var minMilliseconds = minDuration.TotalMilliseconds;
        var maxMilliseconds = maxDuration.TotalMilliseconds;
        return TimeSpan.FromMilliseconds(
            minMilliseconds + Random.Shared.NextDouble() * (maxMilliseconds - minMilliseconds));
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

        if (!TryPlayAction("Happy"))
        {
            TryPlayAction("Idle");
        }

        var message = ShowRandomSpeech();
        RecordInteraction("click", message);
    }

    private void TriggerEat()
    {
        _stateMachine.ForceState(PetState.Idle);
        _interactionAnimationUntil = DateTimeOffset.UtcNow + InteractionAnimationDuration;

        if (!TryPlayAction("Eat"))
        {
            TryPlayAction("Happy");
        }

        var message = ShowSpeech("开饭啦！");
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
            || string.Equals(normalizedLine, AngrySpeechText, StringComparison.Ordinal);
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

    private void SetPetFrame(string framePath)
    {
        if (!File.Exists(framePath))
        {
            MissingAssetFallback.Visibility = Visibility.Visible;
            return;
        }

        if (_frameCache.TryGetValue(framePath, out var cachedImage))
        {
            PetImage.Source = cachedImage;
            MissingAssetFallback.Visibility = Visibility.Collapsed;
            return;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(framePath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();

            _frameCache[framePath] = image;
            PetImage.Source = image;
            MissingAssetFallback.Visibility = Visibility.Collapsed;
        }
        catch (Exception exception) when (exception is IOException
            or ArgumentException
            or NotSupportedException
            or InvalidOperationException)
        {
            Debug.WriteLine($"Failed to load pet frame '{framePath}': {exception.Message}");
            MissingAssetFallback.Visibility = Visibility.Visible;
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

    private void ShowSettingsWindow()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings)
        {
            Owner = this
        };
        _settingsWindow.ScaleChanged += value =>
        {
            _settings.Appearance.Scale = value;
            ApplyVisualSettings();
            SaveSettings();
        };
        _settingsWindow.OpacityChanged += value =>
        {
            _settings.Appearance.Opacity = value;
            ApplyVisualSettings();
            SaveSettings();
        };
        _settingsWindow.VolumeChanged += value =>
        {
            _settings.Audio.Volume = value;
            SaveSettings();
        };
        _settingsWindow.ClickThroughChanged += SetClickThrough;
        _settingsWindow.AutoStartChanged += SetAutoStart;
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void SetClickThrough(bool enabled)
    {
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

    private void ApplyVisualSettings()
    {
        var scale = _settings.Appearance.Scale;
        Width = 340 * scale;
        Height = 390 * scale;
        PetImage.Width = 300 * scale;
        PetImage.Height = 300 * scale;
        SpeechBubble.Width = 230 * scale;
        SpeechBubble.Height = 96 * scale;
        Opacity = _settings.Appearance.Opacity;
        ClampWindowToWorkArea();
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
        if (_isClampingWindow)
        {
            return;
        }

        var width = ActualWidth > 0 ? ActualWidth : Width;
        var height = ActualHeight > 0 ? ActualHeight : Height;
        var workArea = SystemParameters.WorkArea;
        var minLeft = workArea.Left;
        var minTop = workArea.Top;
        var maxLeft = Math.Max(workArea.Left, workArea.Right - width);
        var maxTop = Math.Max(workArea.Top, workArea.Bottom - height);
        var clampedLeft = Math.Clamp(Left, minLeft, maxLeft);
        var clampedTop = Math.Clamp(Top, minTop, maxTop);

        if (Math.Abs(clampedLeft - Left) < 0.1 && Math.Abs(clampedTop - Top) < 0.1)
        {
            return;
        }

        try
        {
            _isClampingWindow = true;
            Left = clampedLeft;
            Top = clampedTop;
        }
        finally
        {
            _isClampingWindow = false;
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

    private static bool IsFromPetImage(object originalSource)
    {
        return IsFromNamedElement(originalSource, "PetImage");
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
}

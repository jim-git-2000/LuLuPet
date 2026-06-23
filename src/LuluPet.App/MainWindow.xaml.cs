using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LuluPet.Core.Animation;
using LuluPet.Core.Behavior;
using LuluPet.Core.Config;
using LuluPet.Core.Dialogues;
using Forms = System.Windows.Forms;

namespace LuluPet.App;

public partial class MainWindow : Window
{
    private readonly JsonSettingsStore _settingsStore;
    private readonly AppSettings _settings;
    private readonly DialogueLineProvider _dialogueLineProvider;
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
    private DateTimeOffset _lastAnimationTick;
    private DateTimeOffset _lastStateTick;
    private DateTimeOffset? _interactionAnimationUntil;
    private int _walkDirection = 1;
    private bool _isExitRequested;

    private const double WalkPixelsPerSecond = 36;
    private static readonly TimeSpan InteractionAnimationDuration = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan BubbleVisibleDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan PeriodicSpeechInterval = TimeSpan.FromSeconds(30);

    public MainWindow()
    {
        _settingsStore = new JsonSettingsStore(
            Path.Combine(AppContext.BaseDirectory, "settings.json"));
        _settings = _settingsStore.Load();
        _dialogueLineProvider = new DialogueLineProvider(
            Path.Combine(AppContext.BaseDirectory, "Assets", "dialogues", "lines.json"));

        InitializeComponent();
        _showMenuItem = new Forms.ToolStripMenuItem("显示", null, (_, _) => ShowPetWindow());
        _hideMenuItem = new Forms.ToolStripMenuItem("隐藏", null, (_, _) => HidePetWindow());
        _notifyIcon = CreateNotifyIcon();

        RestoreWindowPosition();
        InitializeAnimation();
        InitializeBehavior();
        InitializeSpeech();
        UpdateTrayMenuState();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
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

        if (IsFromCloseButton(e.OriginalSource))
        {
            return;
        }

        if (IsFromPetImage(e.OriginalSource))
        {
            HandlePetClick();
            e.Handled = true;
            return;
        }

        try
        {
            DragMove();
            SaveWindowPosition();
        }
        catch (InvalidOperationException)
        {
            // DragMove can fail when the mouse button state changes mid-drag.
        }
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
            _stateMachine.ForceState(PetState.Walk);
            PlayStateAnimation(PetState.Walk);
        }

        if (e.Key == Key.S)
        {
            _stateMachine.ForceState(PetState.Sleep);
            PlayStateAnimation(PetState.Sleep);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
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
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "icons", "app.ico");

        if (!File.Exists(iconPath))
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

    private void ShowPetWindow()
    {
        Show();

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        Keyboard.Focus(this);

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

    private void LoadAction(string actionName, string directoryName, int fps)
    {
        var directoryPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "pet",
            directoryName);

        _animationPlayer.TryLoadActionFromDirectory(actionName, directoryPath, fps);
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

        if (args.CurrentState == PetState.Walk)
        {
            _walkDirection = Random.Shared.Next(0, 2) == 0 ? -1 : 1;
        }
    }

    private void PlayStateAnimation(PetState state)
    {
        if (IsInteractionAnimationActive())
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

        var workArea = SystemParameters.WorkArea;
        var nextLeft = Left + _walkDirection * WalkPixelsPerSecond * elapsed.TotalSeconds;
        var minLeft = workArea.Left;
        var maxLeft = Math.Max(workArea.Left, workArea.Right - ActualWidth);

        if (nextLeft <= minLeft)
        {
            nextLeft = minLeft;
            _walkDirection = 1;
        }
        else if (nextLeft >= maxLeft)
        {
            nextLeft = maxLeft;
            _walkDirection = -1;
        }

        Left = nextLeft;
    }

    private void PetImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        HandlePetClick();
        e.Handled = true;
    }

    private void HandlePetClick()
    {
        _stateMachine.ForceState(PetState.Idle);
        _interactionAnimationUntil = DateTimeOffset.UtcNow + InteractionAnimationDuration;

        if (!TryPlayAction("Happy"))
        {
            TryPlayAction("Idle");
        }

        ShowRandomSpeech();
    }

    private bool IsInteractionAnimationActive()
    {
        return _interactionAnimationUntil is not null
            && DateTimeOffset.UtcNow < _interactionAnimationUntil.Value;
    }

    private void ShowRandomSpeech()
    {
        var line = _dialogueLineProvider.GetRandomLine();
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        SpeechBubble.SetText(line);
        SpeechBubble.Visibility = Visibility.Visible;

        _bubbleHideTimer.Stop();
        _bubbleHideTimer.Start();
    }

    private void HideSpeechBubble()
    {
        _bubbleHideTimer.Stop();
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

    private void RestoreWindowPosition()
    {
        Left = _settings.Window.Left;
        Top = _settings.Window.Top;
    }

    private void SaveWindowPosition()
    {
        _settings.Window.Left = Left;
        _settings.Window.Top = Top;

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

    private static bool IsFromCloseButton(object originalSource)
    {
        return IsFromNamedElement(originalSource, "CloseButton");
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

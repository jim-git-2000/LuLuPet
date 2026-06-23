using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LuluPet.Core.Animation;
using LuluPet.Core.Config;
using Forms = System.Windows.Forms;

namespace LuluPet.App;

public partial class MainWindow : Window
{
    private readonly JsonSettingsStore _settingsStore;
    private readonly AppSettings _settings;
    private readonly FrameAnimationPlayer _animationPlayer = new();
    private readonly DispatcherTimer _animationTimer = new();
    private readonly Dictionary<string, BitmapImage> _frameCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _showMenuItem;
    private readonly Forms.ToolStripMenuItem _hideMenuItem;
    private DateTimeOffset _lastAnimationTick;
    private bool _isExitRequested;

    public MainWindow()
    {
        _settingsStore = new JsonSettingsStore(
            Path.Combine(AppContext.BaseDirectory, "settings.json"));
        _settings = _settingsStore.Load();

        InitializeComponent();
        _showMenuItem = new Forms.ToolStripMenuItem("显示", null, (_, _) => ShowPetWindow());
        _hideMenuItem = new Forms.ToolStripMenuItem("隐藏", null, (_, _) => HidePetWindow());
        _notifyIcon = CreateNotifyIcon();

        RestoreWindowPosition();
        InitializeAnimation();
        UpdateTrayMenuState();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        if (IsFromCloseButton(e.OriginalSource))
        {
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
            TryPlayAction("Idle");
        }

        if (e.Key == Key.W)
        {
            TryPlayAction("Walk");
        }

        if (e.Key == Key.S)
        {
            TryPlayAction("Sleep");
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

        UpdateTrayMenuState();
    }

    private void HidePetWindow()
    {
        SaveWindowPosition();
        _animationTimer.Stop();
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
        if (originalSource is not DependencyObject current)
        {
            return false;
        }

        while (current is not null)
        {
            if (current is System.Windows.Controls.Button button && button.Name == "CloseButton")
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}

using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LuluPet.Core.Config;

namespace LuluPet.App;

public partial class MainWindow : Window
{
    private readonly JsonSettingsStore _settingsStore;
    private readonly AppSettings _settings;

    public MainWindow()
    {
        _settingsStore = new JsonSettingsStore(
            Path.Combine(AppContext.BaseDirectory, "settings.json"));
        _settings = _settingsStore.Load();

        InitializeComponent();
        RestoreWindowPosition();
        LoadIdleFrame();
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

    private void LoadIdleFrame()
    {
        var framePath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "pet",
            "idle",
            "lulu_idle_0001.png");

        if (!File.Exists(framePath))
        {
            MissingAssetFallback.Visibility = Visibility.Visible;
            return;
        }

        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(framePath, UriKind.Absolute);
        image.EndInit();
        image.Freeze();

        PetImage.Source = image;
        MissingAssetFallback.Visibility = Visibility.Collapsed;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        SaveWindowPosition();
        base.OnClosing(e);
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
            if (current is Button button && button.Name == "CloseButton")
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}

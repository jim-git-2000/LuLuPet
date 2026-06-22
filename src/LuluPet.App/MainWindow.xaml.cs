using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LuluPet.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadIdleFrame();
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
}

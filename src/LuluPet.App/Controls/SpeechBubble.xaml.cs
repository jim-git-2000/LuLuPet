using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LuluPet.App.Controls;

public partial class SpeechBubble : System.Windows.Controls.UserControl
{
    public SpeechBubble()
    {
        InitializeComponent();
    }

    public void SetText(string text)
    {
        BubbleText.Text = text;
    }

    public void ApplyScale(double scale)
    {
        BubbleText.FontSize = 14 * scale;
        BubbleText.LineHeight = 18 * scale;
        BubbleText.Margin = new Thickness(
            22 * scale,
            16 * scale,
            26 * scale,
            24 * scale);
    }

    public void TrySetBackgroundImage(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            UseFallbackBackground();
            return;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource = new Uri(imagePath, UriKind.Absolute);
            image.EndInit();
            image.Freeze();

            BubbleBackground.Source = image;
            BubbleBackground.Visibility = Visibility.Visible;
            FallbackBackground.Visibility = Visibility.Collapsed;
        }
        catch (IOException)
        {
            UseFallbackBackground();
        }
        catch (ArgumentException)
        {
            UseFallbackBackground();
        }
    }

    private void UseFallbackBackground()
    {
        BubbleBackground.Source = null;
        BubbleBackground.Visibility = Visibility.Collapsed;
        FallbackBackground.Visibility = Visibility.Visible;
    }
}

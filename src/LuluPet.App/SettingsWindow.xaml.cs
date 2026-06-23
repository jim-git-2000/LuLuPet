using System.Windows;
using LuluPet.Core.Config;

namespace LuluPet.App;

public partial class SettingsWindow : System.Windows.Window
{
    private bool _isInitializing;

    public SettingsWindow(AppSettings settings)
    {
        InitializeComponent();
        ApplySettings(settings);
    }

    public event Action<double>? ScaleChanged;

    public event Action<double>? OpacityChanged;

    public event Action<double>? VolumeChanged;

    public event Action<bool>? ClickThroughChanged;

    public event Action<bool>? AutoStartChanged;

    public void ApplySettings(AppSettings settings)
    {
        _isInitializing = true;
        try
        {
            ScaleSlider.Value = settings.Appearance.Scale;
            OpacitySlider.Value = settings.Appearance.Opacity;
            VolumeSlider.Value = settings.Audio.Volume;
            ClickThroughCheckBox.IsChecked = settings.Interaction.ClickThrough;
            AutoStartCheckBox.IsChecked = settings.Startup.AutoStart;
            UpdateValueText();
        }
        finally
        {
            _isInitializing = false;
        }
    }

    private void ScaleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateValueText();

        if (!_isInitializing)
        {
            ScaleChanged?.Invoke(e.NewValue);
        }
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateValueText();

        if (!_isInitializing)
        {
            OpacityChanged?.Invoke(e.NewValue);
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateValueText();

        if (!_isInitializing)
        {
            VolumeChanged?.Invoke(e.NewValue);
        }
    }

    private void ClickThroughCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isInitializing)
        {
            ClickThroughChanged?.Invoke(ClickThroughCheckBox.IsChecked == true);
        }
    }

    private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isInitializing)
        {
            AutoStartChanged?.Invoke(AutoStartCheckBox.IsChecked == true);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void UpdateValueText()
    {
        if (ScaleValueText is null
            || OpacityValueText is null
            || VolumeValueText is null)
        {
            return;
        }

        ScaleValueText.Text = $"{ScaleSlider.Value:0.00}x";
        OpacityValueText.Text = $"{OpacitySlider.Value:P0}";
        VolumeValueText.Text = $"{VolumeSlider.Value:P0}";
    }
}

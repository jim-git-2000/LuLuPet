using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace LuluPet.App.Controls;

public partial class ToggleSwitch : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
        nameof(IsChecked),
        typeof(bool),
        typeof(ToggleSwitch),
        new PropertyMetadata(false, OnIsCheckedChanged));

    public ToggleSwitch()
    {
        InitializeComponent();
        UpdateVisual(animate: false);
    }

    public bool IsChecked
    {
        get => (bool)GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    public event EventHandler<bool>? CheckedChanged;

    private static void OnIsCheckedChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
    {
        var toggleSwitch = (ToggleSwitch)dependencyObject;
        toggleSwitch.UpdateVisual(animate: true);
        toggleSwitch.CheckedChanged?.Invoke(toggleSwitch, (bool)args.NewValue);
    }

    private void ToggleSwitch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        IsChecked = !IsChecked;
        e.Handled = true;
    }

    private void ToggleSwitch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Space && e.Key != Key.Enter)
        {
            return;
        }

        IsChecked = !IsChecked;
        e.Handled = true;
    }

    private void UpdateVisual(bool animate)
    {
        var targetX = IsChecked ? 22.0 : 0.0;
        Track.Background = new SolidColorBrush(IsChecked
            ? Color.FromRgb(242, 178, 63)
            : Color.FromRgb(229, 231, 235));
        Track.BorderBrush = new SolidColorBrush(IsChecked
            ? Color.FromRgb(217, 145, 28)
            : Color.FromRgb(209, 213, 219));

        if (!animate)
        {
            KnobTransform.X = targetX;
            return;
        }

        var animation = new DoubleAnimation
        {
            To = targetX,
            Duration = TimeSpan.FromMilliseconds(120),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        KnobTransform.BeginAnimation(TranslateTransform.XProperty, animation);
    }
}

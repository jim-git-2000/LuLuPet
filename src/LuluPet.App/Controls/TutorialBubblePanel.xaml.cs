using System.Windows;

namespace LuluPet.App.Controls;

public partial class TutorialBubblePanel : System.Windows.Controls.UserControl
{
    public TutorialBubblePanel()
    {
        InitializeComponent();
    }

    public event EventHandler? CloseRequested;

    public void ApplyScale(double scale)
    {
        Width = 300;
        Height = 360;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

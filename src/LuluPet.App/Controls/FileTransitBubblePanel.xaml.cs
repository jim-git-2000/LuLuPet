using System.Windows;
using LuluPet.App.Services;
using LuluPet.Core.FileTransit;

namespace LuluPet.App.Controls;

public partial class FileTransitBubblePanel : System.Windows.Controls.UserControl
{
    public FileTransitBubblePanel()
    {
        InitializeComponent();
        ApplySnapshot(new FileTransitSnapshot(string.Empty, Array.Empty<FileTransitItem>()));
    }

    public event EventHandler? CloseRequested;

    public event EventHandler? OpenFolderRequested;

    public void ApplySnapshot(FileTransitSnapshot snapshot)
    {
        var viewItems = snapshot.Items
            .Select(item => new FileTransitViewItem(
                item.Name,
                FileTransitService.FormatSize(item.SizeBytes),
                FormatModifiedAt(item.ModifiedAt)))
            .ToArray();

        FileListBox.ItemsSource = viewItems;
        EmptyText.Visibility = viewItems.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        FolderText.Text = snapshot.FolderPath;
    }

    public void ApplyScale(double scale)
    {
        Width = 300;
        Height = 292;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        OpenFolderRequested?.Invoke(this, EventArgs.Empty);
    }

    private static string FormatModifiedAt(DateTimeOffset modifiedAt)
    {
        var localTime = modifiedAt.LocalDateTime;
        return localTime.Date == DateTime.Today
            ? localTime.ToString("HH:mm")
            : localTime.ToString("MM-dd HH:mm");
    }

    private sealed record FileTransitViewItem(
        string Name,
        string SizeText,
        string ModifiedAtText);
}

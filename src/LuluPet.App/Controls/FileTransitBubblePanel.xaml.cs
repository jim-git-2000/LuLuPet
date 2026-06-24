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

    public event EventHandler<IReadOnlyList<string>>? FilesDropped;

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
        StatusText.Text = viewItems.Length == 0 ? "拖入文件可暂存" : $"共 {viewItems.Length} 个文件";
        FolderText.Text = snapshot.FolderPath;
    }

    public void SetStatus(string message)
    {
        StatusText.Text = message;
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

    private void FileTransitPanel_DragEnter(object sender, DragEventArgs e)
    {
        ApplyDragEffects(e);
    }

    private void FileTransitPanel_DragOver(object sender, DragEventArgs e)
    {
        ApplyDragEffects(e);
    }

    private void FileTransitPanel_Drop(object sender, DragEventArgs e)
    {
        if (!TryGetDroppedFiles(e, out var files))
        {
            e.Effects = DragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = DragDropEffects.Copy;
        FilesDropped?.Invoke(this, files);
        e.Handled = true;
    }

    private static void ApplyDragEffects(DragEventArgs e)
    {
        e.Effects = TryGetDroppedFiles(e, out _)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private static bool TryGetDroppedFiles(DragEventArgs e, out IReadOnlyList<string> files)
    {
        files = Array.Empty<string>();
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return false;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return false;
        }

        files = paths.Where(File.Exists).ToArray();
        return files.Count > 0;
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

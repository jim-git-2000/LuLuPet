using System.IO;
using System.Windows;
using LuluPet.App.Services;
using LuluPet.Core.FileTransit;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;

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

    private void FileTransitPanel_DragEnter(object sender, WpfDragEventArgs e)
    {
        ApplyDragEffects(e);
    }

    private void FileTransitPanel_DragOver(object sender, WpfDragEventArgs e)
    {
        ApplyDragEffects(e);
    }

    private void FileTransitPanel_Drop(object sender, WpfDragEventArgs e)
    {
        if (!TryGetDroppedFiles(e, out var files))
        {
            e.Effects = WpfDragDropEffects.None;
            e.Handled = true;
            return;
        }

        e.Effects = WpfDragDropEffects.Copy;
        FilesDropped?.Invoke(this, files);
        e.Handled = true;
    }

    private static void ApplyDragEffects(WpfDragEventArgs e)
    {
        e.Effects = TryGetDroppedFiles(e, out _)
            ? WpfDragDropEffects.Copy
            : WpfDragDropEffects.None;
        e.Handled = true;
    }

    private static bool TryGetDroppedFiles(WpfDragEventArgs e, out IReadOnlyList<string> files)
    {
        files = Array.Empty<string>();
        if (!e.Data.GetDataPresent(WpfDataFormats.FileDrop))
        {
            return false;
        }

        if (e.Data.GetData(WpfDataFormats.FileDrop) is not string[] paths)
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

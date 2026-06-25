using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LuluPet.App.Services;
using LuluPet.Core.FileTransit;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfKey = System.Windows.Input.Key;
using WpfKeyboard = System.Windows.Input.Keyboard;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfModifierKeys = System.Windows.Input.ModifierKeys;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;

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

    public event EventHandler? PasteRequested;

    public event EventHandler<string>? FileOpenRequested;

    public event EventHandler<string>? FileCopyRequested;

    public void ApplySnapshot(FileTransitSnapshot snapshot)
    {
        var viewItems = snapshot.Items
            .Select(item => new FileTransitViewItem(
                item.Name,
                item.FullPath,
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

    private void FileTransitPanel_PreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == WpfKey.V
            && (WpfKeyboard.Modifiers & WpfModifierKeys.Control) == WpfModifierKeys.Control)
        {
            PasteRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }

    private void FileListBox_MouseDoubleClick(object sender, WpfMouseButtonEventArgs e)
    {
        if (TryGetFileItem(e.OriginalSource, out var item))
        {
            FileOpenRequested?.Invoke(this, item.FullPath);
            e.Handled = true;
        }
    }

    private void FileListBox_PreviewMouseRightButtonUp(object sender, WpfMouseButtonEventArgs e)
    {
        if (TryGetFileItem(e.OriginalSource, out var item))
        {
            FileCopyRequested?.Invoke(this, item.FullPath);
            e.Handled = true;
        }
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

    private static bool TryGetFileItem(object originalSource, out FileTransitViewItem item)
    {
        item = default!;
        if (originalSource is not DependencyObject current)
        {
            return false;
        }

        while (current is not null)
        {
            if (current is ListBoxItem { DataContext: FileTransitViewItem viewItem })
            {
                item = viewItem;
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
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
        string FullPath,
        string SizeText,
        string ModifiedAtText);
}

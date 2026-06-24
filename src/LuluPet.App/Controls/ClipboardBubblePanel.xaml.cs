using System.Windows;
using LuluPet.Core.Clipboard;

namespace LuluPet.App.Controls;

public partial class ClipboardBubblePanel : System.Windows.Controls.UserControl
{
    public ClipboardBubblePanel()
    {
        InitializeComponent();
        ApplyHistory(Array.Empty<ClipboardHistoryItem>(), capacity: 100);
    }

    public event EventHandler? CloseRequested;

    public event EventHandler<string>? TextSelected;

    public void ApplyHistory(IReadOnlyList<ClipboardHistoryItem> items, int capacity)
    {
        var viewItems = items
            .Select(item => new ClipboardHistoryViewItem(
                item.Text,
                FormatPreview(item.Text),
                FormatCapturedAt(item.CapturedAt)))
            .ToArray();

        HistoryListBox.ItemsSource = viewItems;
        EmptyText.Visibility = viewItems.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        CountText.Text = $"共 {viewItems.Length} / {capacity}";
    }

    public void SetStatus(string message)
    {
        CountText.Text = message;
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

    private void HistoryListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (HistoryListBox.SelectedItem is ClipboardHistoryViewItem item)
        {
            TextSelected?.Invoke(this, item.Text);
            HistoryListBox.SelectedItem = null;
        }
    }

    private static string FormatPreview(string text)
    {
        var preview = text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ').Trim();
        return preview.Length <= 180 ? preview : $"{preview[..180]}...";
    }

    private static string FormatCapturedAt(DateTimeOffset capturedAt)
    {
        var localTime = capturedAt.LocalDateTime;
        return localTime.Date == DateTime.Today
            ? localTime.ToString("HH:mm")
            : localTime.ToString("MM-dd HH:mm");
    }

    private sealed record ClipboardHistoryViewItem(
        string Text,
        string Preview,
        string CapturedAtText);
}

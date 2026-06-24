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

    public void ApplyHistory(IReadOnlyList<ClipboardHistoryItem> items, int capacity)
    {
        var viewItems = items
            .Select(item => new ClipboardHistoryViewItem(
                FormatPreview(item.Text),
                FormatCapturedAt(item.CapturedAt)))
            .ToArray();

        HistoryListBox.ItemsSource = viewItems;
        EmptyText.Visibility = viewItems.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        CountText.Text = $"共 {viewItems.Length} / {capacity}";
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
        string Preview,
        string CapturedAtText);
}

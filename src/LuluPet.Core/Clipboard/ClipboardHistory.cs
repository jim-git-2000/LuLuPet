namespace LuluPet.Core.Clipboard;

public sealed class ClipboardHistory
{
    private readonly List<ClipboardHistoryItem> _items = [];

    public ClipboardHistory(int capacity = 100)
    {
        Capacity = NormalizeCapacity(capacity);
    }

    public int Capacity { get; }

    public int Count => _items.Count;

    public bool AddText(string? text, DateTimeOffset capturedAt)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var normalizedText = text.Trim();
        var existingIndex = _items.FindIndex(item => item.Text == normalizedText);
        if (existingIndex >= 0)
        {
            _items.RemoveAt(existingIndex);
        }

        _items.Insert(0, new ClipboardHistoryItem(normalizedText, capturedAt));

        if (_items.Count > Capacity)
        {
            _items.RemoveRange(Capacity, _items.Count - Capacity);
        }

        return true;
    }

    public IReadOnlyList<ClipboardHistoryItem> Snapshot()
    {
        return _items.ToArray();
    }

    public void Clear()
    {
        _items.Clear();
    }

    private static int NormalizeCapacity(int capacity)
    {
        if (capacity <= 0)
        {
            return 100;
        }

        return Math.Min(capacity, 100);
    }
}

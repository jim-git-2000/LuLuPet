using System.Text.Json;

namespace LuluPet.Core.Clipboard;

public sealed class ClipboardHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _historyPath;

    public ClipboardHistoryStore(string historyPath)
    {
        if (string.IsNullOrWhiteSpace(historyPath))
        {
            throw new ArgumentException("History path is required.", nameof(historyPath));
        }

        _historyPath = historyPath;
    }

    public IReadOnlyList<ClipboardHistoryItem> Load()
    {
        if (!File.Exists(_historyPath))
        {
            return Array.Empty<ClipboardHistoryItem>();
        }

        try
        {
            var json = File.ReadAllText(_historyPath);
            return JsonSerializer.Deserialize<ClipboardHistoryItem[]>(json, JsonOptions)
                ?? Array.Empty<ClipboardHistoryItem>();
        }
        catch (Exception exception) when (exception is JsonException
            or IOException
            or UnauthorizedAccessException
            or System.Security.SecurityException)
        {
            return Array.Empty<ClipboardHistoryItem>();
        }
    }

    public void Save(IEnumerable<ClipboardHistoryItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var directory = Path.GetDirectoryName(_historyPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(items.Take(100).ToArray(), JsonOptions);
        File.WriteAllText(_historyPath, json);
    }
}

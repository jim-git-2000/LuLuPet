using System.Text.Json;
using LuluPet.Core.Behavior;

namespace LuluPet.Core.Dialogues;

public sealed class DialogueLineProvider
{
    private static readonly string[] BuiltInFallbackLines =
    {
        "Hi, I am Lulu.",
        "Click again?",
        "I am right here.",
        "Time for a tiny break."
    };

    private readonly IPetRandom _random;
    private readonly IReadOnlyList<string> _lines;

    public DialogueLineProvider(
        string linesPath,
        IPetRandom? random = null,
        IReadOnlyList<string>? fallbackLines = null)
    {
        if (string.IsNullOrWhiteSpace(linesPath))
        {
            throw new ArgumentException("Dialogue lines path is required.", nameof(linesPath));
        }

        _random = random ?? new SystemPetRandom();
        _lines = LoadLines(linesPath, fallbackLines ?? BuiltInFallbackLines);
    }

    public IReadOnlyList<string> Lines => _lines;

    public string GetRandomLine()
    {
        if (_lines.Count == 0)
        {
            return string.Empty;
        }

        return _lines[_random.NextInt(0, _lines.Count)];
    }

    private static IReadOnlyList<string> LoadLines(string linesPath, IReadOnlyList<string> fallbackLines)
    {
        try
        {
            if (!File.Exists(linesPath))
            {
                return NormalizeLines(fallbackLines);
            }

            using var stream = File.OpenRead(linesPath);
            using var document = JsonDocument.Parse(stream);
            var lines = ReadLines(document.RootElement);
            var normalizedLines = NormalizeLines(lines);
            return normalizedLines.Count > 0 ? normalizedLines : NormalizeLines(fallbackLines);
        }
        catch (JsonException)
        {
            return NormalizeLines(fallbackLines);
        }
        catch (IOException)
        {
            return NormalizeLines(fallbackLines);
        }
        catch (UnauthorizedAccessException)
        {
            return NormalizeLines(fallbackLines);
        }
    }

    private static IReadOnlyList<string> ReadLines(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return ReadStringArray(root);
        }

        if (root.ValueKind == JsonValueKind.Object
            && root.TryGetProperty("lines", out var linesElement)
            && linesElement.ValueKind == JsonValueKind.Array)
        {
            return ReadStringArray(linesElement);
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement arrayElement)
    {
        var lines = new List<string>();

        foreach (var item in arrayElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                lines.Add(item.GetString() ?? string.Empty);
            }
        }

        return lines;
    }

    private static IReadOnlyList<string> NormalizeLines(IReadOnlyList<string> lines)
    {
        return lines
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}

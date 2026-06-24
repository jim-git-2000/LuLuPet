namespace LuluPet.Core.Clipboard;

public sealed record ClipboardHistoryItem(
    string Text,
    DateTimeOffset CapturedAt);

namespace LuluPet.Core.Config;

public sealed class ToolPanelSettings
{
    public const int DefaultClipboardHistoryLimit = 100;
    public const int MinClipboardHistoryLimit = 1;
    public const int MaxClipboardHistoryLimit = 100;

    public int ClipboardHistoryLimit { get; set; } = DefaultClipboardHistoryLimit;

    public bool ClipboardPersistEnabled { get; set; }

    public string FileTransitFolderPath { get; set; } = string.Empty;

    public bool FileTransitCopyOnDrop { get; set; } = true;

    public static void Normalize(ToolPanelSettings settings)
    {
        settings.ClipboardHistoryLimit = Clamp(
            settings.ClipboardHistoryLimit,
            min: MinClipboardHistoryLimit,
            max: MaxClipboardHistoryLimit,
            fallback: DefaultClipboardHistoryLimit);

        if (settings.FileTransitFolderPath is null)
        {
            settings.FileTransitFolderPath = string.Empty;
        }
    }

    private static int Clamp(int value, int min, int max, int fallback)
    {
        if (value <= 0)
        {
            return fallback;
        }

        return Math.Clamp(value, min, max);
    }
}

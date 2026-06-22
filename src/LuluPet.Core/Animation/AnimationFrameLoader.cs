namespace LuluPet.Core.Animation;

public static class AnimationFrameLoader
{
    public static IReadOnlyList<string> LoadPngFrames(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return Array.Empty<string>();
        }

        return Directory
            .EnumerateFiles(directoryPath, "*.png", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}


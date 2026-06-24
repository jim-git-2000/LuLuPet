namespace LuluPet.Core.FileTransit;

public static class FileTransitPathResolver
{
    public static string GetAvailableDestinationPath(
        string folderPath,
        string sourceFileName,
        Func<string, bool> exists)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            throw new ArgumentException("Folder path is required.", nameof(folderPath));
        }

        if (string.IsNullOrWhiteSpace(sourceFileName))
        {
            throw new ArgumentException("Source file name is required.", nameof(sourceFileName));
        }

        ArgumentNullException.ThrowIfNull(exists);

        var fileName = Path.GetFileName(sourceFileName);
        var candidate = Path.Combine(folderPath, fileName);
        if (!exists(candidate))
        {
            return candidate;
        }

        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName);
        for (var index = 1; index < int.MaxValue; index++)
        {
            candidate = Path.Combine(folderPath, $"{name} ({index}){extension}");
            if (!exists(candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Unable to find an available destination path.");
    }
}

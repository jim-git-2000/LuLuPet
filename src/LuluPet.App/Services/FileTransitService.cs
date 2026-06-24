using System.Diagnostics;
using LuluPet.Core.FileTransit;
using LuluPet.Core.Storage;

namespace LuluPet.App.Services;

public sealed class FileTransitService
{
    private readonly string _folderPath;

    public FileTransitService(string? configuredFolderPath)
    {
        _folderPath = string.IsNullOrWhiteSpace(configuredFolderPath)
            ? LuluPetDataPaths.GetDefaultFileTransitFolderPath()
            : configuredFolderPath;
    }

    public string FolderPath => _folderPath;

    public FileTransitSnapshot LoadSnapshot()
    {
        EnsureFolderExists();

        var items = Directory.EnumerateFiles(_folderPath)
            .Select(CreateItem)
            .OrderByDescending(item => item.ModifiedAt)
            .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();

        return new FileTransitSnapshot(_folderPath, items);
    }

    public void OpenFolder()
    {
        EnsureFolderExists();

        var startInfo = new ProcessStartInfo
        {
            FileName = _folderPath,
            UseShellExecute = true
        };

        Process.Start(startInfo);
    }

    public void EnsureFolderExists()
    {
        Directory.CreateDirectory(_folderPath);
    }

    public static string FormatSize(long sizeBytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var size = Math.Max(0, sizeBytes);
        var value = (double)size;
        var unitIndex = 0;

        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }

        return unitIndex == 0
            ? $"{size} {units[unitIndex]}"
            : $"{value:0.#} {units[unitIndex]}";
    }

    private static FileTransitItem CreateItem(string path)
    {
        var info = new FileInfo(path);
        return new FileTransitItem(
            info.Name,
            info.FullName,
            info.Length,
            info.LastWriteTime);
    }
}

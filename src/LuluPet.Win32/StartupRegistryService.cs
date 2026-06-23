using Microsoft.Win32;
using System.Runtime.Versioning;

namespace LuluPet.Win32;

public static class StartupRegistryService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    [SupportedOSPlatform("windows")]
    public static bool IsEnabled(string appName)
    {
        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        return runKey?.GetValue(appName) is string value
            && !string.IsNullOrWhiteSpace(value);
    }

    [SupportedOSPlatform("windows")]
    public static void SetEnabled(string appName, string executablePath, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            throw new ArgumentException("Application name is required.", nameof(appName));
        }

        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);

        if (enabled)
        {
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new ArgumentException("Executable path is required.", nameof(executablePath));
            }

            runKey.SetValue(appName, QuoteExecutablePath(executablePath), RegistryValueKind.String);
        }
        else
        {
            runKey.DeleteValue(appName, throwOnMissingValue: false);
        }
    }

    public static string QuoteExecutablePath(string executablePath)
    {
        var trimmed = executablePath.Trim();
        return trimmed.StartsWith('"') && trimmed.EndsWith('"')
            ? trimmed
            : $"\"{trimmed}\"";
    }
}

using System.Text.Json;

namespace LuluPet.Core.Config;

public sealed class JsonSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public JsonSettingsStore(string settingsPath)
    {
        if (string.IsNullOrWhiteSpace(settingsPath))
        {
            throw new ArgumentException("Settings path is required.", nameof(settingsPath));
        }

        _settingsPath = settingsPath;
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return AppSettings.CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return Normalize(settings);
        }
        catch (JsonException)
        {
            return AppSettings.CreateDefault();
        }
        catch (IOException)
        {
            return AppSettings.CreateDefault();
        }
        catch (UnauthorizedAccessException)
        {
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        var normalized = Normalize(settings);
        var directory = Path.GetDirectoryName(_settingsPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(normalized, JsonOptions);
        File.WriteAllText(_settingsPath, json);
    }

    private static AppSettings Normalize(AppSettings? settings)
    {
        var normalized = settings ?? AppSettings.CreateDefault();
        normalized.Window ??= new WindowSettings();
        normalized.Appearance ??= new AppearanceSettings();
        normalized.Audio ??= new AudioSettings();
        normalized.Interaction ??= new InteractionSettings();
        normalized.Startup ??= new StartupSettings();

        if (!double.IsFinite(normalized.Window.Left))
        {
            normalized.Window.Left = 1200;
        }

        if (!double.IsFinite(normalized.Window.Top))
        {
            normalized.Window.Top = 650;
        }

        normalized.Appearance.Scale = ClampFinite(
            normalized.Appearance.Scale,
            min: 0.3,
            max: 1.5,
            fallback: 1.0);
        normalized.Appearance.Opacity = ClampFinite(
            normalized.Appearance.Opacity,
            min: 0.35,
            max: 1.0,
            fallback: 1.0);
        normalized.Audio.Volume = ClampFinite(
            normalized.Audio.Volume,
            min: 0.0,
            max: 1.0,
            fallback: 0.6);

        return normalized;
    }

    private static double ClampFinite(double value, double min, double max, double fallback)
    {
        if (!double.IsFinite(value))
        {
            return fallback;
        }

        return Math.Clamp(value, min, max);
    }
}

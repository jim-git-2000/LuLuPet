using LuluPet.Core.Config;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class P9SettingsTests
{
    [Fact]
    public void JsonSettingsStore_LoadsP9Defaults_WhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "settings.json");
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.Equal(1.0, settings.Appearance.Scale);
        Assert.Equal(1.0, settings.Appearance.Opacity);
        Assert.Equal(0.6, settings.Audio.Volume);
    }

    [Fact]
    public void JsonSettingsStore_SavesAndLoadsP9Settings()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var path = Path.Combine(directory, "settings.json");
        var store = new JsonSettingsStore(path);

        store.Save(new AppSettings
        {
            Appearance = new AppearanceSettings
            {
                Scale = 1.25,
                Opacity = 0.8
            },
            Audio = new AudioSettings
            {
                Volume = 0.4
            }
        });

        var settings = store.Load();

        Assert.Equal(1.25, settings.Appearance.Scale);
        Assert.Equal(0.8, settings.Appearance.Opacity);
        Assert.Equal(0.4, settings.Audio.Volume);
    }

    [Fact]
    public void JsonSettingsStore_ClampsP9Settings_WhenValuesAreOutOfRange()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, """
            {
              "appearance": {
                "scale": 9,
                "opacity": -1
              },
              "audio": {
                "volume": 2
              }
            }
            """);
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.Equal(1.5, settings.Appearance.Scale);
        Assert.Equal(0.35, settings.Appearance.Opacity);
        Assert.Equal(1.0, settings.Audio.Volume);
    }
}

using LuluPet.Core.Config;
using LuluPet.Win32;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class P7SettingsTests
{
    [Fact]
    public void JsonSettingsStore_LoadsP7Defaults_WhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "settings.json");
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.False(settings.Interaction.ClickThrough);
        Assert.False(settings.Startup.AutoStart);
    }

    [Fact]
    public void JsonSettingsStore_SavesAndLoadsP7Settings()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var path = Path.Combine(directory, "settings.json");
        var store = new JsonSettingsStore(path);

        store.Save(new AppSettings
        {
            Interaction = new InteractionSettings
            {
                ClickThrough = true
            },
            Startup = new StartupSettings
            {
                AutoStart = true
            }
        });

        var settings = store.Load();

        Assert.True(settings.Interaction.ClickThrough);
        Assert.True(settings.Startup.AutoStart);
    }

    [Fact]
    public void QuoteExecutablePath_AddsQuotes_WhenNeeded()
    {
        var quoted = StartupRegistryService.QuoteExecutablePath(@"C:\Program Files\LuluPet\LuluPet.exe");

        Assert.Equal(@"""C:\Program Files\LuluPet\LuluPet.exe""", quoted);
    }

    [Fact]
    public void QuoteExecutablePath_KeepsExistingQuotes()
    {
        var quoted = StartupRegistryService.QuoteExecutablePath(@"""C:\LuluPet\LuluPet.exe""");

        Assert.Equal(@"""C:\LuluPet\LuluPet.exe""", quoted);
    }
}

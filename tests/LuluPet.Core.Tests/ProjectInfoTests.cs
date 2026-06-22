using LuluPet.Core;
using LuluPet.Core.Config;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class ProjectInfoTests
{
    [Fact]
    public void ProductName_IsLuluPet()
    {
        Assert.Equal("LuluPet", ProjectInfo.ProductName);
    }

    [Fact]
    public void JsonSettingsStore_LoadsDefaults_WhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "settings.json");
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.Equal(1200, settings.Window.Left);
        Assert.Equal(650, settings.Window.Top);
    }

    [Fact]
    public void JsonSettingsStore_SavesAndLoadsWindowPosition()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var path = Path.Combine(directory, "settings.json");
        var store = new JsonSettingsStore(path);

        store.Save(new AppSettings
        {
            Window = new WindowSettings
            {
                Left = 321,
                Top = 654
            }
        });

        var settings = store.Load();

        Assert.Equal(321, settings.Window.Left);
        Assert.Equal(654, settings.Window.Top);
    }

    [Fact]
    public void JsonSettingsStore_LoadsDefaults_WhenJsonIsInvalid()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ invalid json");

        var store = new JsonSettingsStore(path);
        var settings = store.Load();

        Assert.Equal(1200, settings.Window.Left);
        Assert.Equal(650, settings.Window.Top);
    }
}

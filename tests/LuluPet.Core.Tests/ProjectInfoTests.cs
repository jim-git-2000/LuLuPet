using LuluPet.Core;
using LuluPet.Core.Animation;
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

    [Fact]
    public void FrameAnimationPlayer_LoopsAtEndOfAction()
    {
        var player = new FrameAnimationPlayer();
        player.LoadAction("Idle", ["idle-1.png", "idle-2.png"], fps: 2);
        player.Play("Idle");

        player.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(1, player.CurrentFrameIndex);

        player.Tick(TimeSpan.FromMilliseconds(500));
        Assert.Equal(0, player.CurrentFrameIndex);
        Assert.True(player.IsPlaying);
    }

    [Fact]
    public void FrameAnimationPlayer_SwitchesActionFromFirstFrame()
    {
        var player = new FrameAnimationPlayer();
        player.LoadAction("Idle", ["idle-1.png", "idle-2.png"], fps: 2);
        player.LoadAction("Sleep", ["sleep-1.png", "sleep-2.png"], fps: 2);

        player.Play("Idle");
        player.Tick(TimeSpan.FromMilliseconds(500));
        player.Play("Sleep");

        Assert.Equal("Sleep", player.CurrentAction);
        Assert.Equal(0, player.CurrentFrameIndex);
        Assert.Equal("sleep-1.png", player.CurrentFramePath);
    }

    [Fact]
    public void FrameAnimationPlayer_StopsAtLastFrame_WhenLoopDisabled()
    {
        var player = new FrameAnimationPlayer();
        player.LoadAction("Once", ["once-1.png", "once-2.png"], fps: 2, loop: false);
        player.Play("Once");

        player.Tick(TimeSpan.FromMilliseconds(500));
        player.Tick(TimeSpan.FromMilliseconds(500));

        Assert.Equal(1, player.CurrentFrameIndex);
        Assert.False(player.IsPlaying);
    }

    [Fact]
    public void AnimationFrameLoader_LoadsPngFramesInNameOrder()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        var second = Path.Combine(directory, "lulu_idle_0002.png");
        var first = Path.Combine(directory, "lulu_idle_0001.png");
        File.WriteAllText(second, string.Empty);
        File.WriteAllText(first, string.Empty);

        var frames = AnimationFrameLoader.LoadPngFrames(directory);

        Assert.Equal([first, second], frames);
    }
}

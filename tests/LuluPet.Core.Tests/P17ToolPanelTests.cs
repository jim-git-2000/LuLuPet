using LuluPet.Core.Clipboard;
using LuluPet.Core.Config;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class P17ToolPanelTests
{
    [Fact]
    public void JsonSettingsStore_LoadsP17ToolPanelDefaults_WhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "settings.json");
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.Equal(100, settings.ToolPanels.ClipboardHistoryLimit);
        Assert.False(settings.ToolPanels.ClipboardPersistEnabled);
        Assert.Equal(string.Empty, settings.ToolPanels.FileTransitFolderPath);
        Assert.True(settings.ToolPanels.FileTransitCopyOnDrop);
    }

    [Fact]
    public void JsonSettingsStore_PreservesExistingSettings_WhenToolPanelSectionIsMissing()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, """
            {
              "window": {
                "left": -320,
                "top": 720
              },
              "appearance": {
                "scale": 1.2,
                "opacity": 0.8
              },
              "audio": {
                "volume": 0.35
              },
              "interaction": {
                "clickThrough": true
              },
              "startup": {
                "autoStart": true
              },
              "reminders": {
                "waterReminderEnabled": false
              }
            }
            """);
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.Equal(-320, settings.Window.Left);
        Assert.Equal(720, settings.Window.Top);
        Assert.Equal(1.2, settings.Appearance.Scale);
        Assert.Equal(0.8, settings.Appearance.Opacity);
        Assert.Equal(0.35, settings.Audio.Volume);
        Assert.True(settings.Interaction.ClickThrough);
        Assert.True(settings.Startup.AutoStart);
        Assert.False(settings.Reminders.WaterReminderEnabled);
        Assert.Equal(100, settings.ToolPanels.ClipboardHistoryLimit);
        Assert.True(settings.ToolPanels.FileTransitCopyOnDrop);
    }

    [Fact]
    public void JsonSettingsStore_ClampsP17ToolPanelSettings_WhenValuesAreOutOfRange()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, """
            {
              "toolPanels": {
                "clipboardHistoryLimit": 999,
                "clipboardPersistEnabled": true,
                "fileTransitFolderPath": null,
                "fileTransitCopyOnDrop": false
              }
            }
            """);
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.Equal(100, settings.ToolPanels.ClipboardHistoryLimit);
        Assert.True(settings.ToolPanels.ClipboardPersistEnabled);
        Assert.Equal(string.Empty, settings.ToolPanels.FileTransitFolderPath);
        Assert.False(settings.ToolPanels.FileTransitCopyOnDrop);
    }

    [Fact]
    public void ClipboardHistory_KeepsLatestItemsWithinCapacity()
    {
        var history = new ClipboardHistory(capacity: 100);
        var capturedAt = DateTimeOffset.Parse("2026-06-24T00:00:00Z");

        for (var index = 1; index <= 120; index++)
        {
            history.AddText($"item {index}", capturedAt.AddSeconds(index));
        }

        var snapshot = history.Snapshot();

        Assert.Equal(100, snapshot.Count);
        Assert.Equal("item 120", snapshot[0].Text);
        Assert.Equal("item 21", snapshot[^1].Text);
    }

    [Fact]
    public void ClipboardHistory_MovesDuplicateTextToTopWithoutGrowing()
    {
        var history = new ClipboardHistory(capacity: 100);
        var first = DateTimeOffset.Parse("2026-06-24T00:00:00Z");
        var second = first.AddMinutes(1);

        history.AddText("alpha", first);
        history.AddText("beta", first.AddSeconds(1));
        history.AddText("alpha", second);

        var snapshot = history.Snapshot();

        Assert.Equal(2, snapshot.Count);
        Assert.Equal("alpha", snapshot[0].Text);
        Assert.Equal(second, snapshot[0].CapturedAt);
        Assert.Equal("beta", snapshot[1].Text);
    }

    [Fact]
    public void ClipboardHistory_IgnoresBlankText()
    {
        var history = new ClipboardHistory(capacity: 100);

        var addedNull = history.AddText(null, DateTimeOffset.UtcNow);
        var addedEmpty = history.AddText("", DateTimeOffset.UtcNow);
        var addedWhitespace = history.AddText("   ", DateTimeOffset.UtcNow);

        Assert.False(addedNull);
        Assert.False(addedEmpty);
        Assert.False(addedWhitespace);
        Assert.Empty(history.Snapshot());
    }

    [Fact]
    public void ClipboardHistory_ClearRemovesAllItems()
    {
        var history = new ClipboardHistory(capacity: 100);
        history.AddText("alpha", DateTimeOffset.UtcNow);
        history.AddText("beta", DateTimeOffset.UtcNow);

        history.Clear();

        Assert.Empty(history.Snapshot());
    }
}

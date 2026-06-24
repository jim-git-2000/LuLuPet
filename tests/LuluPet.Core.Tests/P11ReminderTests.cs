using LuluPet.Core.Config;
using LuluPet.Core.Reminders;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class P11ReminderTests
{
    [Fact]
    public void JsonSettingsStore_LoadsP11ReminderDefaults_WhenFileDoesNotExist()
    {
        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName(), "settings.json");
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.False(settings.Reminders.PomodoroEnabled);
        Assert.Equal(25, settings.Reminders.PomodoroMinutes);
        Assert.Equal(5, settings.Reminders.PomodoroRestMinutes);
        Assert.True(settings.Reminders.WaterReminderEnabled);
        Assert.Equal(45, settings.Reminders.WaterReminderMinutes);
        Assert.True(settings.Reminders.StandReminderEnabled);
        Assert.Equal(60, settings.Reminders.StandReminderMinutes);
    }

    [Fact]
    public void JsonSettingsStore_PreservesExistingSettings_WhenReminderSectionIsMissing()
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
        Assert.Equal(25, settings.Reminders.PomodoroMinutes);
        Assert.True(settings.Reminders.WaterReminderEnabled);
    }

    [Fact]
    public void JsonSettingsStore_ClampsP11ReminderSettings_WhenValuesAreOutOfRange()
    {
        var directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, """
            {
              "reminders": {
                "pomodoroMinutes": 999,
                "pomodoroRestMinutes": 0,
                "waterReminderMinutes": -5,
                "standReminderMinutes": 999
              }
            }
            """);
        var store = new JsonSettingsStore(path);

        var settings = store.Load();

        Assert.Equal(240, settings.Reminders.PomodoroMinutes);
        Assert.Equal(5, settings.Reminders.PomodoroRestMinutes);
        Assert.Equal(45, settings.Reminders.WaterReminderMinutes);
        Assert.Equal(240, settings.Reminders.StandReminderMinutes);
    }

    [Fact]
    public void ReminderScheduler_StartsPomodoroInFocus_ThenCyclesFocusAndRest()
    {
        var scheduler = new ReminderScheduler(new ReminderSettings
        {
            PomodoroMinutes = 1,
            PomodoroRestMinutes = 1,
            WaterReminderEnabled = false,
            StandReminderEnabled = false
        });

        var started = scheduler.SetPomodoroEnabled(true);
        var focusEvents = scheduler.Tick(TimeSpan.FromMinutes(1));
        var restEvents = scheduler.Tick(TimeSpan.FromMinutes(1));

        Assert.Equal(ReminderKind.PomodoroStarted, started?.Kind);
        Assert.Equal(PomodoroPhase.Focus, scheduler.PomodoroPhase);
        Assert.Collection(
            focusEvents,
            reminder => Assert.Equal(ReminderKind.PomodoroFocusDone, reminder.Kind));
        Assert.Collection(
            restEvents,
            reminder => Assert.Equal(ReminderKind.PomodoroRestDone, reminder.Kind));
        Assert.Equal(PomodoroPhase.Focus, scheduler.PomodoroPhase);
        Assert.Equal(TimeSpan.FromMinutes(1), scheduler.PomodoroRemaining);
    }

    [Fact]
    public void ReminderScheduler_TriggersWaterAndStandRemindersIndependently()
    {
        var scheduler = new ReminderScheduler(new ReminderSettings
        {
            PomodoroEnabled = false,
            WaterReminderEnabled = true,
            WaterReminderMinutes = 1,
            StandReminderEnabled = true,
            StandReminderMinutes = 2
        });

        var firstMinuteEvents = scheduler.Tick(TimeSpan.FromMinutes(1));
        var secondMinuteEvents = scheduler.Tick(TimeSpan.FromMinutes(1));

        Assert.Collection(
            firstMinuteEvents,
            reminder => Assert.Equal(ReminderKind.Water, reminder.Kind));
        Assert.Collection(
            secondMinuteEvents,
            reminder => Assert.Equal(ReminderKind.Water, reminder.Kind),
            reminder => Assert.Equal(ReminderKind.Stand, reminder.Kind));
    }

    [Fact]
    public void ReminderScheduler_LargeTick_EmitsAtMostOneEventPerReminderKind()
    {
        var scheduler = new ReminderScheduler(new ReminderSettings
        {
            PomodoroEnabled = true,
            PomodoroMinutes = 1,
            PomodoroRestMinutes = 1,
            WaterReminderEnabled = true,
            WaterReminderMinutes = 1,
            StandReminderEnabled = true,
            StandReminderMinutes = 1
        });

        var events = scheduler.Tick(TimeSpan.FromMinutes(30));

        Assert.Collection(
            events,
            reminder => Assert.Equal(ReminderKind.PomodoroFocusDone, reminder.Kind),
            reminder => Assert.Equal(ReminderKind.Water, reminder.Kind),
            reminder => Assert.Equal(ReminderKind.Stand, reminder.Kind));
        Assert.Equal(PomodoroPhase.Rest, scheduler.PomodoroPhase);
        Assert.Equal(TimeSpan.FromMinutes(1), scheduler.PomodoroRemaining);
        Assert.Equal(TimeSpan.FromMinutes(1), scheduler.WaterRemaining);
        Assert.Equal(TimeSpan.FromMinutes(1), scheduler.StandRemaining);
    }

    [Fact]
    public void ReminderScheduler_DisablesReminders()
    {
        var scheduler = new ReminderScheduler(new ReminderSettings
        {
            PomodoroEnabled = true,
            PomodoroMinutes = 1,
            WaterReminderEnabled = true,
            WaterReminderMinutes = 1,
            StandReminderEnabled = true,
            StandReminderMinutes = 1
        });

        var stopped = scheduler.SetPomodoroEnabled(false);
        scheduler.SetWaterReminderEnabled(false);
        scheduler.SetStandReminderEnabled(false);
        var events = scheduler.Tick(TimeSpan.FromMinutes(5));

        Assert.Equal(ReminderKind.PomodoroStopped, stopped?.Kind);
        Assert.Equal(PomodoroPhase.Off, scheduler.PomodoroPhase);
        Assert.Equal(TimeSpan.Zero, scheduler.PomodoroRemaining);
        Assert.Equal(TimeSpan.Zero, scheduler.WaterRemaining);
        Assert.Equal(TimeSpan.Zero, scheduler.StandRemaining);
        Assert.Empty(events);
    }
}

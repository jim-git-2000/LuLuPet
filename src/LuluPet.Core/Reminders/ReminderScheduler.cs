using System;
using System.Collections.Generic;

namespace LuluPet.Core.Reminders;

public sealed class ReminderScheduler
{
    private ReminderSettings _settings;

    public ReminderScheduler(ReminderSettings? settings = null)
    {
        _settings = (settings ?? new ReminderSettings()).Clone();
        ReminderSettings.Normalize(_settings);
        ResetFromSettings();
    }

    public PomodoroPhase PomodoroPhase { get; private set; }

    public TimeSpan PomodoroRemaining { get; private set; }

    public TimeSpan WaterRemaining { get; private set; }

    public TimeSpan StandRemaining { get; private set; }

    public ReminderSettings Settings => _settings.Clone();

    public ReminderEvent? SetPomodoroEnabled(bool enabled)
    {
        if (enabled)
        {
            if (PomodoroPhase != PomodoroPhase.Off)
            {
                return null;
            }

            _settings.PomodoroEnabled = true;
            StartFocus();
            return CreatePomodoroStartedEvent();
        }

        if (PomodoroPhase == PomodoroPhase.Off)
        {
            _settings.PomodoroEnabled = false;
            return null;
        }

        _settings.PomodoroEnabled = false;
        PomodoroPhase = PomodoroPhase.Off;
        PomodoroRemaining = TimeSpan.Zero;
        return CreatePomodoroStoppedEvent();
    }

    public void SetWaterReminderEnabled(bool enabled)
    {
        _settings.WaterReminderEnabled = enabled;
        WaterRemaining = enabled ? Minutes(_settings.WaterReminderMinutes) : TimeSpan.Zero;
    }

    public void SetStandReminderEnabled(bool enabled)
    {
        _settings.StandReminderEnabled = enabled;
        StandRemaining = enabled ? Minutes(_settings.StandReminderMinutes) : TimeSpan.Zero;
    }

    public void ApplySettings(ReminderSettings settings)
    {
        var nextSettings = settings.Clone();
        ReminderSettings.Normalize(nextSettings);

        var resetWater = nextSettings.WaterReminderEnabled != _settings.WaterReminderEnabled
            || nextSettings.WaterReminderMinutes != _settings.WaterReminderMinutes;
        var resetStand = nextSettings.StandReminderEnabled != _settings.StandReminderEnabled
            || nextSettings.StandReminderMinutes != _settings.StandReminderMinutes;
        var pomodoroWasOff = PomodoroPhase == PomodoroPhase.Off;

        _settings = nextSettings;

        if (!_settings.PomodoroEnabled)
        {
            PomodoroPhase = PomodoroPhase.Off;
            PomodoroRemaining = TimeSpan.Zero;
        }
        else if (pomodoroWasOff)
        {
            StartFocus();
        }

        if (resetWater)
        {
            WaterRemaining = _settings.WaterReminderEnabled
                ? Minutes(_settings.WaterReminderMinutes)
                : TimeSpan.Zero;
        }

        if (resetStand)
        {
            StandRemaining = _settings.StandReminderEnabled
                ? Minutes(_settings.StandReminderMinutes)
                : TimeSpan.Zero;
        }
    }

    public IReadOnlyList<ReminderEvent> Tick(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
        {
            return Array.Empty<ReminderEvent>();
        }

        var events = new List<ReminderEvent>(capacity: 3);

        TickPomodoro(elapsed, events);
        TickWater(elapsed, events);
        TickStand(elapsed, events);

        return events;
    }

    private void ResetFromSettings()
    {
        PomodoroPhase = PomodoroPhase.Off;
        PomodoroRemaining = TimeSpan.Zero;

        if (_settings.PomodoroEnabled)
        {
            StartFocus();
        }

        WaterRemaining = _settings.WaterReminderEnabled
            ? Minutes(_settings.WaterReminderMinutes)
            : TimeSpan.Zero;
        StandRemaining = _settings.StandReminderEnabled
            ? Minutes(_settings.StandReminderMinutes)
            : TimeSpan.Zero;
    }

    private void TickPomodoro(TimeSpan elapsed, ICollection<ReminderEvent> events)
    {
        if (PomodoroPhase == PomodoroPhase.Off)
        {
            return;
        }

        PomodoroRemaining -= elapsed;
        if (PomodoroRemaining > TimeSpan.Zero)
        {
            return;
        }

        if (PomodoroPhase == PomodoroPhase.Focus)
        {
            events.Add(CreatePomodoroFocusDoneEvent());
            PomodoroPhase = PomodoroPhase.Rest;
            PomodoroRemaining = Minutes(_settings.PomodoroRestMinutes);
            return;
        }

        events.Add(CreatePomodoroRestDoneEvent());
        StartFocus();
    }

    private void TickWater(TimeSpan elapsed, ICollection<ReminderEvent> events)
    {
        if (!_settings.WaterReminderEnabled)
        {
            return;
        }

        WaterRemaining -= elapsed;
        if (WaterRemaining > TimeSpan.Zero)
        {
            return;
        }

        events.Add(new ReminderEvent(
            ReminderKind.Water,
            "噜噜提醒你喝水啦。",
            "reminder_water",
            "Happy"));
        WaterRemaining = Minutes(_settings.WaterReminderMinutes);
    }

    private void TickStand(TimeSpan elapsed, ICollection<ReminderEvent> events)
    {
        if (!_settings.StandReminderEnabled)
        {
            return;
        }

        StandRemaining -= elapsed;
        if (StandRemaining > TimeSpan.Zero)
        {
            return;
        }

        events.Add(new ReminderEvent(
            ReminderKind.Stand,
            "起来活动一下，伸个懒腰吧。",
            "reminder_stand",
            "Happy"));
        StandRemaining = Minutes(_settings.StandReminderMinutes);
    }

    private void StartFocus()
    {
        PomodoroPhase = PomodoroPhase.Focus;
        PomodoroRemaining = Minutes(_settings.PomodoroMinutes);
    }

    private static TimeSpan Minutes(int minutes)
    {
        return TimeSpan.FromMinutes(minutes);
    }

    private static ReminderEvent CreatePomodoroStartedEvent()
    {
        return new ReminderEvent(
            ReminderKind.PomodoroStarted,
            "番茄钟开始，噜噜陪你专注。",
            "pomodoro_start",
            "Happy");
    }

    private static ReminderEvent CreatePomodoroStoppedEvent()
    {
        return new ReminderEvent(
            ReminderKind.PomodoroStopped,
            "番茄钟已关闭。",
            "pomodoro_stop",
            "Idle");
    }

    private static ReminderEvent CreatePomodoroFocusDoneEvent()
    {
        return new ReminderEvent(
            ReminderKind.PomodoroFocusDone,
            "番茄钟结束啦，休息一下吧。",
            "pomodoro_focus_done",
            "Happy");
    }

    private static ReminderEvent CreatePomodoroRestDoneEvent()
    {
        return new ReminderEvent(
            ReminderKind.PomodoroRestDone,
            "休息结束，新的番茄钟开始啦。",
            "pomodoro_rest_done",
            "Surprised");
    }
}

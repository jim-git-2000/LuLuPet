using System;

namespace LuluPet.Core.Reminders;

public sealed class ReminderSettings
{
    public const int DefaultPomodoroMinutes = 25;
    public const int DefaultPomodoroRestMinutes = 5;
    public const int DefaultWaterReminderMinutes = 45;
    public const int DefaultStandReminderMinutes = 60;

    public bool PomodoroEnabled { get; set; }

    public int PomodoroMinutes { get; set; } = DefaultPomodoroMinutes;

    public int PomodoroRestMinutes { get; set; } = DefaultPomodoroRestMinutes;

    public bool WaterReminderEnabled { get; set; } = true;

    public int WaterReminderMinutes { get; set; } = DefaultWaterReminderMinutes;

    public bool StandReminderEnabled { get; set; } = true;

    public int StandReminderMinutes { get; set; } = DefaultStandReminderMinutes;

    public ReminderSettings Clone()
    {
        return new ReminderSettings
        {
            PomodoroEnabled = PomodoroEnabled,
            PomodoroMinutes = PomodoroMinutes,
            PomodoroRestMinutes = PomodoroRestMinutes,
            WaterReminderEnabled = WaterReminderEnabled,
            WaterReminderMinutes = WaterReminderMinutes,
            StandReminderEnabled = StandReminderEnabled,
            StandReminderMinutes = StandReminderMinutes
        };
    }

    public static void Normalize(ReminderSettings settings)
    {
        settings.PomodoroMinutes = Clamp(
            settings.PomodoroMinutes,
            min: 1,
            max: 240,
            fallback: DefaultPomodoroMinutes);
        settings.PomodoroRestMinutes = Clamp(
            settings.PomodoroRestMinutes,
            min: 1,
            max: 120,
            fallback: DefaultPomodoroRestMinutes);
        settings.WaterReminderMinutes = Clamp(
            settings.WaterReminderMinutes,
            min: 1,
            max: 240,
            fallback: DefaultWaterReminderMinutes);
        settings.StandReminderMinutes = Clamp(
            settings.StandReminderMinutes,
            min: 1,
            max: 240,
            fallback: DefaultStandReminderMinutes);
    }

    private static int Clamp(int value, int min, int max, int fallback)
    {
        if (value <= 0)
        {
            return fallback;
        }

        return Math.Clamp(value, min, max);
    }
}

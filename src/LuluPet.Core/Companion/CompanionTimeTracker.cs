using System;

namespace LuluPet.Core.Companion;

public sealed class CompanionTimeTracker
{
    private double _fractionalSeconds;

    public CompanionTimeTracker(DateOnly localDate, long totalSeconds)
    {
        LocalDate = localDate;
        TotalSeconds = Math.Max(0, totalSeconds);
    }

    public DateOnly LocalDate { get; private set; }

    public long TotalSeconds { get; private set; }

    public long Tick(TimeSpan elapsed, DateOnly currentLocalDate)
    {
        if (currentLocalDate != LocalDate)
        {
            LocalDate = currentLocalDate;
            TotalSeconds = 0;
            _fractionalSeconds = 0;
        }

        if (elapsed <= TimeSpan.Zero)
        {
            return 0;
        }

        _fractionalSeconds += elapsed.TotalSeconds;
        var wholeSeconds = (long)Math.Floor(_fractionalSeconds);
        if (wholeSeconds <= 0)
        {
            return 0;
        }

        _fractionalSeconds -= wholeSeconds;
        TotalSeconds += wholeSeconds;
        return wholeSeconds;
    }

    public void Reset(DateOnly localDate, long totalSeconds)
    {
        LocalDate = localDate;
        TotalSeconds = Math.Max(0, totalSeconds);
        _fractionalSeconds = 0;
    }

    public string FormatTrayText()
    {
        return FormatTrayText(TotalSeconds);
    }

    public static string FormatTrayText(long totalSeconds)
    {
        var normalizedSeconds = Math.Max(0, totalSeconds);
        var hours = normalizedSeconds / 3600;
        var minutes = normalizedSeconds % 3600 / 60;
        return $"噜噜今天已经陪伴：{hours}h {minutes}min";
    }
}

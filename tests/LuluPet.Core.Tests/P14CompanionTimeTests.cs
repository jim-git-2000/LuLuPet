using LuluPet.Core.Companion;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class P14CompanionTimeTests
{
    [Fact]
    public void CompanionTimeTracker_FormatsTrayText()
    {
        Assert.Equal(
            "噜噜今天已经陪伴：2h 5min",
            CompanionTimeTracker.FormatTrayText(2 * 3600 + 5 * 60 + 12));
    }

    [Fact]
    public void CompanionTimeTracker_AccumulatesWholeSeconds()
    {
        var tracker = new CompanionTimeTracker(new DateOnly(2026, 6, 24), 60);

        var first = tracker.Tick(TimeSpan.FromMilliseconds(500), new DateOnly(2026, 6, 24));
        var second = tracker.Tick(TimeSpan.FromMilliseconds(700), new DateOnly(2026, 6, 24));

        Assert.Equal(0, first);
        Assert.Equal(1, second);
        Assert.Equal(61, tracker.TotalSeconds);
    }

    [Fact]
    public void CompanionTimeTracker_ResetsWhenLocalDateChanges()
    {
        var tracker = new CompanionTimeTracker(new DateOnly(2026, 6, 24), 3600);

        var added = tracker.Tick(TimeSpan.FromSeconds(30), new DateOnly(2026, 6, 25));

        Assert.Equal(new DateOnly(2026, 6, 25), tracker.LocalDate);
        Assert.Equal(30, added);
        Assert.Equal(30, tracker.TotalSeconds);
    }
}

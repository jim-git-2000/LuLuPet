using System;

namespace LuluPet.Core.Companion;

public sealed record CompanionDayStats(
    DateOnly LocalDate,
    long TotalSeconds,
    DateTimeOffset UpdatedAtUtc);

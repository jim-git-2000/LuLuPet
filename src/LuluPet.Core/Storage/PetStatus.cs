namespace LuluPet.Core.Storage;

public sealed record PetStatus(
    string State,
    double WindowLeft,
    double WindowTop,
    DateTimeOffset UpdatedAtUtc);

namespace LuluPet.Core.Storage;

public sealed record PetInteraction(
    string InteractionType,
    string? Message,
    string State,
    double WindowLeft,
    double WindowTop,
    DateTimeOffset CreatedAtUtc);

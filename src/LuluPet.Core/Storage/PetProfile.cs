namespace LuluPet.Core.Storage;

public sealed record PetProfile(
    string Name,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    long InteractionCount);

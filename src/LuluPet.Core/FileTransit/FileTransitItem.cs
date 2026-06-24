namespace LuluPet.Core.FileTransit;

public sealed record FileTransitItem(
    string Name,
    string FullPath,
    long SizeBytes,
    DateTimeOffset ModifiedAt);

namespace LuluPet.Core.FileTransit;

public sealed record FileTransitSnapshot(
    string FolderPath,
    IReadOnlyList<FileTransitItem> Items);

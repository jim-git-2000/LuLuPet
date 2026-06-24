using LuluPet.Core.Storage;
using LuluPet.Core.FileTransit;
using Xunit;

namespace LuluPet.Core.Tests;

public sealed class P20FileTransitTests
{
    [Fact]
    public void LuluPetDataPaths_PlacesFileTransitFolderBesideDatabase()
    {
        var dataDirectory = LuluPetDataPaths.GetDefaultDataDirectory();
        var databasePath = LuluPetDataPaths.GetDefaultDatabasePath();
        var transitFolderPath = LuluPetDataPaths.GetDefaultFileTransitFolderPath();

        Assert.Equal(Path.Combine(dataDirectory, "lulupet.db"), databasePath);
        Assert.Equal(Path.Combine(dataDirectory, "Transit"), transitFolderPath);
    }

    [Fact]
    public void FileTransitPathResolver_ReturnsOriginalName_WhenDestinationDoesNotExist()
    {
        var destinationPath = FileTransitPathResolver.GetAvailableDestinationPath(
            "/transit",
            "report.pdf",
            _ => false);

        Assert.Equal(Path.Combine("/transit", "report.pdf"), destinationPath);
    }

    [Fact]
    public void FileTransitPathResolver_AppendsNumber_WhenDestinationExists()
    {
        var existing = new HashSet<string>
        {
            Path.Combine("/transit", "report.pdf"),
            Path.Combine("/transit", "report (1).pdf")
        };

        var destinationPath = FileTransitPathResolver.GetAvailableDestinationPath(
            "/transit",
            "report.pdf",
            existing.Contains);

        Assert.Equal(Path.Combine("/transit", "report (2).pdf"), destinationPath);
    }

    [Fact]
    public void FileTransitPathResolver_HandlesFilesWithoutExtension()
    {
        var existing = new HashSet<string>
        {
            Path.Combine("/transit", "notes")
        };

        var destinationPath = FileTransitPathResolver.GetAvailableDestinationPath(
            "/transit",
            "notes",
            existing.Contains);

        Assert.Equal(Path.Combine("/transit", "notes (1)"), destinationPath);
    }
}

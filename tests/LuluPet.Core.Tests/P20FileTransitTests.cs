using LuluPet.Core.Storage;
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
}

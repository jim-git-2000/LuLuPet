namespace LuluPet.Core.Storage;

public static class LuluPetDataPaths
{
    public static string GetDefaultDataDirectory()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = AppContext.BaseDirectory;
        }

        return Path.Combine(localAppData, "LuluPet", "data");
    }

    public static string GetDefaultDatabasePath()
    {
        return Path.Combine(GetDefaultDataDirectory(), "lulupet.db");
    }

    public static string GetDefaultFileTransitFolderPath()
    {
        return Path.Combine(GetDefaultDataDirectory(), "Transit");
    }
}

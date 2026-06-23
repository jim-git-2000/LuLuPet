namespace LuluPet.Core.Storage;

public static class LuluPetDataPaths
{
    public static string GetDefaultDatabasePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        if (string.IsNullOrWhiteSpace(localAppData))
        {
            localAppData = AppContext.BaseDirectory;
        }

        return Path.Combine(localAppData, "LuluPet", "data", "lulupet.db");
    }
}

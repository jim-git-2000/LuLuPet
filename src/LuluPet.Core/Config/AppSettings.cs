namespace LuluPet.Core.Config;

public sealed class AppSettings
{
    public WindowSettings Window { get; set; } = new();

    public static AppSettings CreateDefault()
    {
        return new AppSettings();
    }
}

public sealed class WindowSettings
{
    public double Left { get; set; } = 1200;

    public double Top { get; set; } = 650;
}


namespace LuluPet.Core.Config;

public sealed class AppSettings
{
    public WindowSettings Window { get; set; } = new();

    public InteractionSettings Interaction { get; set; } = new();

    public StartupSettings Startup { get; set; } = new();

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

public sealed class InteractionSettings
{
    public bool ClickThrough { get; set; }
}

public sealed class StartupSettings
{
    public bool AutoStart { get; set; }
}

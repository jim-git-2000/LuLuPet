using LuluPet.Core.Reminders;

namespace LuluPet.Core.Config;

public sealed class AppSettings
{
    public WindowSettings Window { get; set; } = new();

    public AppearanceSettings Appearance { get; set; } = new();

    public AudioSettings Audio { get; set; } = new();

    public InteractionSettings Interaction { get; set; } = new();

    public StartupSettings Startup { get; set; } = new();

    public ReminderSettings Reminders { get; set; } = new();

    public ToolPanelSettings ToolPanels { get; set; } = new();

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

public sealed class AppearanceSettings
{
    public double Scale { get; set; } = 1.0;

    public double Opacity { get; set; } = 1.0;
}

public sealed class AudioSettings
{
    public double Volume { get; set; } = 0.6;
}

public sealed class InteractionSettings
{
    public bool ClickThrough { get; set; }

    public bool WalkMode { get; set; }
}

public sealed class StartupSettings
{
    public bool AutoStart { get; set; }
}

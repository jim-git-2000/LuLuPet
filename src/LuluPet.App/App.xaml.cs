using System.IO;

namespace LuluPet.App;

public partial class App : System.Windows.Application
{
    public App()
    {
        DispatcherUnhandledException += (_, args) =>
        {
            LogUnhandledException(args.Exception);
            System.Windows.MessageBox.Show(
                "LuluPet 启动失败，错误已写入 crash.log。",
                "LuluPet",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception exception)
            {
                LogUnhandledException(exception);
            }
        };
    }

    private static void LogUnhandledException(Exception exception)
    {
        try
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "LuluPet");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, "crash.log");
            File.AppendAllText(
                logPath,
                $"[{DateTimeOffset.Now:O}]{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}

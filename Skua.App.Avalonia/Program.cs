using Avalonia;
using System;

namespace Skua.App.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Environment.CurrentDirectory = AppContext.BaseDirectory;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}

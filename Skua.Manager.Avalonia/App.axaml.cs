using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Skua.Core.AppStartup;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Services;
using Skua.Core.ViewModels.Manager;
using Skua.Manager.Avalonia.Services;
using System;

namespace Skua.Manager.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddCommonServices();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDispatcherService, DispatcherService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IClientFilesService, ClientFilesService>();
        services.AddSingleton<IThemeService, AvaloniaThemeService>();
        services.AddSingleton<BackgroundThemeService>();
        services.AddSkuaManagerViewModels();

        Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        Ioc.Default.GetRequiredService<ISettingsService>().SetApplicationVersion();
        IThemeService themeService = Ioc.Default.GetRequiredService<IThemeService>();
        themeService.ThemeChanged += OnThemeChanged;
        themeService.SchemeChanged += OnSchemeChanged;
        ApplyThemeFromService(themeService);

        IClientFilesService clientFiles = Ioc.Default.GetRequiredService<IClientFilesService>();
        clientFiles.CreateDirectories();
        clientFiles.CreateFiles();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Startup += OnStartup;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnStartup(object? sender, ControlledApplicationLifetimeStartupEventArgs e)
    {
        var mainWindow = new Views.MainWindow();
        mainWindow.Show();
    }

    private void OnThemeChanged(object? theme)
    {
        IThemeService themeService = Ioc.Default.GetRequiredService<IThemeService>();
        ISettingsService settings = Ioc.Default.GetRequiredService<ISettingsService>();
        RequestedThemeVariant = themeService.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        ApplyAccentBrushes(
            settings.Get("ManagerAccentColor", "#C9479A"),
            settings.Get("ManagerAccentForegroundColor", "#FFFFFFFF"));
    }

    private void OnSchemeChanged(Core.Models.ColorScheme scheme, object? color)
    {
        if (scheme == Core.Models.ColorScheme.PrimaryForeground)
            ApplyAccentBrushes(accentForegroundHex: color?.ToString());
        else
            ApplyAccentBrushes(accentHex: color?.ToString());
    }

    private void ApplyThemeFromService(IThemeService themeService)
    {
        ISettingsService settings = Ioc.Default.GetRequiredService<ISettingsService>();
        RequestedThemeVariant = themeService.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        ApplyAccentBrushes(
            settings.Get("ManagerAccentColor", "#C9479A"),
            settings.Get("ManagerAccentForegroundColor", "#FFFFFFFF"));
    }

    private void ApplyAccentBrushes(string? accentHex = null, string? accentForegroundHex = null)
    {
        Color accent;
        Color accentForeground;
        try
        {
            accent = Color.Parse(string.IsNullOrWhiteSpace(accentHex) ? GetResourceColor("SkuaAccentColor", "#C9479A").ToString() : accentHex);
        }
        catch
        {
            accent = Color.Parse("#C9479A");
        }
        try
        {
            accentForeground = Color.Parse(string.IsNullOrWhiteSpace(accentForegroundHex) ? GetResourceColor("SkuaAccentForegroundColor", "#FFFFFFFF").ToString() : accentForegroundHex);
        }
        catch
        {
            accentForeground = Color.Parse("#FFFFFFFF");
        }

        Color hover = Lighten(accent, 0.1);
        Color pressed = Darken(accent, 0.12);
        Color accentLight1 = Lighten(accent, 0.2);
        Color accentLight2 = Lighten(accent, 0.35);
        Color accentLight3 = Lighten(accent, 0.5);
        Color accentDark1 = Darken(accent, 0.18);
        Color accentDark2 = Darken(accent, 0.3);
        Color accentDark3 = Darken(accent, 0.42);
        Color selection = Color.FromArgb(110, accent.R, accent.G, accent.B);
        Resources["SkuaAccentColor"] = accent;
        Resources["SkuaAccentForegroundColor"] = accentForeground;
        Resources["SkuaAccentBrush"] = new SolidColorBrush(accent);
        Resources["SkuaAccentForegroundBrush"] = new SolidColorBrush(accentForeground);
        Resources["SkuaAccentHoverBrush"] = new SolidColorBrush(hover);
        Resources["SkuaAccentPressedBrush"] = new SolidColorBrush(pressed);
        Resources["SkuaSelectionBrush"] = new SolidColorBrush(selection);
        Resources["SystemAccentColor"] = accent;
        Resources["SystemAccentColorLight1"] = accentLight1;
        Resources["SystemAccentColorLight2"] = accentLight2;
        Resources["SystemAccentColorLight3"] = accentLight3;
        Resources["SystemAccentColorDark1"] = accentDark1;
        Resources["SystemAccentColorDark2"] = accentDark2;
        Resources["SystemAccentColorDark3"] = accentDark3;
        Resources["SystemAccentColorBrush"] = new SolidColorBrush(accent);
        Resources["SystemAccentColorLight1Brush"] = new SolidColorBrush(accentLight1);
        Resources["SystemAccentColorLight2Brush"] = new SolidColorBrush(accentLight2);
        Resources["SystemAccentColorLight3Brush"] = new SolidColorBrush(accentLight3);
        Resources["SystemAccentColorDark1Brush"] = new SolidColorBrush(accentDark1);
        Resources["SystemAccentColorDark2Brush"] = new SolidColorBrush(accentDark2);
        Resources["SystemAccentColorDark3Brush"] = new SolidColorBrush(accentDark3);
        ApplyFluentAccent(accent);
    }

    private Color GetResourceColor(string key, string fallbackHex)
    {
        if (Resources.TryGetResource(key, ThemeVariant.Default, out object? value))
        {
            if (value is Color c)
                return c;
            if (value is ISolidColorBrush b)
                return b.Color;
        }
        return Color.Parse(fallbackHex);
    }

    private void ApplyFluentAccent(Color accent)
    {
        foreach (var style in Styles)
        {
            if (style is not FluentTheme fluentTheme)
                continue;

            if (fluentTheme.Palettes.TryGetValue(ThemeVariant.Light, out ColorPaletteResources? light))
                light.Accent = accent;

            if (fluentTheme.Palettes.TryGetValue(ThemeVariant.Dark, out ColorPaletteResources? dark))
                dark.Accent = accent;
        }
    }

    private static Color Lighten(Color color, double amount)
    {
        byte L(byte c) => (byte)Math.Clamp(c + (255 - c) * amount, 0, 255);
        return Color.FromArgb(color.A, L(color.R), L(color.G), L(color.B));
    }

    private static Color Darken(Color color, double amount)
    {
        byte D(byte c) => (byte)Math.Clamp(c * (1 - amount), 0, 255);
        return Color.FromArgb(color.A, D(color.R), D(color.G), D(color.B));
    }

    private void TrayShowManager_Click(object? sender, EventArgs e)
    {
        StrongReferenceMessenger.Default.Send<ShowMainWindowMessage>();
    }

    private void TrayIcon_Clicked(object? sender, EventArgs e)
    {
        StrongReferenceMessenger.Default.Send<ShowMainWindowMessage>();
    }

    private void TrayLaunchClient_Click(object? sender, EventArgs e)
    {
        LauncherViewModel launcher = Ioc.Default.GetRequiredService<LauncherViewModel>();
        _ = launcher.LaunchSkua();
    }

    private void TrayUpdateScripts_Click(object? sender, EventArgs e)
    {
        StrongReferenceMessenger.Default.Send<UpdateScriptsMessage>(new(false));
    }

    private void TrayResetScripts_Click(object? sender, EventArgs e)
    {
        StrongReferenceMessenger.Default.Send<UpdateScriptsMessage>(new(true));
    }

    private void TrayCheckUpdate_Click(object? sender, EventArgs e)
    {
        StrongReferenceMessenger.Default.Send<CheckClientUpdateMessage>();
    }

    private void TrayExit_Click(object? sender, EventArgs e)
    {
        LauncherViewModel launcher = Ioc.Default.GetRequiredService<LauncherViewModel>();
        launcher.KillAllSkuaProcesses();
        StrongReferenceMessenger.Default.Send<ExitManagerMessage>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }
}

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Skua.Core.AppStartup;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Services;
using Skua.Manager.Avalonia.AppStartup;
using Skua.Manager.Avalonia.Services;
using Skua.Manager.Avalonia.ViewModels;
using Skua.Shared.Avalonia.Services;
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
        services.AddSingleton<IThemeService, Skua.Shared.Avalonia.Services.AvaloniaThemeService>();
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

    // Full theme changes can include base theme + multiple palette slots, so we always
    // republish the complete resource map here (see ThemeResourceApplicator).
    private void OnThemeChanged(object? theme)
    {
        IThemeService themeService = Ioc.Default.GetRequiredService<IThemeService>();
        RequestedThemeVariant = themeService.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        if (themeService is AvaloniaThemeService ats)
        {
            ThemeResourceApplicator.ApplyAccentBrushes(
                this,
                ats.PrimaryHex,
                ats.ForegroundHex,
                ats.SecondaryHex,
                ats.SecondaryForegroundHex,
                isDarkTheme: themeService.IsDarkTheme);
        }
        else
        {
            string? accentHex = theme?.ToString();
            ThemeResourceApplicator.ApplyAccentBrushes(this, accentHex, isDarkTheme: themeService.IsDarkTheme);
        }
    }

    // Single-scheme edits still republish all known colors when AvaloniaThemeService is active.
    // This keeps Material and Skua resources in lockstep and prevents partial updates.
    private void OnSchemeChanged(Core.Models.ColorScheme scheme, object? color)
    {
        bool isDark = RequestedThemeVariant == ThemeVariant.Dark;
        IThemeService themeService = Ioc.Default.GetRequiredService<IThemeService>();
        if (themeService is AvaloniaThemeService ats)
        {
            ThemeResourceApplicator.ApplyAccentBrushes(
                this,
                ats.PrimaryHex,
                ats.ForegroundHex,
                ats.SecondaryHex,
                ats.SecondaryForegroundHex,
                isDarkTheme: isDark);
            return;
        }

        // Non-AvaloniaThemeService fallback can only target one path at a time.
        if (scheme == Core.Models.ColorScheme.PrimaryForeground)
            ThemeResourceApplicator.ApplyAccentBrushes(this, accentForegroundHex: color?.ToString(), isDarkTheme: isDark);
        else
            ThemeResourceApplicator.ApplyAccentBrushes(this, accentHex: color?.ToString(), isDarkTheme: isDark);
    }

    // Apply theme resources before first window render so root styles and previews do not
    // flash default colors on startup.
    private void ApplyThemeFromService(IThemeService themeService)
    {
        bool isDark = themeService.IsDarkTheme;
        RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        if (themeService is AvaloniaThemeService ats)
        {
            ThemeResourceApplicator.ApplyAccentBrushes(
                this,
                ats.PrimaryHex,
                ats.ForegroundHex,
                ats.SecondaryHex,
                ats.SecondaryForegroundHex,
                isDarkTheme: isDark);
            return;
        }

        string? accentHex = themeService.SelectedColor?.ToString();
        ThemeResourceApplicator.ApplyAccentBrushes(this, accentHex, isDarkTheme: isDark);
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

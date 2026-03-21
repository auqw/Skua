using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Skua.App.Avalonia.Flash;
using Skua.App.Avalonia.AppStartup;
using Skua.App.Avalonia.Services;
using Skua.Shared.Avalonia.Services;
using Skua.Core.AppStartup;
using Skua.Core.Interfaces;
using Skua.Core.Utils;
using System;
using System.Threading;
using System.Threading.Tasks;

#if IS_WINDOWS
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using WinForms = System.Windows.Forms;
#endif

namespace Skua.App.Avalonia;

public partial class App : Application
{
    private static int _exceptionDialogShown;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        RegisterGlobalExceptionHandlers();

        ServiceCollection services = new();
        services.AddCommonServices();
        services.AddCompiler();
        services.AddScriptableObjects();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDispatcherService, DispatcherService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IWindowService, WindowService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<IThemeService, AvaloniaThemeService>();
        services.AddSingleton(HotKeys.CreateHotKeys);
        services.AddSingleton<IHotKeyService, HotKeyService>();
        services.AddSingleton<ISoundService, SoundService>();
        services.AddSingleton<IFlashUtil, FlashUtil>();
        services.AddSingleton<SkuaStartupHandler>();
        services.AddSkuaMainAppViewModels();

        Ioc.Default.ConfigureServices(services.BuildServiceProvider());
        ISettingsService settings = Ioc.Default.GetRequiredService<ISettingsService>();
        settings.SetApplicationVersion();
        FlashTrustManager.EnsureTrustFile();
        IClientFilesService clientFiles = Ioc.Default.GetRequiredService<IClientFilesService>();
        clientFiles.CreateDirectories();
        clientFiles.CreateFiles();
        IThemeService themeService = Ioc.Default.GetRequiredService<IThemeService>();
        themeService.ThemeChanged += OnThemeChanged;
        themeService.SchemeChanged += OnSchemeChanged;
        ApplyThemeFromService(themeService);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow();
            desktop.Exit += OnDesktopExit;
        }

        TryInitializeStartupServices();
        base.OnFrameworkInitializationCompleted();
    }

    private static void RegisterGlobalExceptionHandlers()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            Exception ex = e.ExceptionObject as Exception ?? new Exception("Unknown unhandled exception.");
            ShowUnhandledException("AppDomain.CurrentDomain.UnhandledException", ex);
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            ShowUnhandledException("TaskScheduler.UnobservedTaskException", e.Exception);
            e.SetObserved();
        };

    }

    private static void ShowUnhandledException(string source, Exception ex)
    {
        if (Interlocked.Exchange(ref _exceptionDialogShown, 1) == 1)
            return;

        try
        {
            string details = $"Unhandled exception source: {source}\r\n\r\n{ex.GetType().FullName}: {ex.Message}\r\n\r\n{ex.StackTrace}";

            #if IS_WINDOWS
            WinForms.MessageBox.Show(
                details,
                "Skua Client Exception",
                WinForms.MessageBoxButtons.OK,
                WinForms.MessageBoxIcon.Error);
            #endif
        }
        catch
        {
            // ignored
        }
        finally
        {
            Interlocked.Exchange(ref _exceptionDialogShown, 0);
        }
    }

    private static void TryInitializeStartupServices()
    {
        try
        {
            Ioc.Default.GetRequiredService<IPluginManager>().Initialize();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Startup plugin init failed: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            Ioc.Default.GetRequiredService<IHotKeyService>().Reload();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Startup hotkey init failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static async void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        try
        {
            Ioc.Default.GetRequiredService<ICaptureProxy>().Stop();
        }
        catch (Exception ex) { Console.Error.WriteLine($"Exit capture shutdown failed: {ex.GetType().Name}: {ex.Message}"); }

        try
        {
            await ((IAsyncDisposable)Ioc.Default.GetRequiredService<IScriptBoost>()).DisposeAsync();
            await ((IAsyncDisposable)Ioc.Default.GetRequiredService<IScriptBotStats>()).DisposeAsync();
            await ((IAsyncDisposable)Ioc.Default.GetRequiredService<IScriptDrop>()).DisposeAsync();
            await Ioc.Default.GetRequiredService<IScriptManager>().StopScript();
            await ((IScriptInterfaceManager)Ioc.Default.GetRequiredService<IScriptInterface>()).StopTimerAsync();
        }
        catch (Exception ex) { Console.Error.WriteLine($"Exit script cleanup failed: {ex.GetType().Name}: {ex.Message}"); }

        try
        {
            Ioc.Default.GetRequiredService<IFlashUtil>().Dispose();
        }
        catch (Exception ex) { Console.Error.WriteLine($"Exit flash cleanup failed: {ex.GetType().Name}: {ex.Message}"); }

        try
        {
            if (Ioc.Default.GetRequiredService<IHotKeyService>() is IDisposable disposableHotKeys)
                disposableHotKeys.Dispose();
        }
        catch (Exception ex) { Console.Error.WriteLine($"Exit hotkey cleanup failed: {ex.GetType().Name}: {ex.Message}"); }

        WeakReferenceMessenger.Default.Cleanup();
        WeakReferenceMessenger.Default.Reset();
        StrongReferenceMessenger.Default.Reset();
    }

    private void OnThemeChanged(object? theme)
    {
        IThemeService themeService = Ioc.Default.GetRequiredService<IThemeService>();
        ISettingsService settings = Ioc.Default.GetRequiredService<ISettingsService>();
        RequestedThemeVariant = themeService.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        string? accentFromEvent = theme?.ToString();
        ThemeResourceApplicator.ApplyAccentBrushes(this,
            string.IsNullOrWhiteSpace(accentFromEvent) ? settings.Get("ManagerAccentColor", "#7D9AA9") : accentFromEvent,
            settings.Get("ManagerAccentForegroundColor", "#FFFFFFFF"),
            isDarkTheme: themeService.IsDarkTheme);
    }

    private void OnSchemeChanged(Core.Models.ColorScheme scheme, object? color)
    {
        bool isDark = RequestedThemeVariant == ThemeVariant.Dark;
        if (scheme == Core.Models.ColorScheme.PrimaryForeground)
            ThemeResourceApplicator.ApplyAccentBrushes(this, accentForegroundHex: color?.ToString(), isDarkTheme: isDark);
        else
            ThemeResourceApplicator.ApplyAccentBrushes(this, accentHex: color?.ToString(), isDarkTheme: isDark);
    }

    private void ApplyThemeFromService(IThemeService themeService)
    {
        ISettingsService settings = Ioc.Default.GetRequiredService<ISettingsService>();
        bool isDark = themeService.IsDarkTheme;
        RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        ThemeResourceApplicator.ApplyAccentBrushes(this,
            settings.Get("ManagerAccentColor", "#7D9AA9"),
            settings.Get("ManagerAccentForegroundColor", "#FFFFFFFF"),
            isDarkTheme: isDark);
    }

    private void TrayIcon_Clicked(object? sender, EventArgs e)
    {
        ShowClientWindow();
    }

    private void TrayShowClient_Click(object? sender, EventArgs e)
    {
        ShowClientWindow();
    }

    private void TrayHideClient_Click(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null)
            return;
        desktop.MainWindow.Hide();
    }

    private void TrayExit_Click(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    private void ShowClientWindow()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop || desktop.MainWindow is null)
            return;

        if (!desktop.MainWindow.IsVisible)
            desktop.MainWindow.Show();
        desktop.MainWindow.Activate();
    }
}

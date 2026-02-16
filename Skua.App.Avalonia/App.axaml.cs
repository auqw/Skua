using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Skua.App.Avalonia.Flash;
using Skua.App.Avalonia.Services;
using Skua.Core.AppStartup;
using Skua.Core.Interfaces;
using Skua.Core.Utils;
using Skua.Core.ViewModels;
using System;

namespace Skua.App.Avalonia;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
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
        ApplyThemeFromManagerSettings(settings);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Views.MainWindow();
            desktop.Exit += OnDesktopExit;
        }

        TryInitializeStartupServices();
        base.OnFrameworkInitializationCompleted();
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

    private void ApplyThemeFromManagerSettings(ISettingsService settings)
    {
        bool isDark = settings.Get("ManagerIsDarkTheme", true);
        RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
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
            accent = Color.Parse(string.IsNullOrWhiteSpace(accentHex) ? "#C9479A" : accentHex);
        }
        catch
        {
            accent = Color.Parse("#C9479A");
        }

        try
        {
            accentForeground = Color.Parse(string.IsNullOrWhiteSpace(accentForegroundHex) ? "#FFFFFFFF" : accentForegroundHex);
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
}

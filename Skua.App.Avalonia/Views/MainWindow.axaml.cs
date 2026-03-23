using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Skua.App.Avalonia.Services;
using Skua.App.Avalonia.ViewModels;
using Skua.App.Avalonia.ViewModels.MainMenu;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.InteropServices;

#if IS_WINDOWS
using Avalonia.Input;
using AxShockwaveFlashObjects;
using System.Runtime.Versioning;
#endif

namespace Skua.App.Avalonia.Views;

public partial class MainWindow : Window
{
    private static readonly TimeSpan MetricRefreshInterval = TimeSpan.FromSeconds(1);

    private readonly IFlashUtil _flash;
    private readonly IScriptOption _options;
    private readonly SkuaStartupHandler _startup;
    private readonly MainMenuViewModel _mainMenuVm;
    private readonly MainViewModel _mainViewModel;
    private MenuItem? _pluginsMenuItem;
    private readonly DispatcherTimer _metricsTimer;
    private bool _isFlashLoaded;
    private bool _isMetricsCollapsed;

    public MainWindow()
    {
        InitializeComponent();
        _mainViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        _mainMenuVm = Ioc.Default.GetRequiredService<MainMenuViewModel>();
        _flash = Ioc.Default.GetRequiredService<IFlashUtil>();
        _options = Ioc.Default.GetRequiredService<IScriptOption>();
        _startup = Ioc.Default.GetRequiredService<SkuaStartupHandler>();
        _metricsTimer = new DispatcherTimer(MetricRefreshInterval, DispatcherPriority.Background, (_, _) => UpdateMetrics());
        
        DataContext = _mainViewModel;
        MainMenuBar.DataContext = _mainMenuVm;
        MetricsStrip.CollapseRequested += MetricsStrip_CollapseRequested;
        StrongReferenceMessenger.Default.Register<MainWindow, TogglePerformanceStripMessage>(this, static (r, _) => r.ToggleMetricsCollapsed());
        SetMetricsCollapsed(true);
        ConfigureMainMenu();
        RegisterLifecycleHandlers();
        
        #if IS_WINDOWS
        RegisterMessages();
        #endif
    }

    private void ConfigureMainMenu()
    {
        BuildMainMenu();
        _mainMenuVm.MainMenuItems.CollectionChanged += MainMenuItemsChanged;
        _mainMenuVm.Plugins.CollectionChanged += PluginsChanged;
    }

    #if IS_WINDOWS
    private void RegisterMessages()
    {
        WeakReferenceMessenger.Default.Register<MainWindow, FlashChangedMessage<AxShockwaveFlash>>(this, static (r, m) => r.OnFlashControlChanged(m.Flash));
    }
    #endif

    private void RegisterLifecycleHandlers()
    {
        Opened += MainWindow_Opened;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        _startup.Execute();
        _flash.FlashCall += OnFlashCall;
        
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) _flash.InitializeFlash();
        
        if (!_isMetricsCollapsed)
        {
            UpdateMetrics();
            _metricsTimer.Start();
        }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _flash.FlashCall -= OnFlashCall;
        _metricsTimer.Stop();
        MetricsStrip.CollapseRequested -= MetricsStrip_CollapseRequested;
        _startup.Dispose();
        _mainMenuVm.MainMenuItems.CollectionChanged -= MainMenuItemsChanged;
        _mainMenuVm.Plugins.CollectionChanged -= PluginsChanged;
        StrongReferenceMessenger.Default.UnregisterAll(this);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }
    
    #if IS_WINDOWS
    private void OnFlashControlChanged(AxShockwaveFlash flash)
    {
        FlashHost.SetChild(flash);
    }
    #endif

    private void OnFlashCall(string function, params object[] args)
    {
        if (function == "loaded" && !_isFlashLoaded)
        {
            _isFlashLoaded = true;
            Dispatcher.UIThread.Post(() =>
            {
                LoadingBar.IsVisible = false;
                FlashHost.IsVisible = true;
            });
        }
    }

    private void MainMenuItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        BuildMainMenu();
    }

    private void PluginsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        BuildPluginsMenuItems();
    }

    private void BuildMainMenu()
    {
        MainMenuControl.Items.Clear();
        foreach (MainMenuItemViewModel item in _mainMenuVm.MainMenuItems)
            MainMenuControl.Items.Add(CreateMainMenuItem(item));

        _pluginsMenuItem = new MenuItem { Header = "Plugins" };
        _pluginsMenuItem.Classes.Add("top-nav-item");
        MainMenuControl.Items.Add(_pluginsMenuItem);
        BuildPluginsMenuItems();
    }

    private void BuildPluginsMenuItems()
    {
        if (_pluginsMenuItem is null)
            return;

        _pluginsMenuItem.Items.Clear();
        foreach (MainMenuItemViewModel plugin in _mainMenuVm.Plugins)
            _pluginsMenuItem.Items.Add(CreateSubMenuItem(plugin));
    }

    private MenuItem CreateMainMenuItem(MainMenuItemViewModel item)
    {
        MenuItem menuItem = new()
        {
            Header = item.Header,
            Command = item.Command
        };
        menuItem.Classes.Add("top-nav-item");

        if (item.SubItems is { Count: > 0 })
        {
            foreach (MainMenuItemViewModel child in item.SubItems)
                menuItem.Items.Add(CreateSubMenuItem(child));
        }

        return menuItem;
    }

    private static MenuItem CreateSubMenuItem(MainMenuItemViewModel item)
    {
        MenuItem menuItem = new()
        {
            Header = item.Header,
            Command = item.Command
        };

        if (item.SubItems is { Count: > 0 })
        {
            foreach (MainMenuItemViewModel child in item.SubItems)
                menuItem.Items.Add(CreateSubMenuItem(child));
        }

        return menuItem;
    }

    private void UpdateMetrics()
    {
        Process proc = Process.GetCurrentProcess();
        double workingSetMb = proc.WorkingSet64 / (1024d * 1024d);
        double privateMb = proc.PrivateMemorySize64 / (1024d * 1024d);
        double managedMb = GC.GetTotalMemory(false) / (1024d * 1024d);
        int fps = _options.SetFPS;

        MetricsStrip.FpsText = $"FPS: {fps}";
        MetricsStrip.WorkingSetText = $"RAM: {workingSetMb:0.0} MB";
        MetricsStrip.PrivateText = $"Private: {privateMb:0.0} MB";
        MetricsStrip.ManagedText = $"Managed: {managedMb:0.0} MB";
    }

    private void MetricsStrip_CollapseRequested(object? sender, EventArgs e)
    {
        SetMetricsCollapsed(true);
    }

    private void ToggleMetricsCollapsed()
    {
        SetMetricsCollapsed(!_isMetricsCollapsed);
    }

    private void SetMetricsCollapsed(bool collapsed)
    {
        if (_isMetricsCollapsed == collapsed)
            return;

        _isMetricsCollapsed = collapsed;
        MetricsStrip.IsVisible = !collapsed;
        if (collapsed)
        {
            _metricsTimer.Stop();
            return;
        }

        UpdateMetrics();
        if (!_metricsTimer.IsEnabled)
            _metricsTimer.Start();
    }
}

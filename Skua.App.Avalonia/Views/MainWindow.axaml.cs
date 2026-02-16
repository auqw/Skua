using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using AxShockwaveFlashObjects;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Skua.App.Avalonia.Services;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.ViewModels;
using System.Collections.Specialized;
using System;
using System.Diagnostics;
using System.Linq;

namespace Skua.App.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly IFlashUtil _flash;
    private readonly IScriptOption _options;
    private readonly SkuaStartupHandler _startup;
    private readonly MainMenuViewModel _mainMenuVm;
    private readonly MainViewModel _mainViewModel;
    private MenuItem[] _hoverMenus = [];
    private MenuItem? _pluginsMenuItem;
    private readonly DispatcherTimer _metricsTimer;
    private bool _flashLoaded;

    public MainWindow()
    {
        InitializeComponent();
        _mainViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        _mainMenuVm = Ioc.Default.GetRequiredService<MainMenuViewModel>();
        DataContext = _mainViewModel;
        _flash = Ioc.Default.GetRequiredService<IFlashUtil>();
        _options = Ioc.Default.GetRequiredService<IScriptOption>();
        _startup = Ioc.Default.GetRequiredService<SkuaStartupHandler>();
        TopActionsPanel.DataContext = _mainMenuVm;
        AutoPanel.DataContext = _mainMenuVm.AutoViewModel;
        JumpPanel.DataContext = _mainMenuVm.JumpViewModel;

        BuildMainMenu();
        _mainMenuVm.MainMenuItems.CollectionChanged += MainMenuItemsChanged;
        _mainMenuVm.Plugins.CollectionChanged += PluginsChanged;
        _metricsTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, (_, _) => UpdateMetrics());

        WeakReferenceMessenger.Default.Register<MainWindow, FlashChangedMessage<AxShockwaveFlash>>(this, static (r, m) => r.OnFlashControlChanged(m.Flash));

        Opened += MainWindow_Opened;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        _startup.Execute();
        _flash.FlashCall += OnFlashCall;
        _flash.InitializeFlash();
        UpdateMetrics();
        _metricsTimer.Start();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _flash.FlashCall -= OnFlashCall;
        _metricsTimer.Stop();
        _startup.Dispose();
        _mainMenuVm.MainMenuItems.CollectionChanged -= MainMenuItemsChanged;
        _mainMenuVm.Plugins.CollectionChanged -= PluginsChanged;
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private void OnFlashControlChanged(AxShockwaveFlash flash)
    {
        FlashHost.SetChild(flash);
    }

    private void OnFlashCall(string function, params object[] args)
    {
        switch (function)
        {
            case "loaded" when !_flashLoaded:
                _flashLoaded = true;
                Dispatcher.UIThread.Post(() =>
                {
                    LoadingBar.IsVisible = false;
                    FlashHost.IsVisible = true;
                });
                break;
        }
    }

    private void TopMenuItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not MenuItem hovered || !hovered.HasSubMenu)
            return;

        foreach (MenuItem item in _hoverMenus)
            item.IsSubMenuOpen = ReferenceEquals(item, hovered);
    }

    private void NonMenuArea_PointerEntered(object? sender, PointerEventArgs e)
    {
        foreach (MenuItem item in _hoverMenus)
            item.IsSubMenuOpen = false;
    }

    private void MainMenuItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        BuildMainMenu();
    }

    private void PluginsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        BuildPluginsMenuItems();
        RefreshHoverMenus();
    }

    private void BuildMainMenu()
    {
        MainMenuControl.Items.Clear();
        foreach (MainMenuItemViewModel item in _mainMenuVm.MainMenuItems)
            MainMenuControl.Items.Add(CreateMainMenuItem(item));

        _pluginsMenuItem = new MenuItem { Header = "Plugins" };
        _pluginsMenuItem.Classes.Add("top-nav-item");
        _pluginsMenuItem.PointerEntered += TopMenuItem_PointerEntered;
        MainMenuControl.Items.Add(_pluginsMenuItem);
        BuildPluginsMenuItems();
        RefreshHoverMenus();
    }

    private void BuildPluginsMenuItems()
    {
        if (_pluginsMenuItem is null)
            return;

        _pluginsMenuItem.Items.Clear();
        foreach (MainMenuItemViewModel plugin in _mainMenuVm.Plugins)
            _pluginsMenuItem.Items.Add(CreateSubMenuItem(plugin));
    }

    private void RefreshHoverMenus()
    {
        _hoverMenus = MainMenuControl.Items
            .OfType<MenuItem>()
            .Where(x => x.HasSubMenu)
            .ToArray();
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

            menuItem.PointerEntered += TopMenuItem_PointerEntered;
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

        FpsMetricText.Text = $"FPS: {fps}";
        WorkingSetMetricText.Text = $"RAM: {workingSetMb:0.0} MB";
        PrivateMetricText.Text = $"Private: {privateMb:0.0} MB";
        ManagedMetricText.Text = $"Managed: {managedMb:0.0} MB";
    }
}

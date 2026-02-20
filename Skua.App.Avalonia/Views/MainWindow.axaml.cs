using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
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
using System.Linq;

namespace Skua.App.Avalonia.Views;

public partial class MainWindow : Window
{
    private static readonly TimeSpan MetricRefreshInterval = TimeSpan.FromSeconds(1);

    //private readonly IFlashUtil _flash;
    private readonly IScriptOption _options;
    private readonly SkuaStartupHandler _startup;
    private readonly MainMenuViewModel _mainMenuVm;
    private readonly MainViewModel _mainViewModel;
    private MenuItem? _pluginsMenuItem;
    private readonly DispatcherTimer _metricsTimer;
    private MenuItem[] _hoverMenus = [];
    private bool _isFlashLoaded;

    public MainWindow()
    {
        InitializeComponent();
        _mainViewModel = Ioc.Default.GetRequiredService<MainViewModel>();
        _mainMenuVm = Ioc.Default.GetRequiredService<MainMenuViewModel>();
        //_flash = Ioc.Default.GetRequiredService<IFlashUtil>();
        _options = Ioc.Default.GetRequiredService<IScriptOption>();
        _startup = Ioc.Default.GetRequiredService<SkuaStartupHandler>();
        _metricsTimer = new DispatcherTimer(MetricRefreshInterval, DispatcherPriority.Background, (_, _) => UpdateMetrics());
        
        DataContext = _mainViewModel;
        ConfigureTopActions();
        ConfigureMainMenu();
        RegisterMessages();
        RegisterLifecycleHandlers();
        
        TitleBar.PointerPressed += (_, e) => BeginMoveDrag(e);
        MinimizeButton.Click += (_, _) => WindowState = WindowState.Minimized;
        MaximizeButton.Click += (_, _) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        CloseButton.Click += (_, _) => Close();
    }

    private void ConfigureTopActions()
    {
        BotWindowButton.Command = _mainMenuVm.ShowBotWindowCommand;
        ConfigureAutoActions(_mainMenuVm.AutoViewModel);
        ConfigureJumpActions(_mainMenuVm.JumpViewModel);
    }

    private void ConfigureAutoActions(AutoViewModel viewModel)
    {
        AutoAttackButton.Command = viewModel.StartAutoAttackCommand;
        AutoHuntButton.Command = viewModel.StartAutoHuntCommand;
        AutoStopButton.Command = viewModel.StopAutoAsyncCommand;
        BindTextBlock(AutoClassText, nameof(AutoViewModel.SelectedClassString), viewModel, "Class: {0}");
    }

    private void ConfigureJumpActions(JumpViewModel viewModel)
    {
        JumpCurrentButton.Command = viewModel.GetCurrentCommand;
        JumpRefreshButton.Command = viewModel.UpdateCellsCommand;
        JumpGoButton.Command = viewModel.JumpToCommand;
        BindTextBoxTwoWay(JumpCellTextBox, nameof(JumpViewModel.SelectedCell), viewModel);
        BindTextBoxTwoWay(JumpPadTextBox, nameof(JumpViewModel.SelectedPad), viewModel);
    }

    private static void BindTextBlock(TextBlock textBlock, string path, object source, string? stringFormat = null)
    {
        textBlock[!TextBlock.TextProperty] = new Binding(path)
        {
            Source = source,
            Mode = BindingMode.OneWay,
            StringFormat = stringFormat
        };
    }

    private static void BindTextBoxTwoWay(TextBox textBox, string path, object source)
    {
        textBox[!TextBox.TextProperty] = new Binding(path)
        {
            Source = source,
            Mode = BindingMode.TwoWay
        };
    }

    private void ConfigureMainMenu()
    {
        BuildMainMenu();
        _mainMenuVm.MainMenuItems.CollectionChanged += MainMenuItemsChanged;
        _mainMenuVm.Plugins.CollectionChanged += PluginsChanged;
    }

    private void RegisterMessages()
    {
        //WeakReferenceMessenger.Default.Register<MainWindow, FlashChangedMessage<AxShockwaveFlash>>(this, static (r, m) => r.OnFlashControlChanged(m.Flash));
    }

    private void RegisterLifecycleHandlers()
    {
        Opened += MainWindow_Opened;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Opened(object? sender, EventArgs e)
    {
        _startup.Execute();
        //_flash.FlashCall += OnFlashCall;
        //_flash.InitializeFlash();
        UpdateMetrics();
        _metricsTimer.Start();
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        //_flash.FlashCall -= OnFlashCall;
        _metricsTimer.Stop();
        _startup.Dispose();
        _mainMenuVm.MainMenuItems.CollectionChanged -= MainMenuItemsChanged;
        _mainMenuVm.Plugins.CollectionChanged -= PluginsChanged;
        //WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    /*private void OnFlashControlChanged(AxShockwaveFlash flash)
    {
        FlashHost.SetChild(flash);
    }*/

    /*private void OnFlashCall(string function, params object[] args)
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
    }*/

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

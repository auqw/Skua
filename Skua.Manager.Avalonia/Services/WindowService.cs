using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using Skua.Core.Interfaces;
using System;
using System.Collections.Generic;

namespace Skua.Manager.Avalonia.Services;

public class WindowService : IWindowService
{
    private readonly Dictionary<string, Window> _managedWindows = new(StringComparer.OrdinalIgnoreCase);

    public void ShowWindow<TViewModel>(int width, int height) where TViewModel : class
    {
        TViewModel vm = Ioc.Default.GetRequiredService<TViewModel>();
        Window window = CreateHostWindow(vm, typeof(TViewModel).Name, width, height);
        window.Show();
    }

    public void ShowWindow<TViewModel>() where TViewModel : class
    {
        TViewModel vm = Ioc.Default.GetRequiredService<TViewModel>();
        Window window = CreateHostWindow(vm, typeof(TViewModel).Name, 900, 600);
        window.Show();
    }

    public void ShowWindow<TViewModel>(TViewModel viewModel) where TViewModel : class
    {
        Window window = CreateHostWindow(viewModel, typeof(TViewModel).Name, 900, 600);
        window.Show();
    }

    public void ShowManagedWindow(string key)
    {
        if (!_managedWindows.TryGetValue(key, out Window? window))
            return;

        if (!window.IsVisible)
            window.Show();
        window.Activate();

        if (window.DataContext is ObservableRecipient recipient)
            recipient.IsActive = true;
    }

    public void RegisterManagedWindow<TViewModel>(string key, TViewModel viewModel) where TViewModel : class, IManagedWindow
    {
        if (_managedWindows.ContainsKey(key))
            return;

        Window window = CreateHostWindow(viewModel, viewModel.Title, viewModel.Width, viewModel.Height);
        window.CanResize = viewModel.CanResize;
        window.Closing += (_, e) =>
        {
            e.Cancel = true;
            window.Hide();
            if (window.DataContext is ObservableRecipient recipient)
                recipient.IsActive = false;
        };
        _managedWindows[key] = window;
    }

    private static Window CreateHostWindow(object viewModel, string title, double width, double height)
    {
        return new Window
        {
            Title = title,
            Width = width,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            DataContext = viewModel,
            Content = new ContentControl
            {
                Content = viewModel
            }
        };
    }
}

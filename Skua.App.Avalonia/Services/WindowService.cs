using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Layout;
using Skua.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skua.App.Avalonia.Services;

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
        window.Closed += (_, _) =>
        {
            _managedWindows.Remove(key);
            if (window.DataContext is IDisposable disposable)
                disposable.Dispose();
            window.DataContext = null;
        };
        _managedWindows[key] = window;
    }

    private static Window CreateHostWindow(object viewModel, string title, double width, double height)
    {
        bool hasTemplate = Application.Current?.DataTemplates.Any(t => t.Match(viewModel)) == true;

        return new Window
        {
            Title = title,
            Width = width,
            Height = height,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            DataContext = viewModel,
            Content = hasTemplate
                ? new ContentControl { Content = viewModel }
                : new Border
                {
                    Padding = new Thickness(16),
                    Child = new StackPanel
                    {
                        Spacing = 8,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Children =
                        {
                            new TextBlock { Text = "View not ported yet." },
                            new TextBlock { Text = viewModel.GetType().Name }
                        }
                    }
                }
        };
    }
}

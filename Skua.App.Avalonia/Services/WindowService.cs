using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Layout;
using Skua.Core.Interfaces;
using Skua.Shared.Avalonia.Controls.Shell;
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
        bool hasTemplate = Application.Current?.DataTemplates.Any(t => t.Match(viewModel)) == true;
        SizeToContent sizeToContent = SizeToContent.Manual;

        if (width <= 0 && height <= 0)
            sizeToContent = SizeToContent.WidthAndHeight;
        else if (width <= 0)
            sizeToContent = SizeToContent.Width;
        else if (height <= 0)
            sizeToContent = SizeToContent.Height;

        Window window = new()
        {
            Title = title,
            SystemDecorations = SystemDecorations.BorderOnly,
            ExtendClientAreaToDecorationsHint = true,
            ExtendClientAreaChromeHints = global::Avalonia.Platform.ExtendClientAreaChromeHints.NoChrome,
            ExtendClientAreaTitleBarHeightHint = 32,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            SizeToContent = sizeToContent,
            DataContext = viewModel,
            Content = CreateShellContent(
                title,
                hasTemplate
                    ? new ContentControl { Content = viewModel }
                    : CreateFallbackContent(title))
        };
        window.Classes.Add("skua");

        if (width > 0)
            window.Width = width;
        if (height > 0)
            window.Height = height;

        return window;
    }

    private static Control CreateShellContent(string title, Control content)
    {
        Border contentHost = new()
        {
            Margin = new Thickness(4, 2, 4, 4),
            Padding = new Thickness(3),
            Child = content
        };
        contentHost.Classes.Add("card");

        Grid root = new();
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        WindowTitleBar titleBar = new()
        {
            TitleText = title
        };
        Grid.SetRow(titleBar, 0);
        Grid.SetRow(contentHost, 1);
        root.Children.Add(titleBar);
        root.Children.Add(contentHost);

        return root;
    }

    private static Control CreateFallbackContent(string title) =>
        new Border
        {
            Padding = new Thickness(16),
            Child = new TextBlock
            {
                Text = title,
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            }
        };
}

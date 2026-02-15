using Avalonia.Controls;
using Avalonia.Input;
using CommunityToolkit.Mvvm.DependencyInjection;
using Skua.Core.ViewModels;
using System.Collections.Generic;

namespace Skua.App.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = Ioc.Default.GetRequiredService<MainViewModel>();
    }

    private void TopMenuItem_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is not MenuItem hovered || !hovered.HasSubMenu)
            return;

        foreach (MenuItem item in TopMenuItems())
            item.IsSubMenuOpen = ReferenceEquals(item, hovered);
    }

    private void NonMenuArea_PointerEntered(object? sender, PointerEventArgs e)
    {
        foreach (MenuItem item in TopMenuItems())
            item.IsSubMenuOpen = false;
    }

    private IEnumerable<MenuItem> TopMenuItems()
    {
        yield return ScriptsMenuItem;
        yield return OptionsMenuItem;
        yield return HelpersMenuItem;
        yield return ToolsMenuItem;
        yield return PluginsMenuItem;
    }
}

using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.DependencyInjection;
using Skua.Manager.Avalonia.ViewModels;

namespace Skua.Manager.Avalonia.UserControls;

public partial class LauncherUserControl : UserControl
{
    private readonly LauncherViewModel _viewModel;

    public LauncherUserControl()
    {
        InitializeComponent();
        _viewModel = Ioc.Default.GetRequiredService<LauncherViewModel>();
        DataContext = _viewModel;
    }

    private void KillAll_Click(object? sender, RoutedEventArgs e)
    {
        _viewModel.KillAllSkuaProcesses();
    }
}

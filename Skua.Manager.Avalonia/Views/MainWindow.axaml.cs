using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Messaging;
using Skua.Core.ViewModels.Manager;

namespace Skua.Manager.Avalonia.Views;

public partial class MainWindow : Window
{
    private readonly ManagerMainViewModel _viewModel;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = Ioc.Default.GetRequiredService<ManagerMainViewModel>();
        DataContext = _viewModel;

        StrongReferenceMessenger.Default.Register<MainWindow, ShowMainWindowMessage>(this, ShowManager);
        StrongReferenceMessenger.Default.Register<MainWindow, ExitManagerMessage>(this, AllowClose);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
            _viewModel.IsActive = false;
            return;
        }

        _viewModel.IsActive = false;
        StrongReferenceMessenger.Default.UnregisterAll(this);
        base.OnClosing(e);
    }

    private void ShowManager(MainWindow recipient, ShowMainWindowMessage message)
    {
        if (recipient.IsVisible)
        {
            recipient.Hide();
            return;
        }

        recipient.Show();
        recipient.Activate();
    }

    private void AllowClose(MainWindow recipient, ExitManagerMessage message)
    {
        recipient._allowClose = true;
    }
}

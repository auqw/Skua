using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace Skua.App.Avalonia.ViewModels.MainMenu;

public partial class MainMenuItemViewModel : ObservableObject, IMainMenuItemViewModel
{
    public MainMenuItemViewModel(string header, IEnumerable<IMainMenuItemViewModel> subItems)
    {
        Header = header;
        SubItems = subItems.ToList();
        Command = new RelayCommand(OpenManagedWindow);
    }

    public MainMenuItemViewModel(string header)
    {
        Header = header;
        Command = new RelayCommand(OpenManagedWindow);
    }

    public MainMenuItemViewModel(string header, IRelayCommand command)
    {
        Header = header;
        Command = command;
    }

    [ObservableProperty]
    private string _header = "Default Title";

    [ObservableProperty]
    private List<IMainMenuItemViewModel>? _subItems = null;

    public IRelayCommand Command { get; }

    private void OpenManagedWindow()
    {
        Ioc.Default.GetRequiredService<IWindowService>().ShowManagedWindow(Header);
    }
}
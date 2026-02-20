using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace Skua.Core.Interfaces.ViewModels;

public interface IMainMenuItemViewModel : INotifyPropertyChanged
{
    string Header { get; }
    List<IMainMenuItemViewModel> SubItems { get; }
    IRelayCommand Command { get; }
}
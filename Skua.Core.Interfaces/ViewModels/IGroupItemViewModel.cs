using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Skua.Core.Interfaces.ViewModels;

public interface IGroupItemViewModel : INotifyPropertyChanged
{
    string Name { get; set; }
    bool IsExpanded { get; set; }
    ObservableCollection<IAccountItemViewModel> Accounts { get; }
    IRelayCommand RemoveCommand { get; }
    IRelayCommand StartCommand { get; }
    IRelayCommand StartWithScriptCommand { get; }
    IRelayCommand RenameCommand { get; }
}
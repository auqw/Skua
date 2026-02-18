using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Skua.Core.Interfaces.ViewModels;

public interface IAccountItemViewModel : INotifyPropertyChanged
{
    string DisplayName { get; set; }
    string Username { get; set; }
    string Password { get; set; }
    int AccountNumber { get; set; }
    string DisplayOrUsername { get; }
    ObservableCollection<string> Tags { get; }
    bool IsExpanded { get; set; }
    bool UseCheck { get; set; }

    IRelayCommand RemoveCommand { get; }
    IRelayCommand AddTagsCommand { get; }
    IRelayCommand StartCommand { get; }
    IRelayCommand StartWithScriptCommand { get; }
    IRelayCommand ToggleSelectionCommand { get; }
    IRelayCommand AddToGroupCommand { get; }

    void RefreshDisplayName();
}
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Skua.Manager.Avalonia.ViewModels;

public partial class SelectGroupDialogViewModel : ObservableObject
{
    public SelectGroupDialogViewModel(IEnumerable<GroupItemViewModel> groups)
    {
        Groups = new ObservableCollection<GroupItemViewModel>(groups);
    }

    public ObservableCollection<GroupItemViewModel> Groups { get; }

    [ObservableProperty]
    private GroupItemViewModel? _selectedGroup;

    public bool DialogResult { get; private set; }

    [RelayCommand]
    private void Confirm()
    {
        DialogResult = SelectedGroup != null;
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
    }
}

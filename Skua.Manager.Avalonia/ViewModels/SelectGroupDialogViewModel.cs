using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Skua.Manager.Avalonia.ViewModels;

public partial class SelectGroupDialogViewModel : ObservableObject
{
    public SelectGroupDialogViewModel(IEnumerable<IGroupItemViewModel> groups)
    {
        Groups = new ObservableCollection<IGroupItemViewModel>(groups);
    }

    public ObservableCollection<IGroupItemViewModel> Groups { get; }

    [ObservableProperty]
    private IGroupItemViewModel? _selectedGroup;

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

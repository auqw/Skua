using CommunityToolkit.Mvvm.ComponentModel;
using Skua.Core.Interfaces.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Skua.Manager.Avalonia.ViewModels;

public partial class SelectAccountDialogViewModel : ObservableObject
{
    public SelectAccountDialogViewModel(IEnumerable<IAccountItemViewModel> accounts)
    {
        Accounts = new ObservableCollection<IAccountItemViewModel>(accounts);
        FilteredAccounts = new ObservableCollection<IAccountItemViewModel>(Accounts);
    }

    public ObservableCollection<IAccountItemViewModel> Accounts { get; }
    public ObservableCollection<IAccountItemViewModel> FilteredAccounts { get; }

    [ObservableProperty]
    private IAccountItemViewModel? _selectedAccount;

    [ObservableProperty]
    private string _searchText = string.Empty;

    partial void OnSearchTextChanged(string value)
    {
        IEnumerable<IAccountItemViewModel> filtered = string.IsNullOrWhiteSpace(value)
            ? Accounts
            : Accounts.Where(a => a.DisplayOrUsername.Contains(value, System.StringComparison.OrdinalIgnoreCase));

        FilteredAccounts.Clear();
        foreach (IAccountItemViewModel account in filtered)
            FilteredAccounts.Add(account);

        if (SelectedAccount is null || !FilteredAccounts.Contains(SelectedAccount))
            SelectedAccount = FilteredAccounts.FirstOrDefault();
    }
}

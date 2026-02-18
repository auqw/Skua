using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Messaging;
using System.Collections.ObjectModel;

namespace Skua.Manager.Avalonia.ViewModels;

public partial class AccountItemViewModel : ObservableObject, IAccountItemViewModel
{
    public AccountItemViewModel()
    {
        Tags = new ObservableCollection<string>();
    }

    [ObservableProperty]
    private string _displayName;

    [ObservableProperty]
    private string _username;

    [ObservableProperty]
    private string _password;

    [ObservableProperty]
    private int _accountNumber;

    public string DisplayOrUsername
    {
        get
        {
            bool anonymise = Ioc.Default.GetRequiredService<ISettingsService>().Get("AnonymiseAccounts", false);
            if (anonymise)
                return $"Account {AccountNumber}";

            return string.IsNullOrWhiteSpace(DisplayName) ? Username : DisplayName;
        }
    }

    public ObservableCollection<string> Tags { get; }

    [ObservableProperty]
    private bool _isExpanded;

    private bool _useCheck;

    public bool UseCheck
    {
        get => _useCheck;
        set
        {
            if (SetProperty(ref _useCheck, value))
                WeakReferenceMessenger.Default.Send<AccountSelectedMessage>(new(_useCheck));
        }
    }

    [RelayCommand]
    private void Remove()
    {
        WeakReferenceMessenger.Default.Send<RemoveAccountMessage>(new(this));
    }

    [RelayCommand]
    private void AddTags()
    {
        WeakReferenceMessenger.Default.Send<AddTagsMessage>(new(this));
    }

    [RelayCommand]
    private void Start()
    {
        WeakReferenceMessenger.Default.Send<StartAccountMessage>(new(this, false));
    }

    [RelayCommand]
    private void StartWithScript()
    {
        WeakReferenceMessenger.Default.Send<StartAccountMessage>(new(this, true));
    }

    [RelayCommand]
    private void ToggleSelection()
    {
        UseCheck = !UseCheck;
    }

    [RelayCommand]
    private void AddToGroup()
    {
        WeakReferenceMessenger.Default.Send<AddAccountToGroupMessage>(new(this));
    }

    partial void OnDisplayNameChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayOrUsername));
    }

    partial void OnUsernameChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayOrUsername));
    }

    partial void OnAccountNumberChanged(int value)
    {
        OnPropertyChanged(nameof(DisplayOrUsername));
    }

    public void RefreshDisplayName()
    {
        OnPropertyChanged(nameof(DisplayOrUsername));
    }
}

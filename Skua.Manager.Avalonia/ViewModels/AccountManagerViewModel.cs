using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Messaging;
using Skua.Core.Models;
using Skua.Core.Models.Servers;
using Skua.Core.Utils;
using Skua.Shared.Avalonia.ViewModels;
using Skua.Shared.Avalonia.ViewModels.Dialogs;
using Skua.Shared.Avalonia.ViewModels.ScriptRepo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Skua.Manager.Avalonia.ViewModels;

public sealed partial class AccountManagerViewModel : BotControlViewModelBase
{
    // Constructor and Dependencies
    public AccountManagerViewModel(ISettingsService settingsService, IDialogService dialogService, IFileDialogService fileService)
        : base("Accounts")
    {
        IMessengerExtensions.Register<AccountManagerViewModel, RemoveAccountMessage>(Messenger, this, (r, m) => r._RemoveAccount(m.Account));
        IMessengerExtensions.Register<AccountManagerViewModel, AccountSelectedMessage>(Messenger, this, AccountSelected);
        IMessengerExtensions.Register<AccountManagerViewModel, StartAccountMessage>(Messenger, this, (r, m) => r._StartAccount(m.Account, m.WithScript));
        IMessengerExtensions.Register<AccountManagerViewModel, RemoveGroupMessage>(Messenger, this, (r, m) => r._RemoveGroup(m.Group));
        IMessengerExtensions.Register<AccountManagerViewModel, RenameGroupMessage>(Messenger, this, (r, m) => r._RenameGroup(m.Group));
        IMessengerExtensions.Register<AccountManagerViewModel, RemoveAccountFromGroupMessage>(Messenger, this, (r, m) => r._RemoveAccountFromGroup(m.Group, m.Account));
        IMessengerExtensions.Register<AccountManagerViewModel, RefreshAccountDisplayNamesMessage>(Messenger, this, (r, _) => r.RefreshAccountDisplayNames());
        IMessengerExtensions.Register<AccountManagerViewModel, StartGroupMessage>(Messenger, this, (r, m) => r._StartGroup(m.Group, m.WithScript));
        StrongReferenceMessenger.Default.Register<AccountManagerViewModel, LoadScriptMessage, int>(this,
            (int)MessageChannels.ScriptStatus, (r, m) => r.HandleLoadScript(m));
        _settingsService = settingsService;
        _dialogService = dialogService;
        _fileService = fileService;
        ServerList = new();
        Task.Run(async () => await _GetServers());
        Accounts = new();
        _GetSavedAccounts();
        _GetSavedGroups();
        RefreshTagFilters();
        _syncThemes = _settingsService.Get("syncTheme", false);
    }

    private readonly string _exePath = Path.Combine(AppContext.BaseDirectory, "Skua.exe");
    private readonly ISettingsService _settingsService;
    private readonly IDialogService _dialogService;
    private readonly IFileDialogService _fileService;

    // Collections
    public RangedObservableCollection<IAccountItemViewModel> Accounts { get; }
    public RangedObservableCollection<IAccountItemViewModel> FilteredAccounts { get; } = new();
    public RangedObservableCollection<IGroupItemViewModel> Groups { get; } = new();

    // Account Properties
    [ObservableProperty]
    private int _selectedAccountQuant;

    [ObservableProperty]
    private string _usernameInput;

    [ObservableProperty]
    private string _displayNameInput;

    public string PasswordInput { private get; set; }

    // UI State Properties
    [ObservableProperty]
    private bool _showTags;

    [ObservableProperty]
    private bool _showGridView;

    [ObservableProperty]
    private int _columns = 3;

    // Tag Filtering Properties
    [ObservableProperty]
    private bool _isFilterPopupOpen;

    private string _tagFilter = string.Empty;
    public string TagFilter
    {
        get => _tagFilter;
        set
        {
            if (SetProperty(ref _tagFilter, value))
                ApplyTagFilter();
        }
    }

    private string _tagSearchText = string.Empty;
    public string TagSearchText
    {
        get => _tagSearchText;
        set
        {
            if (SetProperty(ref _tagSearchText, value))
                _UpdateFilteredTags();
        }
    }

    public ObservableCollection<TagFilterItem> AllTags { get; } = new();
    public ObservableCollection<TagFilterItem> FilteredTags { get; } = new();
    private HashSet<string> _selectedTagFilters = new();

    // Script and Server Properties
    [ObservableProperty]
    private string _scriptPath = string.Empty;

    [ObservableProperty]
    private bool _startWithScript;

    [ObservableProperty]
    private Server _selectedServer;

    partial void OnSelectedServerChanged(Server value)
    {
        if (value != null)
            _settingsService.Set("LastServer", value.Name);
    }

    private List<Server> _cachedServers = new();

    [ObservableProperty]
    private RangedObservableCollection<Server> _serverList;

    private bool _syncThemes;

    // Account Management
    [RelayCommand]
    public void AddAccount()
    {
        if (string.IsNullOrWhiteSpace(UsernameInput) || string.IsNullOrWhiteSpace(PasswordInput))
        {
            _dialogService.ShowMessageBox("Username or password must not be empty", "Missing Input");
            return;
        }

        string username = UsernameInput.Trim();
        string password = PasswordInput;
        string displayName = string.IsNullOrWhiteSpace(DisplayNameInput) ? username : DisplayNameInput.Trim();

        // Check if account with this username already exists
        IAccountItemViewModel? existingAccount = Accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

        if (existingAccount != null)
        {
            // Update existing account
            existingAccount.Password = password;
            existingAccount.DisplayName = displayName;
        }
        else
        {
            // Create new account
            AccountItemViewModel newAccount = new()
            {
                Username = username,
                Password = password,
                DisplayName = displayName
            };
            Accounts.Add(newAccount);
            ReindexAccounts();
        }

        ApplyTagFilter();

        UsernameInput = string.Empty;
        DisplayNameInput = string.Empty;
        StrongReferenceMessenger.Default.Send<ClearPasswordBoxMessage>();

        _SaveAccounts();
    }

    [RelayCommand]
    public async Task StartAccounts()
    {
        // TODO show dialog to choose between clients
        // TODO manage ids for sync in the future

        _syncThemes = _settingsService.Get("syncTheme", false);
        foreach (IAccountItemViewModel acc in Accounts)
        {
            if (acc.UseCheck)
            {
                _LaunchAcc(acc.Username, acc.Password, acc.DisplayName);
                await Task.Delay(1000);
            }
        }
    }

    [RelayCommand]
    public async Task StartAllAccounts()
    {
        _syncThemes = _settingsService.Get("syncTheme", false);
        foreach (IAccountItemViewModel acc in Accounts)
        {
            _LaunchAcc(acc.Username, acc.Password, acc.DisplayName);
            await Task.Delay(1000);
        }
    }

    [RelayCommand]
    public async Task RemoveAccounts()
    {
        List<IAccountItemViewModel> toRemove = new();
        foreach (IAccountItemViewModel acc in Accounts)
        {
            if (acc.UseCheck)
                toRemove.Add(acc);
        }

        foreach (IAccountItemViewModel acc in toRemove)
            _RemoveAccount(acc);

        _SaveAccounts();
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (IAccountItemViewModel account in Accounts)
        {
            account.UseCheck = false;
        }
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (IAccountItemViewModel account in Accounts)
        {
            account.UseCheck = false;
        }

        foreach (IAccountItemViewModel account in FilteredAccounts)
        {
            account.UseCheck = true;
        }
    }

    private void _RemoveAccount(IAccountItemViewModel account)
    {
        if (account.UseCheck)
            SelectedAccountQuant--;

        // Remove account from all groups
        foreach (IGroupItemViewModel group in Groups.ToList())
        {
            if (group.Accounts.Contains(account))
            {
                group.Accounts.Remove(account);
            }
        }

        Accounts.Remove(account);
        FilteredAccounts.Remove(account);
        ReindexAccounts();

        _SaveAccounts();
        _SaveGroups();
    }

    private void AccountSelected(AccountManagerViewModel recipient, AccountSelectedMessage message)
    {
        if (message.Add)
            recipient.SelectedAccountQuant++;
        else
            recipient.SelectedAccountQuant--;
    }

    private void _StartAccount(IAccountItemViewModel account, bool withScript)
    {
        _syncThemes = _settingsService.Get("syncTheme", false);

        if (withScript && string.IsNullOrEmpty(ScriptPath))
        {
            _dialogService.ShowMessageBox("No script selected. Please select a script first.", "No Script");
            return;
        }

        _LaunchAcc(account.Username, account.Password, account.DisplayName, withScript);
    }

    private async void _StartGroup(IGroupItemViewModel group, bool withScript)
    {
        _syncThemes = _settingsService.Get("syncTheme", false);

        if (withScript && string.IsNullOrEmpty(ScriptPath))
        {
            _dialogService.ShowMessageBox("No script selected. Please select a script first.", "No Script");
            return;
        }

        foreach (IAccountItemViewModel account in group.Accounts)
        {
            _LaunchAcc(account.Username, account.Password, account.DisplayName, withScript);
            await Task.Delay(1000);
        }
    }

    private void _LaunchAcc(string username, string password, string? displayName = null, bool? withScript = null)
    {
        try
        {
            ProcessStartInfo psi = new(_exePath)
            {
                ArgumentList =
                {
                    "-u",
                    username,
                    "-p",
                    password,
                    "-s",
                    SelectedServer?.Name ?? "Twilly"
                },
                WorkingDirectory = AppContext.BaseDirectory
            };

            if (_syncThemes)
            {
                psi.ArgumentList.Add("--use-theme");
                psi.ArgumentList.Add(_settingsService.Get("CurrentTheme", "no-theme"));
            }

            bool shouldUseScript = withScript ?? StartWithScript;
            if (shouldUseScript)
            {
                psi.ArgumentList.Add("--run-script");
                psi.ArgumentList.Add(ScriptPath);
            }

            Process? process = Process.Start(psi);
            if (process != null)
            {
                string accountName = !string.IsNullOrWhiteSpace(displayName) ? displayName : username;
                StrongReferenceMessenger.Default.Send(new AddProcessMessage(process, accountName));
            }
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessageBox($"Error while starting process: {ex.Message}", "Launch Error");
        }
    }

    // Group Management
    public void AddGroup(string groupName)
    {
        if (string.IsNullOrWhiteSpace(groupName))
            return;

        if (Groups.Any(g => g.Name.Equals(groupName, StringComparison.OrdinalIgnoreCase)))
        {
            _dialogService.ShowMessageBox($"A group named '{groupName}' already exists.", "Duplicate Group");
            return;
        }

        GroupItemViewModel newGroup = new(groupName);
        Groups.Add(newGroup);
        _SaveGroups();
    }

    public void AddAccountToGroup(IAccountItemViewModel account, IGroupItemViewModel group)
    {
        if (!group.Accounts.Contains(account))
        {
            group.Accounts.Add(account);
            _SaveGroups();
        }
    }

    private void _RemoveGroup(IGroupItemViewModel group)
    {
        if (Groups.Contains(group))
        {
            Groups.Remove(group);
            _SaveGroups();
        }
    }

    private void _RenameGroup(IGroupItemViewModel group)
    {
        InputDialogViewModel inputDialogViewModel = new(
            "Rename Group", "Enter new group name", "Group Name", numericInputOnly: false)
        {
            DialogTextInput = group.Name
        };

        bool? result = _dialogService.ShowDialog(inputDialogViewModel, "Rename Group");

        if (result == true && !string.IsNullOrWhiteSpace(inputDialogViewModel.DialogTextInput))
        {
            string newName = inputDialogViewModel.DialogTextInput.Trim();

            if (newName.Equals(group.Name, StringComparison.OrdinalIgnoreCase))
                return;

            if (Groups.Any(g => g.Name.Equals(newName, StringComparison.OrdinalIgnoreCase)))
            {
                _dialogService.ShowMessageBox($"A group named '{newName}' already exists.", "Duplicate Group");
                return;
            }

            group.Name = newName;

            _SaveGroups();
        }
    }

    private void _RemoveAccountFromGroup(IGroupItemViewModel group, IAccountItemViewModel account)
    {
        if (group.Accounts.Contains(account))
        {
            group.Accounts.Remove(account);
            _SaveGroups();
        }
    }

    // Tag Filtering
    public void RefreshTagFilters()
    {
        // Remember which tags were selected before refresh
        HashSet<string> previouslySelected = new(_selectedTagFilters, StringComparer.OrdinalIgnoreCase);

        AllTags.Clear();
        Dictionary<string, int> tagCounts = new(StringComparer.OrdinalIgnoreCase);

        foreach (IAccountItemViewModel account in Accounts)
        {
            foreach (string tag in account.Tags)
            {
                if (tagCounts.ContainsKey(tag))
                    tagCounts[tag]++;
                else
                    tagCounts[tag] = 1;
            }
        }

        foreach (KeyValuePair<string, int> kvp in tagCounts.OrderBy(x => x.Key))
        {
            TagFilterItem tagItem = new(kvp.Key, kvp.Value);

            // Restore selection state if this tag was previously selected
            if (previouslySelected.Contains(kvp.Key))
            {
                tagItem.IsSelected = true;
            }

            tagItem.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TagFilterItem.IsSelected))
                {
                    _OnTagFilterSelectionChanged();
                }
            };
            AllTags.Add(tagItem);
        }

        // Remove any selected tags that no longer exist
        _selectedTagFilters.RemoveWhere(tag => !tagCounts.ContainsKey(tag));

        _UpdateFilteredTags();
    }

    [RelayCommand]
    private void ClearTagFilters()
    {
        foreach (TagFilterItem tag in AllTags)
        {
            tag.IsSelected = false;
        }
        TagSearchText = string.Empty;
    }

    public void ApplyTagFilter()
    {
        FilteredAccounts.Clear();

        if (_selectedTagFilters.Count == 0)
        {
            FilteredAccounts.AddRange(Accounts);
            return;
        }

        // Filter uses OR logic: shows accounts that have ANY of the selected tags
        IEnumerable<IAccountItemViewModel> filtered = Accounts.Where(a =>
            a.Tags.Any(t => _selectedTagFilters.Contains(t))
        );

        FilteredAccounts.AddRange(filtered);
    }

    private void _UpdateFilteredTags()
    {
        FilteredTags.Clear();

        IEnumerable<TagFilterItem> filtered = string.IsNullOrWhiteSpace(TagSearchText)
            ? AllTags
            : AllTags.Where(t => t.Name.Contains(TagSearchText, StringComparison.OrdinalIgnoreCase));

        foreach (TagFilterItem tag in filtered)
        {
            FilteredTags.Add(tag);
        }
    }

    private void _OnTagFilterSelectionChanged()
    {
        _selectedTagFilters.Clear();
        foreach (TagFilterItem tag in AllTags.Where(t => t.IsSelected))
        {
            _selectedTagFilters.Add(tag.Name);
        }
        ApplyTagFilter();
    }

    // Persistence
    public void SaveAccounts() => _SaveAccounts();

    private void _SaveAccounts()
    {
        Dictionary<string, AccountData> accs = new(StringComparer.OrdinalIgnoreCase);
        foreach (IAccountItemViewModel account in Accounts)
        {
            accs[account.Username] = new AccountData
            {
                DisplayName = account.DisplayName,
                Password = account.Password,
                Tags = account.Tags.ToList()
            };
        }

        _settingsService.Set("ManagedAccounts", accs);
    }

    private void _GetSavedAccounts()
    {
        Accounts.Clear();
        FilteredAccounts.Clear();
        Dictionary<string, AccountData>? accs = _settingsService.Get<Dictionary<string, AccountData>>("ManagedAccounts");
        if (accs is null)
            return;

        foreach (KeyValuePair<string, AccountData> kvp in accs)
        {
            AccountItemViewModel accountVm = new()
            {
                Username = kvp.Key,
                DisplayName = kvp.Value.DisplayName,
                Password = kvp.Value.Password
            };
            foreach (string tag in kvp.Value.Tags)
                accountVm.Tags.Add(tag);
            Accounts.Add(accountVm);
        }

        ReindexAccounts();

        ApplyTagFilter();
    }

    private void _SaveGroups()
    {
        List<GroupData> groupsData = Groups.Select(g => new GroupData
        {
            Name = g.Name,
            Accounts = g.Accounts.Select<IAccountItemViewModel, string>(a => a.Username).ToList()
        }).ToList();

        _settingsService.Set("AccountGroups", groupsData);
        _SaveAccounts();
    }

    private void _GetSavedGroups()
    {
        try
        {
            List<GroupData>? groupsData = _settingsService.Get<List<GroupData>>("AccountGroups");

            if (groupsData == null || groupsData.Count == 0)
            {
                Groups.Clear();
                return;
            }

            List<GroupItemViewModel> loadedGroups = new();

            foreach (GroupData groupData in groupsData)
            {
                try
                {
                    if (string.IsNullOrEmpty(groupData.Name))
                        continue;

                    GroupItemViewModel group = new(groupData.Name);

                    foreach (string username in groupData.Accounts)
                    {
                        if (!string.IsNullOrEmpty(username))
                        {
                            IAccountItemViewModel? account = Accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
                            if (account != null && !group.Accounts.Contains(account))
                            {
                                group.Accounts.Add(account);
                            }
                        }
                    }

                    loadedGroups.Add(group);
                }
                catch
                {
                    continue;
                }
            }

            Groups.ReplaceRange(loadedGroups);
        }
        catch
        {
            Groups.Clear();
        }
    }

    private async Task _GetServers()
    {
        try
        {
            string response = await ValidatedHttpExtensions.GetStringAsync(HttpClients.GetGHClient()
, $"http://content.aq.com/game/api/data/servers");

            List<Server>? servers = JsonConvert.DeserializeObject<List<Server>>(response);
            if (servers == null || servers.Count == 0)
                return;

            _cachedServers = servers;

            // Measure pings before displaying servers
            await _MeasureServerPings(_cachedServers);

            ServerList.AddRange(_cachedServers);

            string lastServer = _settingsService.Get<string>("LastServer");
            Server? serverToSelect = null;
            if (!string.IsNullOrEmpty(lastServer))
                serverToSelect = Enumerable.FirstOrDefault<Server>(ServerList, s => s.Name == lastServer);
            SelectedServer = serverToSelect ?? ServerList[0];
        }
        catch
        {
        }
    }

    private async Task _MeasureServerPings(List<Server> servers)
    {
        const int TimeoutMs = 2000;
        const long FailedPing = 9999;

        List<Task> pingTasks = servers.Select(server => Task.Run(async () =>
        {
            using (System.Net.NetworkInformation.Ping pinger = new())
            {
                try
                {
                    System.Net.NetworkInformation.PingReply reply = await pinger.SendPingAsync(server.IP, TimeoutMs);
                    server.Ping = reply.Status == System.Net.NetworkInformation.IPStatus.Success
                        ? reply.RoundtripTime
                        : FailedPing;
                }
                catch
                {
                    server.Ping = FailedPing;
                }
            }
        })).ToList();

        await Task.WhenAll(pingTasks);
    }

    // Script Commands
    [RelayCommand]
    public void ChangeScriptPath()
    {
        string? folderPath = _fileService.OpenFile(ClientFileSources.SkuaScriptsDIR, "Skua Scripts (*.cs)|*.cs");

        if (!string.IsNullOrEmpty(folderPath))
            ScriptPath = folderPath;
    }

    [RelayCommand]
    public void OpenGetScripts()
    {
        IServiceProvider? services = Ioc.Default.GetService<IServiceProvider>();
        IWindowService? windowService = Ioc.Default.GetService<IWindowService>();
        if (services != null)
        {
            windowService.RegisterManagedWindow("Script Repo", services.GetRequiredService<ScriptRepoViewModel>());
            windowService.RegisterManagedWindow("Application Themes", services.GetRequiredService<ApplicationThemesViewModel>());
        }

        windowService?.ShowManagedWindow("Script Repo");
    }

    private void HandleLoadScript(LoadScriptMessage message)
    {
        if (string.IsNullOrEmpty(message.Path))
            return;

        ScriptPath = message.Path;

        _dialogService.ShowMessageBox($"Script loaded: {Path.GetFileName(message.Path)}", "Script Loaded");
    }

    private void ReindexAccounts()
    {
        for (int i = 0; i < Accounts.Count; i++)
        {
            Accounts[i].AccountNumber = i + 1;
        }
    }

    private void RefreshAccountDisplayNames()
    {
        foreach (IAccountItemViewModel account in Accounts)
        {
            account.RefreshDisplayName();
        }
    }
}

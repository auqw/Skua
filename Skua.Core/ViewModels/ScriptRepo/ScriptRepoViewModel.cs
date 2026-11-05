using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models;
using Skua.Core.Models.GitHub;
using Skua.Core.Utils;

namespace Skua.Core.ViewModels;

public partial class ScriptRepoViewModel : BotControlViewModelBase
{
    private readonly IGetScriptsService _getScriptsService;
    private readonly IProcessService _processService;
    private readonly SemaphoreSlim _refreshGate = new(1, 1);

    public ScriptRepoViewModel(IGetScriptsService getScripts, IProcessService processService)
        : base("Search Scripts", 969, 500)
    {
        _getScriptsService = getScripts;
        _processService = processService;
        OpenScriptFolderCommand = new RelayCommand(_processService.OpenVSC);
    }

    protected override void OnActivated()
    {
        _getScriptsService.PropertyChanged += GetScriptsService_PropertyChanged;
        if (!StrongReferenceMessenger.Default.IsRegistered<ScriptStartedMessage, int>(this, (int)MessageChannels.ScriptStatus))
            StrongReferenceMessenger.Default.Register<ScriptRepoViewModel, ScriptStartedMessage, int>(this, (int)MessageChannels.ScriptStatus, (r, _) => r.OnScriptStarted());
        if (_scripts.Count == 0 || _getScriptsService.Scripts.Count == 0)
            _ = RefreshScripts(CancellationToken.None);
        else
            _ = RefreshScriptsList();
    }

    protected override void OnDeactivated()
    {
        _getScriptsService.PropertyChanged -= GetScriptsService_PropertyChanged;
        StrongReferenceMessenger.Default.Unregister<ScriptStartedMessage, int>(this, (int)MessageChannels.ScriptStatus);
    }

    private void GetScriptsService_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IGetScriptsService.Scripts))
            _ = RefreshScriptsList();
    }

    [ObservableProperty]
    private bool _isManagerMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ScriptQuantity), nameof(BotScriptQuantity))]
    private RangedObservableCollection<ScriptInfoViewModel> _scripts = new();

    [ObservableProperty]
    private ScriptInfoViewModel? _selectedItem;

    // Counted off the UI thread: the outdated check hashes every downloaded script.
    [ObservableProperty]
    private int _downloadedQuantity;

    [ObservableProperty]
    private int _outdatedQuantity;

    private int _countsVersion;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _progressReportMessage = string.Empty;

    [ObservableProperty]
    private string _sortBy = "Name";

    [ObservableProperty]
    private bool _sortDescending;

    [ObservableProperty]
    private string _filterBy = "All";

    public List<string> SortOptions { get; } = new() { "Name", "Date Created" };
    public List<string> FilterOptions { get; } = new() { "All", "Army", "Classes", "Dailies", "Evil", "Farm", "Good", "Legion", "Local", "Nation", "Other", "Rep", "Seasonal", "Story", "Ultras" };

    public int ScriptQuantity => _getScriptsService.Total;
    public int BotScriptQuantity => _scripts.Count;
    public IRelayCommand OpenScriptFolderCommand { get; }
    public Action? RebuildIndexCallback { get; set; }

    partial void OnSortByChanged(string value) => _ = RefreshScriptsList();
    partial void OnSortDescendingChanged(bool value) => _ = RefreshScriptsList();
    partial void OnFilterByChanged(string value) => _ = RefreshScriptsList();

    [RelayCommand]
    private void OpenScript()
    {
        if (SelectedItem is null || !SelectedItem.Downloaded)
            return;

        StrongReferenceMessenger.Default.Send<EditScriptMessage, int>(new(SelectedItem.LocalFile), (int)MessageChannels.ScriptStatus);
    }

    [RelayCommand]
    private void LoadScript(ScriptInfoViewModel? script)
    {
        if (script is null || !script.Downloaded)
            return;

        StrongReferenceMessenger.Default.Send<LoadScriptMessage, int>(new(script.LocalFile), (int)MessageChannels.ScriptStatus);
        ProgressHandler($"Loaded {script.FileName}");
    }

    // The loader may ask first or refuse (another script running), so "Started" waits for ScriptStartedMessage.
    private ScriptInfoViewModel? _pendingStart;

    [RelayCommand]
    private void StartScript(ScriptInfoViewModel? script)
    {
        if (script is null || !script.Downloaded)
            return;

        _pendingStart = script;
        StrongReferenceMessenger.Default.Send<StartScriptMessage, int>(new(script.LocalFile), (int)MessageChannels.ScriptStatus);
    }

    private void OnScriptStarted()
    {
        ScriptInfoViewModel? pending = _pendingStart;
        string? started = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetService<IScriptManager>()?.LoadedScript;
        if (pending is null || string.IsNullOrEmpty(started)
            || !string.Equals(Path.GetFullPath(started), Path.GetFullPath(pending.LocalFile), StringComparison.OrdinalIgnoreCase))
            return;

        _pendingStart = null;
        ProgressHandler($"Started {pending.FileName}");
    }

    [RelayCommand]
    private async Task RefreshScripts(CancellationToken token)
    {
        IsBusy = true;
        try
        {
            Progress<string> progress = new(ProgressHandler);
            await _getScriptsService.RefreshScriptsAsync(progress, token);
        }
        catch { }

        await RefreshScriptsList();
    }

    [RelayCommand]
    private async Task UpdateDates(CancellationToken token)
    {
        IsBusy = true;
        try
        {
            Progress<string> progress = new(ProgressHandler);
            await _getScriptsService.RefreshScriptsAsync(progress, token);
        }
        catch { }

        await RefreshScriptsList();
    }

    [RelayCommand]
    private void AddCustomFolder()
    {
        var fileDialog = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<IFileDialogService>();
        string? folder = fileDialog.OpenFolder(ClientFileSources.SkuaScriptsDIR);
        if (!string.IsNullOrEmpty(folder))
        {
            CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<ISettingsService>().Set("UserCustomScriptsFolder", folder);
            _ = RefreshScriptsList();
        }
    }

    [RelayCommand]
    private void ClearCustomFolder()
    {
        CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<ISettingsService>().Set("UserCustomScriptsFolder", string.Empty);
        _ = RefreshScriptsList();
    }

    [RelayCommand]
    private void LoadLocalScript()
    {
        var fileDialog = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<IFileDialogService>();
        string? path = fileDialog.OpenFile(ClientFileSources.SkuaScriptsDIR, "Skua Scripts (*.cs)|*.cs");
        if (string.IsNullOrEmpty(path))
            return;

        var settings = CommunityToolkit.Mvvm.DependencyInjection.Ioc.Default.GetRequiredService<ISettingsService>();
        var list = settings.Get<System.Collections.Specialized.StringCollection>("UserCustomScriptsList") ?? new System.Collections.Specialized.StringCollection();
        if (!list.Contains(path))
        {
            list.Add(path);
            settings.Set("UserCustomScriptsList", list);
            _ = RefreshScriptsList();
        }

        StrongReferenceMessenger.Default.Send<LoadScriptMessage, int>(new(path), (int)MessageChannels.ScriptStatus);
    }

    private async Task RefreshScriptsList()
    {
        await _refreshGate.WaitAsync();
        try
        {
            _scripts.Clear();

            if (_getScriptsService.Scripts != null)
            {
                List<ScriptInfoViewModel> scriptViewModels = await Task.Run(() =>
                {
                    List<ScriptInfoViewModel> viewModels = new();
                    HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
                    foreach (ScriptInfo script in _getScriptsService.Scripts)
                    {
                        if (!PassesFilter(script))
                            continue;

                        if (script?.Name != null && !script.Name.Equals("null"))
                        {
                            if (!seen.Add(script.FilePath))
                                continue;

                            if (script.Description?.Equals("null") == true)
                                script.Description = "No description provided.";

                            if (script.Tags?.Contains("null") == true && script.Tags.Length == 1)
                                script.Tags = new[] { "no-tags" };
                            else
                                script.Tags ??= new[] { "no-tags" };

                            viewModels.Add(new(script));
                        }
                    }

                    return ApplySort(viewModels);
                });

                _scripts.AddRange(scriptViewModels);
            }

            RebuildIndexCallback?.Invoke();

            OnPropertyChanged(nameof(ScriptQuantity));
            OnPropertyChanged(nameof(BotScriptQuantity));
            IsBusy = false;
        }
        finally
        {
            _refreshGate.Release();
        }

        await UpdateCountsAsync();
    }

    private async Task UpdateCountsAsync()
    {
        int version = Interlocked.Increment(ref _countsVersion);

        // Snapshot on the calling thread, then count off it: Outdated hashes files.
        List<ScriptInfo> scripts = _getScriptsService.Scripts.ToList();
        (int downloaded, int outdated) = await Task.Run(() => (scripts.Count(s => s.Downloaded), scripts.Count(s => s.Outdated)));

        // A newer count started meanwhile (a refresh, download or delete); its totals win.
        if (version != Volatile.Read(ref _countsVersion))
            return;

        DownloadedQuantity = downloaded;
        OutdatedQuantity = outdated;
    }

    private bool PassesFilter(ScriptInfo script)
    {
        if (FilterBy == "All")
            return true;

        if (FilterBy == "Local")
            return script.Tags?.Contains("Local") == true || script.FilePath.Contains("UserCustom", StringComparison.OrdinalIgnoreCase);

        string[] filterParts = script.FilePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
        return filterParts.Any(part => part.StartsWith(FilterBy, StringComparison.OrdinalIgnoreCase));
    }

    private List<ScriptInfoViewModel> ApplySort(List<ScriptInfoViewModel> viewModels)
    {
        if (SortBy == "Date Created")
        {
            return SortDescending
                ? viewModels.OrderByDescending(x => x.Info.CreationDate ?? DateTime.MinValue).ToList()
                : viewModels.OrderBy(x => x.Info.CreationDate ?? DateTime.MinValue).ToList();
        }

        return SortDescending
            ? viewModels.OrderByDescending(x => x.FileName).ToList()
            : viewModels.OrderBy(x => x.FileName).ToList();
    }

    public void ProgressHandler(string message)
    {
        ProgressReportMessage = message;
        _ = Task.Delay(3000).ContinueWith(_ =>
        {
            // A newer message keeps its own 3 s.
            if (ProgressReportMessage == message)
                ProgressReportMessage = string.Empty;
        });
    }

    [RelayCommand]
    private async Task Delete()
    {
        IsBusy = true;
        if (_selectedItem is null)
            return;

        ProgressReportMessage = $"Deleting {_selectedItem.FileName}.";
        await _getScriptsService.DeleteScriptAsync(_selectedItem.Info);
        _selectedItem.Downloaded = _selectedItem.Info.Downloaded;
        ProgressReportMessage = _selectedItem.Downloaded
            ? $"Could not delete {_selectedItem.FileName}."
            : $"Deleted {_selectedItem.FileName}.";
        OnPropertyChanged(nameof(ScriptQuantity));
        OnPropertyChanged(nameof(BotScriptQuantity));
        await UpdateCountsAsync();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task Download()
    {
        IsBusy = true;
        if (_selectedItem is null)
            return;

        ProgressReportMessage = $"Downloading {_selectedItem.FileName}.";
        await _getScriptsService.DownloadScriptAsync(_selectedItem.Info);
        ProgressReportMessage = $"Downloaded {_selectedItem.FileName}.";
        _selectedItem.Downloaded = true;
        OnPropertyChanged(nameof(ScriptQuantity));
        OnPropertyChanged(nameof(BotScriptQuantity));
        await UpdateCountsAsync();
        IsBusy = false;
    }

    [RelayCommand]
    private async Task UpdateAll()
    {
        IsBusy = true;
        ProgressReportMessage = "Updating scripts...";
        int count = await _getScriptsService.DownloadAllWhereAsync(s => s.Outdated);
        ProgressReportMessage = $"Updated {count} scripts.";
        await RefreshScriptsList();
    }

    [RelayCommand]
    private async Task DownloadAll()
    {
        IsBusy = true;
        ProgressReportMessage = "Downloading outdated/missing scripts...";
        int count = await _getScriptsService.DownloadAllWhereAsync(s => !s.Downloaded || s.Outdated);
        ProgressReportMessage = $"Downloaded {count} scripts.";
        await RefreshScriptsList();
    }

    [RelayCommand]
    public void CancelTask()
    {
        if (RefreshScriptsCommand.IsRunning)
            RefreshScriptsCommand.Cancel();
        else if (DownloadAllCommand.IsRunning)
            DownloadAllCommand.Cancel();
        else if (UpdateAllCommand.IsRunning)
            UpdateAllCommand.Cancel();
        else if (DownloadCommand.IsRunning)
            DownloadCommand.Cancel();
        else if (DeleteCommand.IsRunning)
            DeleteCommand.Cancel();
        else
            ProgressReportMessage = string.Empty;
    }
}

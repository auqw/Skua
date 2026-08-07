using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Models.GitHub;
using Skua.Core.Utils;

namespace Skua.Core.ViewModels;

public partial class ScriptRepoViewModel : BotControlViewModelBase
{
    public ScriptRepoViewModel(IGetScriptsService getScripts, IProcessService processService)
        : base("Search Scripts", 800, 450)
    {
        _getScriptsService = getScripts;
        _processService = processService;
        OpenScriptFolderCommand = new RelayCommand(_processService.OpenVSC);
    }

    protected override void OnActivated()
    {
        _ = RefreshScripts(CancellationToken.None);
    }

    private readonly IGetScriptsService _getScriptsService;
    private readonly IProcessService _processService;

    [ObservableProperty]
    private bool _isManagerMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DownloadedQuantity), nameof(OutdatedQuantity), nameof(ScriptQuantity), nameof(BotScriptQuantity))]
    private RangedObservableCollection<ScriptInfoViewModel> _scripts = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DownloadedQuantity), nameof(OutdatedQuantity), nameof(ScriptQuantity), nameof(BotScriptQuantity))]
    private ScriptInfoViewModel? _selectedItem;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _progressReportMessage = string.Empty;

    public int DownloadedQuantity => _getScriptsService?.Downloaded ?? 0;
    public int OutdatedQuantity => _getScriptsService?.Outdated ?? 0;
    public int ScriptQuantity => _getScriptsService?.Total ?? 0;
    public int BotScriptQuantity => _scripts.Count;
    public IRelayCommand OpenScriptFolderCommand { get; }
    public Action? RebuildIndexCallback { get; set; }

    private void SetProgressMessage(string message)
    {
        ProgressReportMessage = message;
        _ = Task.Delay(3000).ContinueWith(_ => ProgressReportMessage = string.Empty);
    }

    [RelayCommand]
    private void OpenScript()
    {
        if (SelectedItem is null || !SelectedItem.Downloaded)
            return;

        StrongReferenceMessenger.Default.Send<EditScriptMessage, int>(new(SelectedItem.LocalFile), (int)MessageChannels.ScriptStatus);
    }

    [RelayCommand]
    private async Task RefreshScripts(CancellationToken token)
    {
        IsBusy = true;
        try
        {
            await Task.Run(async () =>
            {
                Progress<string> progress = new(ProgressHandler);
                await _getScriptsService.GetScriptsAsync(progress, token);
            }, token);
        }
        catch { }
        await RefreshScriptsList();
    }

    private async Task RefreshScriptsList()
    {
        _scripts.Clear();
        if (_getScriptsService?.Scripts != null)
        {
            List<ScriptInfoViewModel> scriptViewModels = await Task.Run(() =>
            {
                List<ScriptInfoViewModel> viewModels = new();
                foreach (ScriptInfo script in _getScriptsService.Scripts)
                {
                    if (script?.Name != null && !script.Name.Equals("null"))
                    {
                        if (script.Description?.Equals("null") == true)
                            script.Description = "No description provided.";

                        if (script.Tags?.Contains("null") == true && (script.Tags.Length == 1))
                            script.Tags = new[] { "no-tags" };
                        else script.Tags ??= new[] { "no-tags" };

                        viewModels.Add(new(script));
                    }
                }
                return viewModels;
            });
            _scripts.AddRange(scriptViewModels);
        }

        RebuildIndexCallback?.Invoke();

        OnPropertyChanged(nameof(Scripts));
        OnPropertyChanged(nameof(DownloadedQuantity));
        OnPropertyChanged(nameof(OutdatedQuantity));
        OnPropertyChanged(nameof(ScriptQuantity));
        OnPropertyChanged(nameof(BotScriptQuantity));
        IsBusy = false;
    }

    public void ProgressHandler(string message)
    {
        ProgressReportMessage = message;
        _ = Task.Delay(3000).ContinueWith(_ => ProgressReportMessage = string.Empty);
    }

    [RelayCommand]
    private async Task Delete()
    {
        IsBusy = true;
        if (_selectedItem is null)
            return;
        SetProgressMessage($"Deleting {_selectedItem.FileName}.");
        await _getScriptsService.DeleteScriptAsync(_selectedItem.Info);
        SetProgressMessage($"Deleted {_selectedItem.FileName}.");
        _selectedItem.Downloaded = false;
        OnPropertyChanged(nameof(DownloadedQuantity));
        OnPropertyChanged(nameof(OutdatedQuantity));
        OnPropertyChanged(nameof(ScriptQuantity));
        OnPropertyChanged(nameof(BotScriptQuantity));
        IsBusy = false;
    }

    [RelayCommand]
    private async Task Download()
    {
        IsBusy = true;
        if (_selectedItem is null)
            return;
        SetProgressMessage($"Downloading {_selectedItem.FileName}.");
        await _getScriptsService.DownloadScriptAsync(_selectedItem.Info);
        SetProgressMessage($"Downloaded {_selectedItem.FileName}.");
        _selectedItem.Downloaded = true;
        OnPropertyChanged(nameof(DownloadedQuantity));
        OnPropertyChanged(nameof(OutdatedQuantity));
        OnPropertyChanged(nameof(ScriptQuantity));
        OnPropertyChanged(nameof(BotScriptQuantity));
        IsBusy = false;
    }

    [RelayCommand]
    private async Task UpdateAll()
    {
        IsBusy = true;
        SetProgressMessage("Updating scripts...");
        int count = await _getScriptsService.DownloadAllWhereAsync(s => s.Outdated);
        SetProgressMessage($"Updated {count} scripts.");
        await RefreshScriptsList();
    }

    [RelayCommand]
    private async Task DownloadAll()
    {
        IsBusy = true;
        SetProgressMessage("Downloading outdated/missing scripts...");
        int count = await Task.Run(async () => await _getScriptsService.DownloadAllWhereAsync(s => !s.Downloaded || s.Outdated));
        SetProgressMessage($"Downloaded {count} scripts.");
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
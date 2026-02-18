using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces;
using Skua.Core.Models.Quests;
using Skua.Core.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.ViewModels;

public partial class LoaderViewModel : BotControlViewModelBase, IManagedWindow
{
    public LoaderViewModel(IScriptShop shops, IScriptQuest quests, IQuestDataLoaderService questLoader, IClipboardService clipboardService)
        : base("Loader", 720, 375)
    {
        _shops = shops;
        _quests = quests;
        _questLoader = questLoader;
        _clipboardService = clipboardService;
    }

    private CancellationTokenSource? _loaderCTS;
    private readonly IScriptShop _shops;
    private readonly IScriptQuest _quests;
    private readonly IQuestDataLoaderService _questLoader;
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    private string _progressReport = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadCommand))]
    private string _inputIDs = string.Empty;

    [ObservableProperty]
    private string _questRange = string.Empty;

    [ObservableProperty]
    private int _selectedIndex;

    [ObservableProperty]
    private RangedObservableCollection<QuestData> _questIDs = new();

    [RelayCommand(CanExecute = nameof(AllDigits))]
    private void Load()
    {
        if (SelectedIndex == 0 && int.TryParse((string?)InputIDs, out int id))
        {
            Task.Factory.StartNew(() => _shops.Load(id));
            return;
        }
        if (SelectedIndex == 1)
        {
            string[] parts = InputIDs.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int[] questIds = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                questIds[i] = int.Parse(parts[i]);
            Task.Factory.StartNew(() => _quests.Load(questIds));
        }
    }

    private bool AllDigits()
    {
        if (string.IsNullOrWhiteSpace(InputIDs))
            return false;

        string normalized = InputIDs.Replace(",", "").Replace(" ", "");
        return normalized.Length > 0 && normalized.All(char.IsDigit);
    }

    [RelayCommand]
    private void LoadQuests(IList<object>? items)
    {
        if (items is null)
            return;
        int[] questIds = new int[items.Count];
        for (int i = 0; i < items.Count; i++)
            questIds[i] = ((QuestData)items[i]).ID;
        _quests.Load(questIds);
    }

    [RelayCommand]
    private void CopyQuestsNames(IList<object>? items)
    {
        if (items is null)
            return;

        List<string> names = new(items.Count);
        foreach (object item in items)
            names.Add(((QuestData)item).Name);
        _clipboardService.SetText(string.Join(",", names));
    }

    [RelayCommand]
    private void CopyQuestsIDs(IList<object>? items)
    {
        if (items is null)
            return;

        List<int> ids = new(items.Count);
        foreach (object item in items)
            ids.Add(((QuestData)item).ID);
        _clipboardService.SetText(string.Join(",", ids));
    }

    [RelayCommand]
    private void CopyQuestsNamesAndIDs(IList<object>? items)
    {
        if (items is null)
            return;

        List<string> lines = new(items.Count);
        foreach (object item in items)
        {
            QuestData quest = (QuestData)item;
            lines.Add($"// {quest.Name} | {quest.ID}");
        }
        _clipboardService.SetText(string.Join(Environment.NewLine, lines));
    }

    [RelayCommand]
    private async Task UpdateQuests(bool getAll)
    {
        _loaderCTS = new();
        QuestIDs.Clear();
        Progress<string> progress = new(progress =>
        {
            IsLoading = true;
            ProgressReport = progress;
        });
        try
        {
            List<QuestData> questData = await _questLoader.UpdateAsync("QuestData.json", getAll, progress, _loaderCTS.Token);
            QuestIDs.Clear();
            QuestIDs.AddRange(questData);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsLoading = false;
            ProgressReport = string.Empty;
            _loaderCTS.Dispose();
            _loaderCTS = null;
        }
    }

    [RelayCommand]
    private async Task GetQuests()
    {
        IsLoading = true;
        ProgressReport = "Getting quests";
        QuestIDs.Clear();
        QuestIDs.AddRange(await _questLoader.GetFromFileAsync("QuestData.json"));
        ProgressReport = string.Empty;
        IsLoading = false;
    }

    [RelayCommand]
    private async Task UpdateQuestsRange()
    {
        string[] parts = QuestRange.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 2 || !int.TryParse(parts[0], out int startId) || !int.TryParse(parts[1], out int endId))
        {
            ProgressReport = "Invalid format. Use: startID, endID";
            await Task.Delay(2000);
            ProgressReport = string.Empty;
            return;
        }

        if (startId > endId)
        {
            ProgressReport = "Start ID must be less than or equal to End ID";
            await Task.Delay(2000);
            ProgressReport = string.Empty;
            return;
        }

        _loaderCTS = new();
        QuestIDs.Clear();
        Progress<string> progress = new(progress =>
        {
            IsLoading = true;
            ProgressReport = progress;
        });
        try
        {
            List<QuestData> questData = await _questLoader.UpdateRangeAsync("QuestData.json", startId, endId, progress, _loaderCTS.Token);
            QuestIDs.Clear();
            QuestIDs.AddRange(questData);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            IsLoading = false;
            ProgressReport = string.Empty;
            _loaderCTS.Dispose();
            _loaderCTS = null;
        }
    }

    [RelayCommand]
    private void CancelQuestLoad()
    {
        if (_loaderCTS is not null)
        {
            _loaderCTS.Cancel();
            ProgressReport = "Cancelling task...";
        }
    }

    [RelayCommand]
    private async Task UpdateSelectedQuest(object? item)
    {
        if (item is null)
            return;

        QuestData selectedQuest = (QuestData)item;

        if (selectedQuest is null)
            return;


        try
        {
            IsLoading = true;
            ProgressReport = $"Fake completing quest: {selectedQuest.Name}.";
            bool questData = await Task.Run(() => _quests.UpdateQuest(selectedQuest.ID));
            IsLoading = false;
        }
        catch (OperationCanceledException)
        {
        }
    }
}

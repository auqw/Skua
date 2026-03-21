using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Skua.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skua.App.Avalonia.ViewModels.Runtime;

public partial class RegisteredQuestsViewModel : ObservableRecipient
{
    private readonly char[] _questsSeparator = { '|', ',', ' ' };

    public RegisteredQuestsViewModel(IScriptQuest quests)
    {
        _quests = quests;
    }

    protected override void OnActivated()
    {
        Messenger.Register<RegisteredQuestsViewModel, PropertyChangedMessage<IEnumerable<int>>>(this, RegisteredChanged);
        OnPropertyChanged(nameof(CurrentAutoQuests));
    }

    private readonly IScriptQuest _quests;

    [ObservableProperty]
    private string _addQuestInput = string.Empty;

    [ObservableProperty]
    private string _rewardIdInput = string.Empty;

    public List<RegisteredQuestInfo> CurrentAutoQuests
    {
        get
        {
            List<RegisteredQuestInfo> quests = new();
            foreach (int questId in _quests.Registered)
            {
                quests.Add(new RegisteredQuestInfo
                {
                    QuestId = questId,
                    RewardId = _quests.RegisteredRewards.TryGetValue(questId, out int rewardId) ? rewardId : -1
                });
            }
            return quests;
        }
    }

    [RelayCommand]
    private void RemoveAllQuests()
    {
        _quests.UnregisterAllQuests();
    }

    [RelayCommand]
    private void RemoveQuests(IList<object>? items)
    {
        if (items is null || items.Count == 0)
            return;
        int[] quests = new int[items.Count];
        for (int i = 0; i < items.Count; i++)
            quests[i] = ((RegisteredQuestInfo)items[i]).QuestId;
        _quests.UnregisterQuests(quests);
    }

    [RelayCommand]
    private void AddQuest()
    {
        if (string.IsNullOrWhiteSpace(AddQuestInput))
            return;
        if (!Enumerable.All<char>(AddQuestInput.Replace(",", "").Replace("|", "").Replace(" ", ""), char.IsDigit))
            return;

        string[] parts = AddQuestInput.Split(_questsSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return;

        // Parse reward ID (defaults to -1 if empty or invalid)
        int rewardId = -1;
        if (!string.IsNullOrWhiteSpace(RewardIdInput) && int.TryParse((string?)RewardIdInput.Trim(), out int parsedReward))
            rewardId = parsedReward;

        (int, int)[] quests = new (int, int)[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            quests[i] = (int.Parse(parts[i]), rewardId);
        _quests.RegisterQuests(quests);

        AddQuestInput = string.Empty;
        RewardIdInput = string.Empty;
    }

    private void RegisteredChanged(RegisteredQuestsViewModel recipient, PropertyChangedMessage<IEnumerable<int>> message)
    {
        if (message.PropertyName == nameof(IScriptQuest.Registered))
            recipient.OnPropertyChanged(nameof(recipient.CurrentAutoQuests));
    }
}
namespace Skua.App.Avalonia.ViewModels.Runtime;

public class RegisteredQuestInfo
{
    public int QuestId { get; set; }
    public int RewardId { get; set; }

    public override string ToString()
    {
        return RewardId == -1 ? $"{QuestId}" : $"{QuestId} (Reward: {RewardId})";
    }
}

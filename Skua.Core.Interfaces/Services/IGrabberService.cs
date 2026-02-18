using Skua.Core.Models;

namespace Skua.Core.Interfaces;

public interface IGrabberService
{
    List<object> Grab(GrabberTypes grabType);
    
    Task GetMapItem(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    
    Task TeleportToMonster(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task KillMonster(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task BankToInv(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task HouseInvToBank(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task RegisterQuests(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task AcceptQuests(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task OpenQuests(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task UpdateQuest(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task DefaultQuestTask(string identifier, Action<int[]> action, IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task LoadShop(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task BuyItems(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task SellItem(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task SellAllItems(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task EquipItems(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task InvToBank(IList<object>? objects, IProgress<string> progress, CancellationToken token);
    Task DefaultItemBaseTask(string identifier, Action<int> action, IList<object>? objects, IProgress<string> progress, CancellationToken token);
}
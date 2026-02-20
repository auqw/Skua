using CommunityToolkit.Mvvm.DependencyInjection;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Models;
using Skua.Core.Models.Items;
using Skua.Core.Models.Monsters;
using Skua.Core.Models.Quests;
using Skua.Core.Models.Shops;

namespace Skua.Core.Services;

public class GrabberService : IGrabberService
{
    public GrabberService(
        IScriptShop shops,
        IScriptQuest quests,
        IScriptInventory inventory,
        IScriptBank bank,
        IScriptHouseInv house,
        IScriptTempInv tempInv,
        IScriptMonster monsters,
        IScriptMap map)
    {
        _shops = shops;
        _quests = quests;
        _inventory = inventory;
        _bank = bank;
        _house = house;
        _tempInv = tempInv;
        _monsters = monsters;
        _map = map;
    }

    private readonly IScriptShop _shops;
    private readonly IScriptQuest _quests;
    private readonly IScriptInventory _inventory;
    private readonly IScriptBank _bank;
    private readonly IScriptHouseInv _house;
    private readonly IScriptTempInv _tempInv;
    private readonly IScriptMonster _monsters;
    private readonly IScriptMap _map;

    public List<object> Grab(GrabberTypes grabType)
    {
        List<object> items = new();
        switch (grabType)
        {
            case GrabberTypes.Shop_Items:
                items.AddRange(_shops.Items);
                break;

            case GrabberTypes.Shop_IDs:
                items.AddRange(_shops.LoadedCache);
                break;

            case GrabberTypes.Quests:
                items.AddRange(_quests.Tree);
                break;

            case GrabberTypes.Inventory_Items:
                items.AddRange(_inventory.Items);
                break;

            case GrabberTypes.House_Inventory_Items:
                items.AddRange(_house.Items);
                break;

            case GrabberTypes.Temp_Inventory_Items:
                items.AddRange(_tempInv.Items);
                break;

            case GrabberTypes.Bank_Items:
                items.AddRange(_bank.Items);
                break;

            case GrabberTypes.Cell_Monsters:
                items.AddRange(_monsters.CurrentAvailableMonsters);
                break;

            case GrabberTypes.Map_Monsters:
                items.AddRange(_monsters.MapMonsters);
                break;

            case GrabberTypes.GetMap_Item_IDs:
                items.AddRange(_map.FindMapItems() ?? new());
                break;

            default:
                return new();
        }
        return items;
    }

    public async Task GetMapItem(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No map items found/selected.");
            return;
        }

        List<MapItem> mapItems = objects.Cast<MapItem>().ToList();
        IScriptMap map = Ioc.Default.GetService<IScriptMap>()!;
        IDialogService dialogService = Ioc.Default.GetService<IDialogService>()!;
        progress.Report(mapItems.Count == 1
            ? $"Getting Map Item [{mapItems[0].ID}, input quantity..."
            : $"Getting {mapItems.Count} Map Items, input quantity...");

        var dialog = dialogService.CreateInputDialog(
            $"{(mapItems.Count == 1 ? $"Getting {mapItems[0].ID}" : $"Getting {mapItems.Count} Map Items")}",
            (string)$"Quantity:");
        if (dialogService.ShowDialog(dialog) != true)
        {
            progress.Report("Cancelled.");
            return;
        }

        if (!int.TryParse(dialog.DialogTextInput, out int result))
            return;
        try
        {
            if (mapItems.Count == 1)
            {
                await Task.Run(() => map.GetMapItem(mapItems[0].ID, result), token);
                progress.Report("Map item acquired.");
                return;
            }

            for (int index = 0; index < mapItems.Count; index++)
            {
                progress.Report($"Getting {mapItems[index].ID} x{result}");
                await Task.Run(() => map.GetMapItem(mapItems[index].ID, result), token);
                if (index != mapItems.Count - 1)
                    await Task.Delay(1000, token);
            }
            progress.Report("Map items acquired");
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }
    }

    public async Task TeleportToMonster(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No monsters found/selected.");
            return;
        }

        Monster monster = objects.Cast<Monster>().ToList()[0];
        IScriptMap map = Ioc.Default.GetService<IScriptMap>()!;
        try
        {
            await Task.Run(() => map.Jump(monster.Cell, "Left"), token);
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }
    }

    public async Task KillMonster(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No monsters found/selected.");
            return;
        }
        List<Monster> monsters = objects.Cast<Monster>().ToList();
        try
        {
            await Task.Run(async () =>
            {
                if (monsters.Count == 1)
                {
                    Monster monster = monsters[0];
                    progress.Report($"Killing {monster.Name}.");
                    Kill(monster, token);
                    progress.Report($"Killed {monster.Name}.");
                    return;
                }

                foreach (Monster t1 in monsters)
                {
                    progress.Report($"Killing {t1.Name}.");
                    Kill(t1, token);
                    await Task.Delay(1000, token);
                    progress.Report($"Killed {t1.Name}.");
                }
            }, token);
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }

        return;

        static void Kill(Monster monster, CancellationToken token)
        {
            if (monster.Cell != Ioc.Default.GetService<IScriptPlayer>()!.Cell)
                Ioc.Default.GetService<IScriptMap>()!.Jump(monster.Cell, "Left");

            Ioc.Default.GetService<IScriptKill>()!.Monster(monster, token);
        }
    }

    public async Task BankToInv(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        await DefaultItemBaseTask("Unbanking", id => Ioc.Default.GetService<IScriptBank>()!.ToInventory(id), objects, progress, token);
    }

    public async Task HouseInvToBank(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        await DefaultItemBaseTask("Banking", id => Ioc.Default.GetService<IScriptHouseInv>()!.ToBank(id), objects, progress, token);
    }

    public async Task RegisterQuests(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        await DefaultQuestTask("Registering", Ioc.Default.GetService<IScriptQuest>()!.RegisterQuests, objects, progress, token);
    }

    public async Task AcceptQuests(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        await DefaultQuestTask("Accepting", Ioc.Default.GetService<IScriptQuest>()!.EnsureAccept, objects, progress, token);
    }

    public async Task OpenQuests(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        await DefaultQuestTask("Showing", Ioc.Default.GetService<IScriptQuest>()!.Load, objects, progress, token);
    }

    public async Task UpdateQuest(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No quests found/selected.");
            return;
        }

        if (objects.Count != 1)
        {
            progress.Report("Please select exactly one quest to complete.");
            return;
        }

        int questId = objects.First() switch
        {
            Quest quest => quest.ID,
            MapItem mapItem => mapItem.QuestID,
            _ => 0
        };

        string questName = objects.First() switch
        {
            Quest quest => quest.Name,
            MapItem mapItem => $"Map Item Quest [{mapItem.QuestID}]",
            _ => "unknown"
        };

        if (questId == 0)
        {
            progress.Report("Invalid quest selected.");
            return;
        }

        try
        {
            progress.Report($"Fake completing {questName}...");
            await Task.Run(() => Ioc.Default.GetService<IScriptQuest>()!.UpdateQuest(questId), token);
            progress.Report($"Fake completed {questName}.");
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
            else
                progress.Report("Failed to complete quest.");
        }
    }

    public async Task DefaultQuestTask(string identifier, Action<int[]> action, IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No quests found/selected.");
            return;
        }

        IEnumerable<int>? questIds = objects.First() switch
        {
            Quest => objects.Cast<Quest>().Select(q => q.ID),
            MapItem => objects.Cast<MapItem>().Select(m => m.QuestID),
            _ => null
        };
        try
        {
            if (questIds is not null)
            {
                IEnumerable<int> enumerable = questIds as int[] ?? questIds.ToArray();
                progress.Report($"{identifier} {enumerable.Count()} quests...");
                await Task.Run(() => action(enumerable.ToArray()), token);
                progress.Report("Finished.");
            }
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }
    }

    public async Task LoadShop(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No items found/selected.");
            return;
        }

        ShopInfo shopInfo = objects.Cast<ShopInfo>().First();
        try
        {
            await Task.Run(() => Ioc.Default.GetService<IScriptShop>()!.Load(shopInfo.ID), token);
            progress.Report($"Shop {shopInfo.Name} [{shopInfo.ID}] loaded.");
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }
    }

    public Task BuyItems(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        return BuyItemsImpl(objects, progress, token);
    }

    public Task SellItem(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        return SellItemImpl(objects, progress, token);
    }

    private static async Task BuyItemsImpl(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No items found/selected.");
            return;
        }

        IDialogService dialogService = Ioc.Default.GetService<IDialogService>()!;
        IScriptShop shop = Ioc.Default.GetService<IScriptShop>()!;
        IScriptPlayer player = Ioc.Default.GetService<IScriptPlayer>()!;

        List<ShopItem> items = objects.Cast<ShopItem>().ToList();
        if (items.Count == 1)
        {
            ShopItem item = items[0];
            if (item is { Coins: true, Cost: > 0 })
            {
                WarnAcPurchase(progress, dialogService);
                return;
            }

            progress.Report($"Buying {item.Name}, input quantity...");
            IInputDialogViewModel dialog = dialogService.CreateInputDialog($"Buying {item.Name}", $"Buy quantity (Cost: {item.Cost} {(item.Coins ? "AC" : "Gold")})");
            if (dialogService.ShowDialog(dialog) != true)
            {
                progress.Report("Cancelled.");
                return;
            }

            if (!int.TryParse(dialog.DialogTextInput, out int quantity))
                return;

            quantity = Math.Clamp(quantity, 1, item.MaxStack);
            int totalCost = item.Cost * quantity;
            if (!item.Coins && totalCost > player.Gold)
            {
                progress.Report($"Not enough gold. Total: {totalCost:#,0}");
                dialogService.ShowMessageBox(
                    $"Not enough gold to buy {quantity} {item.Name}.\r\nTotal: {totalCost:#,0}\r\nNeeded: {totalCost - player.Gold:#,0}",
                    "Not enough gold");
                return;
            }

            try
            {
                await Task.Run(() => shop.BuyItem(item.ID, item.ShopItemID, quantity), token);
                progress.Report($"Bought {quantity} {item.Name}");
            }
            catch
            {
                if (token.IsCancellationRequested)
                    progress.Report("Task cancelled.");
            }

            return;
        }

        if (items.Any(i => i.Coins && i.Cost > 0))
        {
            WarnAcPurchase(progress, dialogService);
            return;
        }

        int totalGoldCost = items.Where(i => !i.Coins).Sum(i => i.Cost);
        if (totalGoldCost > player.Gold)
        {
            progress.Report($"Not enough gold. Total: {totalGoldCost:#,0}");
            dialogService.ShowMessageBox(
                $"Not enough gold to buy the {items.Count} items.\r\nTotal: {totalGoldCost:#,0}\r\nNeeded: {totalGoldCost - player.Gold:#,0}",
                "Not enough gold");
            return;
        }

        try
        {
            for (int index = 0; index < items.Count; index++)
            {
                ShopItem item = items[index];
                await Task.Run(() => shop.BuyItem(item.ID), token);
                progress.Report($"Bought {item.Name}");
                if (index != items.Count - 1)
                    await Task.Delay(1000, token);
            }
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }
    }

    private static async Task SellItemImpl(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        IDialogService dialogService = Ioc.Default.GetService<IDialogService>()!;
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No items found/selected.");
            return;
        }

        if (objects.Count > 1)
        {
            progress.Report("Warning");
            dialogService.ShowMessageBox($"ATTENTION - {objects.Count} items selected!\nPlease sell 1 item at a time to prevent losses.", "Selling item - Warning");
            return;
        }

        InventoryItem item = objects.Cast<InventoryItem>().First();
        if (item.Equipped)
        {
            dialogService.ShowMessageBox("Cannot sell equipped item.", "Sell item");
            return;
        }

        progress.Report($"Selling {item.Name}, input quantity...");
        IScriptShop shop = Ioc.Default.GetService<IScriptShop>()!;
        try
        {
            int maxQty = item.Category == ItemCategory.Class ? 1 : item.Quantity;
            IInputDialogViewModel dialog = dialogService.CreateInputDialog($"Selling {item.Name}", $"Sell quantity (Currently has: {maxQty})");
            if (dialogService.ShowDialog(dialog) != true)
            {
                progress.Report("Cancelled.");
                return;
            }

            if (!int.TryParse(dialog.DialogTextInput, out int quantity))
                return;

            quantity = Math.Clamp(quantity, 1, maxQty);
            await Task.Run(() => shop.SellItem(item.ID, quantity), token);
            progress.Report($"Sold {quantity} {item.Name}");
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }
    }

    private static void WarnAcPurchase(IProgress<string> progress, IDialogService dialogService)
    {
        progress.Report("AC item - Cancelled");
        dialogService.ShowMessageBox("Don't use this to buy AC items that aren't 0 AC.", "AC Item");
    }

    /*public async Task BuyItems(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No items found/selected.");
            return;
        }
        IDialogService dialogService = Ioc.Default.GetService<IDialogService>()!;
        IScriptShop shop = Ioc.Default.GetService<IScriptShop>()!;
        IScriptPlayer player = Ioc.Default.GetService<IScriptPlayer>()!;

        List<ShopItem> items = objects.Cast<ShopItem>().ToList();
        if (items.Count == 1)
        {
            ShopItem item = items[0];
            if (item is { Coins: true, Cost: > 0 })
            {
                ACWarning(progress, dialogService);
                return;
            }
            progress.Report($"Buying {item.Name}, input quantity...");
            InputDialogViewModel dialog = new((string)$"Buying {item.Name}", (string)$"Buy quantity (Cost: {item.Cost} {(item.Coins ? "AC" : "Gold")})");
            if (dialogService.ShowDialog(dialog) != true)
            {
                progress.Report("Cancelled.");
                return;
            }

            if (!int.TryParse(dialog.DialogTextInput, out int result))
                return;

            if (result > item.MaxStack)
                result = item.MaxStack;
            int totalCost = item.Cost * result;
            if (!item.Coins && totalCost > player.Gold)
            {
                progress.Report($"Not enough gold. Total: {totalCost:#,0}");
                dialogService.ShowMessageBox($"Not enough gold to buy {result} {item.Name}.\r\nTotal: {totalCost:#,0}\r\nNeeded: {totalCost - player.Gold:#,0}", "Not enough gold");
                return;
            }
            try
            {
                await Task.Run(() => shop.BuyItem(item.ID, item.ShopItemID, result), token);
                progress.Report($"Bought {result} {item.Name}");
                return;
            }
            catch
            {
                if (token.IsCancellationRequested)
                    progress.Report("Task cancelled.");
            }
        }

        List<ShopItem> coinItems = new();
        List<ShopItem> goldItems = new();
        foreach (ShopItem item in items)
        {
            if (item.Coins)
                coinItems.Add(item);
            else
                goldItems.Add(item);
        }

        if (coinItems.Count > 0 && coinItems.Sum(item => item.Cost) > 0)
        {
            ACWarning(progress, dialogService);
            return;
        }
        int totalGoldCost = 0;
        if (goldItems.Count > 0 && (totalGoldCost = goldItems.Sum(i => i.Cost)) > player.Gold)
        {
            progress.Report($"Not enough gold. Total: {totalGoldCost}");
            dialogService.ShowMessageBox($"Not enough gold to buy the {items.Count} items.\r\nTotal: {totalGoldCost:#,0}\r\nNeeded: {totalGoldCost - player.Gold:#,0}", "Not enough gold");
            return;
        }
        try
        {
            for (int index = 0; index < items.Count; index++)
            {
                await Task.Run(() => shop.BuyItem(items[index].ID), token);
                progress.Report($"Bought {items[index].Name}");
                if (index != items.Count - 1)
                    await Task.Delay(1000, token);
            }
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }

        return;

        static void ACWarning(IProgress<string> p, IDialogService dialogService)
        {
            p.Report("AC item - Cancelled");
            dialogService.ShowMessageBox("Don't use this to buy AC items that aren't 0 AC.", "AC Item");
        }
    }*/

    /*public async Task SellItem(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        IDialogService dialogService = Ioc.Default.GetService<IDialogService>()!;
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No items found/selected.");
            return;
        }
        if (objects.Count > 1)
        {
            progress.Report("Warning");
            dialogService.ShowMessageBox($"ATTENTION - {objects.Count} items selected!\nPlease sell 1 item at a time to prevent losses.", "Selling item - Warning");
            return;
        }
        InventoryItem item = objects.Cast<InventoryItem>().First();
        if (item.Equipped)
        {
            dialogService.ShowMessageBox("Cannot sell equipped item.", "Sell item");
            return;
        }
        progress.Report($"Selling {item.Name}, input quantity...");
        IScriptShop shop = Ioc.Default.GetService<IScriptShop>()!;
        try
        {
            InputDialogViewModel dialog = new((string)$"Selling {item.Name}", (string)$"Sell quantity (Currently has: {(item.Category == ItemCategory.Class ? 1 : item.Quantity)})");
            if (dialogService.ShowDialog(dialog) != true)
            {
                progress.Report("Cancelled.");
                return;
            }

            if (!int.TryParse(dialog.DialogTextInput, out int result))
                return;

            await Task.Run(() => shop.SellItem(item.ID, result), token);
            progress.Report($"Sold {result} {item.Name}");
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }
    }*/

    public async Task SellAllItems(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        IDialogService dialogService = Ioc.Default.GetService<IDialogService>()!;
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No items found/selected.");
            return;
        }
        if (objects.Count > 1)
        {
            progress.Report("Warning");
            dialogService.ShowMessageBox($"ATTENTION - {objects.Count} items selected!\nPlease sell 1 item at a time to prevent losses.", "Selling item - Warning");
            return;
        }
        InventoryItem item = objects.Cast<InventoryItem>().First();
        if (item.Equipped)
        {
            dialogService.ShowMessageBox("Cannot sell equipped item.", "Sell item");
            return;
        }
        IScriptShop shop = Ioc.Default.GetService<IScriptShop>()!;
        try
        {
            int quantity = item.Category == ItemCategory.Class ? 1 : item.Quantity;
            progress.Report($"Selling all {quantity} {item.Name}");
            await Task.Run(() => shop.SellItem(item.ID), token);
            progress.Report($"Sold {quantity} {item.Name}");
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }
    }

    public async Task EquipItems(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        await DefaultItemBaseTask("Equipping", Ioc.Default.GetService<IScriptInventory>()!.EquipItem, objects, progress, token);
    }

    public async Task InvToBank(IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        await DefaultItemBaseTask("Banking", id => Ioc.Default.GetService<IScriptInventory>()!.ToBank(id), objects, progress, token);
    }

    public async Task DefaultItemBaseTask(string identifier, Action<int> action, IList<object>? objects, IProgress<string> progress, CancellationToken token)
    {
        if (objects is null || objects.Count == 0)
        {
            progress.Report("No items found/selected.");
            return;
        }
        List<ItemBase> items = objects.Cast<ItemBase>().ToList();
        progress.Report($"{identifier} items...");
        try
        {
            if (items.Count == 1)
            {
                progress.Report($"{identifier} {items[0].Name}.");
                await Task.Run(() => action(items[0].ID), token);
                return;
            }

            for (int index = 0; index < items.Count; index++)
            {
                progress.Report($"{identifier} {items[index].Name}.");
                await Task.Run(() => action(items[index].ID), token);
                if (index != items.Count - 1)
                    await Task.Delay(1000, token);
            }
        }
        catch
        {
            if (token.IsCancellationRequested)
                progress.Report("Task cancelled.");
        }
    }
}

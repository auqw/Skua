using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Skua.App.Avalonia.ViewModels.Grabber;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.FromCore.AppStartup;

internal class Grabber
{
    internal static IGrabberService? _grabberService;
    
    internal static GrabberViewModel CreateViewModel(IServiceProvider s)
    {
        return new GrabberViewModel(s.GetRequiredService<IEnumerable<GrabberListViewModel>>());
    }
    
    internal static IEnumerable<GrabberListViewModel> CreateListViewModels(IServiceProvider s)
    {
        IGrabberService grabberService = s.GetService<IGrabberService>()!;
        IDialogService dialogService = s.GetService<IDialogService>()!;
        IScriptInventory inventory = s.GetService<IScriptInventory>()!;
        IScriptShop shops = s.GetService<IScriptShop>()!;
        List<GrabberTaskViewModel> baseQuestCommands = new()
        {
            new("Open", _grabberService.OpenQuests),
            new("Accept", _grabberService.AcceptQuests)
        };
        List<GrabberTaskViewModel> questCommands = new(baseQuestCommands)
        {
            new("Register", _grabberService.RegisterQuests),
            new("Fake Complete", _grabberService.UpdateQuest),
            new("Unregister All", async (i, p, t) =>
            {
                p.Report("Working...");
                await Task.Run(() => Ioc.Default.GetService<IScriptQuest>()!.UnregisterAllQuests(), t);
                p.Report("Finished.");
            })
        };
        List<GrabberTaskViewModel> inventoryCommands = new()
        {
            new("Equip", _grabberService.EquipItems),
            new("Sell", _grabberService.SellItem),
            new("Sell All", _grabberService.SellAllItems),
            new("To Bank", _grabberService.InvToBank)
        };
        List<GrabberTaskViewModel> mapMonstersCommands = new()
        {
            new("Kill", _grabberService.KillMonster),
            new("Teleport To", _grabberService.TeleportToMonster)
        };
        List<GrabberTaskViewModel> mapItemCommands = new(baseQuestCommands)
        {
            new("Get Map Item", _grabberService.GetMapItem)
        };
        return new List<GrabberListViewModel>()
        {
            new("Shop Items", grabberService, GrabberTypes.Shop_Items, new GrabberTaskViewModel("Buy", _grabberService.BuyItems), true),
            new("Shop IDs", grabberService, GrabberTypes.Shop_IDs, new GrabberTaskViewModel("Load Shop", _grabberService.LoadShop), false),
            new("Quests", grabberService, GrabberTypes.Quests, questCommands, true),
            new("Inventory", grabberService, GrabberTypes.Inventory_Items, inventoryCommands, true),
            new("House Inventory", grabberService, GrabberTypes.House_Inventory_Items, new GrabberTaskViewModel("To Bank", _grabberService.HouseInvToBank), true),
            new("Temp Inventory", grabberService, GrabberTypes.Temp_Inventory_Items, false),
            new("Bank Items", grabberService, GrabberTypes.Bank_Items, new GrabberTaskViewModel("To Inventory", _grabberService.BankToInv), true),
            new("Cell Monsters", grabberService, GrabberTypes.Cell_Monsters, new GrabberTaskViewModel("Kill", _grabberService.KillMonster), true),
            new("Map Monsters", grabberService, GrabberTypes.Map_Monsters, mapMonstersCommands, true),
            new("GetMap Item IDs", grabberService, GrabberTypes.GetMap_Item_IDs, mapItemCommands, true)
        };
    }
}
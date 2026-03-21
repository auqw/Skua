using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Skua.Core.Interfaces;
using Skua.App.Avalonia.ViewModels.Packets;

namespace Skua.App.Avalonia.AppStartup;

internal static class PacketInterceptor
{
    internal static PacketInterceptorViewModel CreateViewModel(IServiceProvider s)
    {
        List<PacketLogFilterViewModel> filters = new()
        {
            new("Combat", p =>
                p[0].Contains("\"cmd\":\"restRequest\"") ||
                p[0].Contains("\"cmd\":\"gar\"") ||
                p[0].Contains("\"cmd\":\"aggroMon\"")),
            new("User Data", p =>
                p[0].Contains("\"cmd\":\"retrieveUserData\"") ||
                p[0].Contains("\"cmd\":\"retrieveUserDatas\"")),
            new("Join", p =>
                p[0].Contains("\"cmd\":\"moveToArea\"") ||
                p[0].Contains("\"cmd\":\"tfer\"") ||
                p[0].Contains("\"cmd\":\"house\"") ||
                p[0].Contains("action='joinOK'")),
            new("Jump", p =>
                p[0].Contains("\"cmd\":\"moveToCell\"")),
            new("Movement", p =>
                p[0].Contains("\"cmd\":\"mv\"") ||
                p[0].Contains("\"cmd\":\"mtcid\"")),
            new("Get Map", p =>
                p[0].Contains("\"cmd\":\"getMapItem\"")),
            new("Quest", p =>
                p[0].Contains("\"cmd\":\"getQuest\"") ||
                p[0].Contains("\"cmd\":\"acceptQuest\"") ||
                p[0].Contains("\"cmd\":\"tryQuestComplete\"") ||
                p[0].Contains("\"cmd\":\"updateQuest\"")),
            new("Shop", p =>
                p[0].Contains("\"cmd\":\"loadShop\"") ||
                p[0].Contains("\"cmd\":\"buyItem\"") ||
                p[0].Contains("\"cmd\":\"sellItem\"")),
            new("Equip", p =>
                p[0].Contains("\"cmd\":\"equipItem\"")),
            new("Drop", p =>
                p[0].Contains("\"cmd\":\"getDrop\"")),
            new("Chat", p =>
                p[0].Contains("\"cmd\":\"message\"") ||
                p[0].Contains("\"cmd\":\"cc\"")),
            new("Auras", p =>
                p[0].Contains("\"cmd\":\"aura+p\"") ||
                p[0].Contains("\"cmd\":\"aura-p\"") ||
                p[0].Contains("\"cmd\":\"clearAuras\"")),
            new("Skills", p =>
                p[0].Contains("\"cmd\":\"sAct\"")),
            new("Stats", p =>
                p[0].Contains("\"cmd\":\"uotls\"") ||
                p[0].Contains("\"cmd\":\"tempSta\"")),
            new("Inventory", p =>
                p[0].Contains("\"cmd\":\"loadInventoryBig\"") ||
                p[0].Contains("\"cmd\":\"loadInventory\"")),
            new("Class", p =>
                p[0].Contains("\"cmd\":\"updateClass\"")),
            new("Misc", p =>
                p[0].Contains("\"cmd\":\"crafting\"") ||
                p[0].Contains("\"cmd\":\"setHomeTown\"") ||
                p[0].Contains("\"cmd\":\"afk\"") ||
                p[0].Contains("\"cmd\":\"summonPet\""))
        };

        List<PacketLogFilterViewModel> specificFilters = new(filters);
        filters.Add(new("Other", p =>
        {
            foreach (PacketLogFilterViewModel f in specificFilters)
            {
                if (f.Filter.Invoke(p))
                    return false;
            }
            return true;
        }));

        return new PacketInterceptorViewModel(filters, s.GetRequiredService<ICaptureProxy>(), s.GetRequiredService<IScriptServers>());
    }
}

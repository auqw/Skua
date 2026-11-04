using Microsoft.Extensions.DependencyInjection;
using Skua.Core.Interfaces;
using Skua.Core.ViewModels;

namespace Skua.Core.AppStartup;

internal static class PacketInterceptor
{
    internal static PacketInterceptorViewModel CreateViewModel(IServiceProvider s)
    {
        Predicate<string[]> join = IsCmd("moveToArea", "tfer", "house");
        List<PacketLogFilterViewModel> filters = new()
        {
            new("Combat", IsCmd("restRequest", "gar", "aggroMon", "ct")),
            new("User Data", IsCmd("retrieveUserData", "retrieveUserDatas")),
            new("Join", p => join(p) || p[0].Contains("action='joinOK'")),
            new("Jump", IsCmd("moveToCell")),
            new("Movement", IsCmd("mv", "mtcid")),
            new("Get Map", IsCmd("getMapItem")),
            new("Quest", IsCmd("getQuest", "getQuests", "acceptQuest", "tryQuestComplete", "updateQuest")),
            new("Shop", IsCmd("loadShop", "buyItem", "sellItem")),
            new("Equip", IsCmd("equipItem")),
            new("Drop", IsCmd("getDrop")),
            new("Chat", IsCmd("message", "cc", "chatm", "whisper")),
            new("Auras", IsCmd("aura+", "aura-", "aura*", "aura++", "aura--", "aura+p", "aura-p", "clearAuras")),
            new("Skills", IsCmd("sAct")),
            new("Stats", IsCmd("uotls", "tempSta")),
            new("Inventory", IsCmd("loadInventoryBig", "loadInventory")),
            new("Class", IsCmd("updateClass")),
            new("Misc", IsCmd("crafting", "setHomeTown", "afk", "summonPet"))
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

    // JSON packets match "cmd":"x" anywhere, so aura events nested in ct count too; %xt% packets match their command.
    private static Predicate<string[]> IsCmd(params string[] cmds)
    {
        string[] jsonCmds = cmds.Select(c => $"\"cmd\":\"{c}\"").ToArray();
        return p =>
        {
            string? strCmd = GetStrCommand(p[0]);
            return strCmd is not null ? cmds.Contains(strCmd) : jsonCmds.Any(p[0].Contains);
        };
    }

    // Client: %xt%zm%x%room%... or %xt%zm%cmd%room%x%... (tfer goes through "cmd"). Server: %xt%x%room%...
    private static string? GetStrCommand(string packet)
    {
        if (!packet.StartsWith("%xt%"))
            return null;

        string[] parts = packet.Split('%', 7);
        if (parts.Length < 4)
            return null;
        if (parts[2] != "zm")
            return parts[2];
        if (parts[3] == "cmd")
            return parts.Length > 5 ? parts[5] : null;
        return parts[3];
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Skua.App.Avalonia.ViewModels;
using Skua.App.Avalonia.ViewModels.CoreBotsOptions;
using Skua.App.Avalonia.ViewModels.CoreBotsOptions.Options;
using Skua.App.Avalonia.ViewModels.Options;
using Skua.Core.Interfaces;
using Skua.Shared.Avalonia.ViewModels;
using Skua.Shared.Avalonia.ViewModels.Options;
using System;
using System.Collections.Generic;

namespace Skua.App.Avalonia.AppStartup;

internal class CoreBots
{
    internal static CoreBotsViewModel CreateViewModel(IServiceProvider s)
    {
        List<TabItemViewModel> tabs = new()
        {
            new((string)"Loadout", (ObservableObject)s.GetRequiredService<CBOLoadoutViewModel>()),
            new((string)"Options", (ObservableObject)s.GetRequiredService<CBOptionsViewModel>()),
            new((string)"Other", (ObservableObject)CreateOtherOptions(s)),
        };

        return new(tabs, s.GetRequiredService<IScriptPlayer>(), s.GetRequiredService<IDialogService>());
    }

    internal static CBOptionsViewModel CreateOptions(IServiceProvider s)
    {
        List<DisplayOptionItemViewModelBase> options = new()
        {
            new DisplayOptionItemViewModel<bool>("Private Rooms", "PrivateRooms", true),
            new DisplayOptionItemViewModel<bool>("Public on Difficult Parts", "PublicDifficult"),
            new DisplayOptionItemViewModel<bool>("Bank Misc. AC items on Start-Up", "BankMiscAC", true),
            new DisplayOptionItemViewModel<bool>("Logger in Chat", "LoggerInChat", true),

            new DisplayOptionItemViewModel<bool>("Force Off MessageBoxes", "MessageBoxCheck"),
            new DisplayOptionItemViewModel<bool>("Should Rest", "RestCheck"),
            new DisplayOptionItemViewModel<bool>("Disable AutoEnhance", "DisableAutoEnhance"),
            new DisplayOptionItemViewModel<bool>("Disable BestGear", "DisableBestGear"),
            new DisplayOptionItemViewModel<bool>("Anti Lag", "AntiLag", true),
            new DisplayOptionItemViewModel<bool>("Incognito Mode", "IncognitoMode", true),

            new DisplayOptionItemViewModel<int>("Room Number", "PrivateRoomNr", 100000),
            new DisplayOptionItemViewModel<int>("Action Delay", "ActionDelayNr", 700),
            new DisplayOptionItemViewModel<int>("Exit Combat Delay", "ExitCombatNr", 1600),
            new DisplayOptionItemViewModel<int>("Hunt Delay", "HuntDelayNr", 100),
            new DisplayOptionItemViewModel<int>("Accept and Complete Tries", "QuestTriesNr", 20),
            new DisplayOptionItemViewModel<int>("Loaded Quests Max.", "QuestMaxNr", 150),

            new DisplayOptionItemViewModel<string>("Map after stopping the Bot", "StopLocationSelect", "Home"),
        };

        return new(options, s.GetRequiredService<IDialogService>());
    }

    private static CBOOtherOptionsViewModel CreateOtherOptions(IServiceProvider s)
    {
        List<CBOOptionItemContainerViewModel> otherOptions = new()
        {
            new((string)"Boosters", new List<DisplayOptionItemViewModelBase>()
            {
                new CBOBoolOptionItemViewModel("Use Gold Boosts when farming gold", "doGoldBoost"),
                new CBOBoolOptionItemViewModel("Use Class Boosts when ranking up a class", "doClassBoost"),
                new CBOBoolOptionItemViewModel("Use Reputation Boosts when farming REP", "doRepBoost"),
                new CBOBoolOptionItemViewModel("Use Experience Boosts when farming EXP", "doExpBoost")
            }),

            new((string)"Nation Farms", new List<DisplayOptionItemViewModelBase>()
            {
                new CBOBoolOptionItemViewModel("Sell Voucher of Nulgath if not needed", "Nation_SellMemVoucher", true),
                new CBOBoolOptionItemViewModel("Do Swindles Return during Supplies", "Nation_ReturnPolicyDuringSupplies", true),
                new CBOBoolOptionItemViewModel("Ultra Alteon for Supplies", "Use Ultra Alteon when farming Supplies instead of Escherion", "UltraAlteonForSupplies", false)
            }),

            new((string)"Bludrut Brawl (PvP)", (DisplayOptionItemViewModelBase)new CBOBoolOptionItemViewModel("Kill ads before boss", "Whether to kill brawlers and restorers in the PvP before the boss.", "PvP_SoloPvPBoss")),

            new((string)"Bot Creators Only", (DisplayOptionItemViewModelBase)new CBOBoolOptionItemViewModel("Story Bot Test Mode", "Toggles innate isCompleteBefore checks. It remains on the for Complete Once Quests", "BCO_Story_TestBot"))
        };

        return new(otherOptions);
    }
}
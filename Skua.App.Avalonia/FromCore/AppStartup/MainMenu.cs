using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Skua.App.Avalonia.ViewModels;
using Skua.App.Avalonia.ViewModels.AdvancedSkills;
using Skua.App.Avalonia.ViewModels.AppLogs;
using Skua.App.Avalonia.ViewModels.CoreBotsOptions;
using Skua.App.Avalonia.ViewModels.FastTravel;
using Skua.App.Avalonia.ViewModels.Grabber;
using Skua.App.Avalonia.ViewModels.HotKeys;
using Skua.App.Avalonia.ViewModels.MainMenu;
using Skua.App.Avalonia.ViewModels.Options;
using Skua.App.Avalonia.ViewModels.Packets;
using Skua.App.Avalonia.ViewModels.Plugins;
using Skua.App.Avalonia.ViewModels.Runtime;
using Skua.Core.Interfaces;
using Skua.Shared.Avalonia.ViewModels;
using Skua.Shared.Avalonia.ViewModels.ScriptRepo;
using System;
using System.Collections.Generic;

namespace Skua.App.Avalonia.FromCore.AppStartup;

internal class MainMenu
{
    internal static MainMenuViewModel CreateViewModel(IServiceProvider s)
    {
        IWindowService windowService = s.GetRequiredService<IWindowService>();

        //windowService.RegisterManagedWindow("About", s.GetRequiredService<AboutViewModel>());
        windowService.RegisterManagedWindow("Scripts", s.GetRequiredService<ScriptLoaderViewModel>());
        windowService.RegisterManagedWindow("Script Repo", s.GetRequiredService<ScriptRepoViewModel>());

        windowService.RegisterManagedWindow("Game", s.GetRequiredService<GameOptionsViewModel>());
        windowService.RegisterManagedWindow("Application", s.GetRequiredService<ApplicationOptionsViewModel>());
        windowService.RegisterManagedWindow("CoreBots", s.GetRequiredService<CoreBotsViewModel>());
        windowService.RegisterManagedWindow("Application Themes", s.GetRequiredService<ApplicationThemesViewModel>());
        //windowService.RegisterManagedWindow("HotKeys", s.GetRequiredService<HotKeysViewModel>());

        windowService.RegisterManagedWindow("Runtime", s.GetRequiredService<RuntimeHelpersViewModel>());
        windowService.RegisterManagedWindow("Notify Drop", s.GetRequiredService<NotifyDropViewModel>());
        windowService.RegisterManagedWindow("Fast Travel", s.GetRequiredService<FastTravelViewModel>());
        windowService.RegisterManagedWindow("Current Drops", s.GetRequiredService<CurrentDropsViewModel>());
        windowService.RegisterManagedWindow("Junk Items", s.GetRequiredService<JunkItemsViewModel>());

        windowService.RegisterManagedWindow("Loader", s.GetRequiredService<LoaderViewModel>());
        windowService.RegisterManagedWindow("Grabber", s.GetRequiredService<GrabberViewModel>());
        windowService.RegisterManagedWindow("Stats", s.GetRequiredService<ScriptStatsViewModel>());
        windowService.RegisterManagedWindow("Console", s.GetRequiredService<ConsoleViewModel>());

        windowService.RegisterManagedWindow("Skills", s.GetRequiredService<AdvancedSkillsViewModel>());

        windowService.RegisterManagedWindow("Spammer", s.GetRequiredService<PacketSpammerViewModel>());
        windowService.RegisterManagedWindow("Logger", s.GetRequiredService<PacketLoggerViewModel>());
        windowService.RegisterManagedWindow("Interceptor", s.GetRequiredService<PacketInterceptorViewModel>());

        windowService.RegisterManagedWindow("Logs", s.GetRequiredService<LogsViewModel>());

        windowService.RegisterManagedWindow("Plugins", s.GetRequiredService<PluginsViewModel>());

        List<MainMenuItemViewModel> menuItems = new()
        {
            new("Scripts"),
            new("Options", new List<MainMenuItemViewModel>()
            {
                new("Game"),
                new("Application"),
                new("CoreBots"),
                new("Application Themes"),
                new("HotKeys")
            }),
            new("Helpers", new List<MainMenuItemViewModel>()
            {
                new("Runtime"),
                new("Fast Travel"),
                new("Current Drops")
            }),
            new("Tools", new List<MainMenuItemViewModel>()
            {
                new("Loader"),
                new("Grabber"),
                new("Junk Items"),
                new("Stats"),
                new("Console")
            }),
            new("Skills"),
            new("Packets", new List<MainMenuItemViewModel>()
            {
                new("Spammer"),
                new("Logger"),
                new("Interceptor")
            }),
            new("Bank", new RelayCommand(Ioc.Default.GetRequiredService<IScriptBank>().Open)),
            new("Logs")
        };

        return new(menuItems, s.GetRequiredService<AutoViewModel>(), s.GetRequiredService<JumpViewModel>(), s.GetRequiredService<IWindowService>());
    }
}
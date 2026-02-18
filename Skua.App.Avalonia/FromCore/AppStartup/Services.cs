using Microsoft.Extensions.DependencyInjection;
using Skua.App.Avalonia.FromCore.Plugins;
using Skua.App.Avalonia.ViewModels;
using Skua.App.Avalonia.ViewModels.AdvancedSkills;
using Skua.App.Avalonia.ViewModels.AppLogs;
using Skua.App.Avalonia.ViewModels.CoreBotsOptions;
using Skua.App.Avalonia.ViewModels.Dialogs;
using Skua.App.Avalonia.ViewModels.FastTravel;
using Skua.App.Avalonia.ViewModels.Grabber;
using Skua.App.Avalonia.ViewModels.HotKeys;
using Skua.App.Avalonia.ViewModels.MainMenu;
using Skua.App.Avalonia.ViewModels.Options;
using Skua.App.Avalonia.ViewModels.Packets;
using Skua.App.Avalonia.ViewModels.Plugins;
using Skua.App.Avalonia.ViewModels.Runtime;
using Skua.App.Avalonia.ViewModels.ScriptRepo;
using Skua.App.Avalonia.ViewModels.Theme;
using Skua.Core.GameProxy;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.Services;
using Skua.Core.Options;
using Skua.Core.Plugins;
using Skua.Core.Scripts;
using Skua.Core.Scripts.Helpers;
using Skua.Core.Services;
using Skua.Core.Skills;
using System.Collections.Generic;

namespace Skua.App.Avalonia.FromCore.AppStartup;

public static class Services
{
    public static IServiceCollection AddCompiler(this IServiceCollection services)
    {
        services.AddTransient(Core.AppStartup.Services.CreateCompiler);

        return services;
    }
    
    public static IServiceCollection AddScriptableObjects(this IServiceCollection services)
    {
        services.AddSingleton<IScriptInterface, ScriptInterface>();
        services.AddSingleton<IScriptManager, ScriptManager>();
        services.AddSingleton<IScriptStatus, ScriptManager>();

        services.AddSingleton<IScriptInventoryHelper, ScriptInventoryHelper>();
        services.AddSingleton<IScriptInventory, ScriptInventory>();
        services.AddSingleton<IScriptHouseInv, ScriptHouseInv>();
        services.AddSingleton<IScriptTempInv, ScriptTempInv>();
        services.AddSingleton<IScriptBank, ScriptBank>();

        services.AddSingleton<IAdvancedSkillContainer, AdvancedSkillContainer>();
        services.AddSingleton<IUltraBossHelper, UltraBossHelper>();
        services.AddSingleton<IScriptCombat, ScriptCombat>();
        services.AddSingleton<IScriptKill, ScriptKill>();
        services.AddSingleton<IScriptHunt, ScriptHunt>();
        services.AddSingleton<IScriptSkill, ScriptSkill>();
        services.AddSingleton<IScriptAuto, ScriptAuto>();
        services.AddSingleton<IScriptSelfAuras, ScriptSelfAuras>();
        services.AddSingleton<IScriptTargetAuras, ScriptTargetAuras>();

        services.AddSingleton<IScriptFaction, ScriptFaction>();
        services.AddSingleton<IScriptMonster, ScriptMonster>();
        services.AddSingleton<IScriptPlayer, ScriptPlayer>();
        services.AddSingleton<IScriptQuest, ScriptQuest>();
        services.AddSingleton<IScriptBoost, ScriptBoost>();
        services.AddSingleton<IScriptShop, ScriptShop>();
        services.AddSingleton<IScriptDrop, ScriptDrop>();
        services.AddSingleton<IScriptMap, ScriptMap>();

        services.AddSingleton<IScriptServers, ScriptServers>();
        services.AddSingleton<IScriptEvent, ScriptEvent>();
        services.AddSingleton<IScriptSend, ScriptSend>();

        services.AddTransient<IScriptOptionContainer, ScriptOptionContainer>();
        services.AddTransient<IOptionContainer, OptionContainer>();
        services.AddSingleton<IScriptOption, ScriptOption>();
        services.AddSingleton<IScriptLite, ScriptLite>();

        services.AddSingleton<IScriptBotStats, ScriptBotStats>();
        services.AddSingleton<IScriptHandlers, ScriptHandlers>();
        services.AddSingleton<IScriptWait, ScriptWait>();
        services.AddSingleton<IScriptAccounts, ScriptAccounts>();

        services.AddSingleton<ICaptureProxy, CaptureProxy>();

        services.AddSingleton<IPluginManager, PluginManager>();
        services.AddTransient<IPluginContainer, PluginContainer>();
        services.AddSingleton<IPluginHelper, PluginHelper>();

        services.AddSingleton<IMapService, MapService>();
        services.AddSingleton<ILogService, LogService>();
        services.AddSingleton<IQuestDataLoaderService, QuestDataLoaderService>();
        services.AddSingleton<IGrabberService, GrabberService>();
        services.AddSingleton<IClientFilesService, ClientFilesService>();
        services.AddSingleton<IAuraMonitorService, AuraMonitorService>();
        services.AddSingleton<IJunkService, JunkService>();
        services.AddSingleton<BackgroundThemeService>();

        return services;
    }
    
    public static IServiceCollection AddSkuaMainAppViewModels(this IServiceCollection services)
    {
        services.AddSingleton<MainViewModel>();
        services.AddSingleton(MainMenu.CreateViewModel);
        services.AddTransient<BotWindowViewModel>();
        services.AddSingleton<IEnumerable<BotControlViewModelBase>>(s => new List<BotControlViewModelBase>()
        {
            s.GetRequiredService<ScriptLoaderViewModel>(),
            s.GetRequiredService<ScriptRepoViewModel>(),
            s.GetRequiredService<LogsViewModel>(),
            s.GetRequiredService<AutoViewModel>(),
            s.GetRequiredService<JumpViewModel>(),
            s.GetRequiredService<FastTravelViewModel>(),
            s.GetRequiredService<CurrentDropsViewModel>(),
            s.GetRequiredService<JunkItemsViewModel>(),
            s.GetRequiredService<RuntimeHelpersViewModel>(),
            s.GetRequiredService<LoaderViewModel>(),
            s.GetRequiredService<GrabberViewModel>(),
            s.GetRequiredService<GameOptionsViewModel>(),
            s.GetRequiredService<ApplicationOptionsViewModel>(),
            s.GetRequiredService<ConsoleViewModel>(),
            s.GetRequiredService<AdvancedSkillsViewModel>(),
            s.GetRequiredService<PacketInterceptorViewModel>(),
            s.GetRequiredService<PacketSpammerViewModel>(),
            s.GetRequiredService<PacketLoggerViewModel>(),
            s.GetRequiredService<ApplicationThemesViewModel>(),
            s.GetRequiredService<HotKeysViewModel>(),
            s.GetRequiredService<PluginsViewModel>()
        });

        services.AddTransient<LoaderViewModel>();

        services.AddTransient(Grabber.CreateViewModel);
        services.AddSingleton(Grabber.CreateListViewModels);

        services.AddSingleton<JumpViewModel>();

        services.AddSingleton<FastTravelViewModel>();
        services.AddTransient<FastTravelEditorViewModel>();
        services.AddTransient<FastTravelEditorDialogViewModel>();

        services.AddSingleton<LogsViewModel>();
        services.AddSingleton(LogTabs.CreateViewModels);

        services.AddSingleton(Options.CreateGameOptions);
        services.AddSingleton(Options.CreateAppOptions);

        services.AddSingleton(PacketLogger.CreateViewModel);
        services.AddSingleton<PacketSpammerViewModel>();
        services.AddSingleton<PacketInterceptorViewModel>();

        services.AddTransient<ConsoleViewModel>();

        services.AddSingleton<ScriptRepoViewModel>();
        services.AddSingleton<ScriptLoaderViewModel>();

        services.AddSingleton<AdvancedSkillsViewModel>();
        services.AddSingleton<AdvancedSkillEditorViewModel>();
        services.AddSingleton<SavedAdvancedSkillsViewModel>();
        services.AddTransient<SkillRulesViewModel>();

        services.AddSingleton<AutoViewModel>();

        services.AddSingleton<BoostsViewModel>();
        services.AddSingleton<ScriptStatsViewModel>();
        services.AddSingleton<RuntimeHelpersViewModel>();
        services.AddSingleton<NotifyDropViewModel>();
        services.AddSingleton<ToPickupDropsViewModel>();
        services.AddSingleton<RegisteredQuestsViewModel>();
        services.AddSingleton<CurrentDropsViewModel>();
        services.AddSingleton<JunkItemsViewModel>();

        services.AddThemeViewModels();

        services.AddSingleton<PluginsViewModel>();

        services.AddSingleton<HotKeysViewModel>();
        //services.AddSingleton(Core.AppStartup.HotKeys.CreateHotKeys);

        services.AddSingleton(CoreBots.CreateViewModel);
        services.AddSingleton(CoreBots.CreateOptions);
        services.AddSingleton<CBOClassEquipmentViewModel>();
        services.AddSingleton<CBOClassSelectViewModel>();
        services.AddSingleton<CBOLoadoutViewModel>();

        return services;
    }
    
    public static IServiceCollection AddThemeViewModels(this IServiceCollection services)
    {
        services.AddSingleton<ApplicationThemesViewModel>();
        services.AddSingleton<ThemeSettingsViewModel>();
        services.AddSingleton<ColorSchemeEditorViewModel>();
        services.AddSingleton<BackgroundThemeViewModel>();

        return services;
    }
    
    
}
using Microsoft.Extensions.DependencyInjection;
using Skua.Core.Interfaces;
using Skua.Core.Services;
using Skua.Manager.Avalonia.ViewModels;
using Skua.Shared.Avalonia.ViewModels;
using Skua.Shared.Avalonia.ViewModels.ScriptRepo;

namespace Skua.Manager.Avalonia.AppStartup;

public static class Services
{
    public static IServiceCollection AddSkuaManagerViewModels(this IServiceCollection services)
    {
        services.AddSingleton<ApplicationThemesViewModel>();
        services.AddSingleton<ThemeSettingsViewModel>();
        services.AddSingleton<ColorSchemeEditorViewModel>();
        services.AddSingleton<BackgroundThemeViewModel>();
        
        services.AddSingleton<AccountManagerViewModel>();
        services.AddSingleton<LauncherViewModel>();
        services.AddSingleton<IClientUpdateService, ClientUpdateService>();
        services.AddSingleton<IClientFilesService, ClientFilesService>();
        services.AddSingleton<ClientUpdatesViewModel>();
        services.AddSingleton<GitHubAuthViewModel>();
        services.AddSingleton<ScriptRepoViewModel>(s =>
        {
            ScriptRepoViewModel vm = new(s.GetRequiredService<IGetScriptsService>(), s.GetRequiredService<IProcessService>())
            {
                IsManagerMode = true
            };
            return vm;
        });
        services.AddSingleton<AboutViewModel>();
        services.AddSingleton<ChangeLogsViewModel>();
        services.AddSingleton(SkuaManager.CreateViewModel);
        services.AddSingleton(SkuaManager.CreateOptionsViewModel);

        return services;
    }
}
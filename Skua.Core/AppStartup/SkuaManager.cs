using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.ViewModels;
using Skua.Core.ViewModels.Manager;

namespace Skua.Core.AppStartup;

internal class SkuaManager
{
    internal static ManagerMainViewModel CreateViewModel(IServiceProvider s)
    {
        List<TabItemViewModel> tabs = new()
        {
            new("Accounts", s.GetRequiredService<AccountManagerViewModel>()),
            new("Launcher", s.GetRequiredService<LauncherViewModel>()),
            new("Updates", s.GetRequiredService<ClientUpdatesViewModel>()),
            new("Options", s.GetRequiredService<ManagerOptionsViewModel>()),
            new("Themes", s.GetRequiredService<ApplicationThemesViewModel>()),
            new("About", s.GetRequiredService<AboutViewModel>()),
            new("Change Logs", s.GetRequiredService<ChangeLogsViewModel>()),
        };
        return new(tabs, s.GetRequiredService<IDialogService>(), s.GetRequiredService<ISettingsService>());
    }

    internal static ManagerOptionsViewModel CreateOptionsViewModel(IServiceProvider s)
    {
        List<DisplayOptionItemViewModelBase> options = new()
        {
            CreateSettingOptionItem<bool>("Use Manager theme on Skua", "Whether to use, when launching from the Launcher tab, the same theme as the Manager in any launched App", "syncTheme"),
            CreateSettingOptionItem<bool>("Check for Client Updates", "Whether to check for client updates when launching the Manager", "CheckClientUpdates"),
            CreateSettingOptionItem<bool>("Check for Client Prereleases", "Whether to check for pre-releases when checking updates", "CheckClientPrereleases"),
            CreateSettingOptionItem<bool>("Delete .zip after Download", "Whether to delete the .zip folder after downloading and extracting the new version", "DeleteZipFileAfter")
        };

        List<DisplayOptionItemViewModelBase> devOptions = new()
        {
            new CommandOptionItemViewModel<bool>(
                "Anonymise Accounts",
                "Show managed accounts as Account N labels in manager lists.",
                "AnonymiseAccounts",
                new RelayCommand<bool>(b =>
                {
                    Ioc.Default.GetRequiredService<ISettingsService>().Set("AnonymiseAccounts", b);
                    StrongReferenceMessenger.Default.Send(new RefreshAccountDisplayNamesMessage());
                }),
                Ioc.Default.GetRequiredService<ISettingsService>().Get("AnonymiseAccounts", false))
        };

        return new(options, devOptions, s.GetRequiredService<ISettingsService>(), s.GetRequiredService<IFileDialogService>());

        static RelayCommand<T> CreateSettingCommand<T>(string key) => new(b => Ioc.Default.GetRequiredService<ISettingsService>().Set(key, b));
        static CommandOptionItemViewModel<T> CreateSettingOptionItem<T>(string content, string description, string key) => new(content, description, key, CreateSettingCommand<T>(key), Ioc.Default.GetRequiredService<ISettingsService>().Get<T>(key));
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;

namespace Skua.Core.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
    private readonly IScriptPlayer _player = Ioc.Default.GetRequiredService<IScriptPlayer>();
    private readonly IDispatcherService _dispatcherService = Ioc.Default.GetRequiredService<IDispatcherService>();
    private readonly System.Timers.Timer _titleUpdateTimer = new(1000);
    private string _lastUsername = string.Empty;

    [ObservableProperty] private string _title = "Skua";
    [ObservableProperty] private bool _showUsernameInTitle;

    public MainViewModel()
    {
        ShowUsernameInTitle = _settingsService.Get("ShowUsernameInTitle", false);

        UpdateTitle();
        _titleUpdateTimer.Elapsed += (_, _) =>
        {
            string username = _player.Username;

            if (_lastUsername == username)
                return;

            _dispatcherService.Invoke(UpdateTitle);
        };

        if (ShowUsernameInTitle)
            _titleUpdateTimer.Start();
    }

    partial void OnShowUsernameInTitleChanged(bool value)
    {
        _settingsService.Set("ShowUsernameInTitle", value);

        if (value)
        {
            UpdateTitle();
            _titleUpdateTimer.Start();
        }
        else
        {
            _titleUpdateTimer.Stop();
            _lastUsername = string.Empty;
            UpdateTitle();
        }
    }

    public void UpdateTitle()
    {
        string username = _player.Username;
        _lastUsername = username;

        string title = $"Skua - {_settingsService.Get("ApplicationVersion", "0.0.0.0")}";

        if (ShowUsernameInTitle && !string.IsNullOrWhiteSpace(username))
            title += $" : {username}";

        Title = title;
    }

    [RelayCommand]
    private void ShowMainWindow() => StrongReferenceMessenger.Default.Send<ShowMainWindowMessage>();
}
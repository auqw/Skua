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

    public MainViewModel()
    {
        UpdateTitle();
        _titleUpdateTimer.Elapsed += (_, _) =>
        {
            string username = _player.Username;

            if (_lastUsername == username)
                return;

            _dispatcherService.Invoke(UpdateTitle);
        };
        _titleUpdateTimer.Start();
    }

    public void UpdateTitle()
    {
        string username = _player.Username;
        _lastUsername = username;

        Title = $"Skua - {_settingsService.Get("ApplicationVersion", "0.0.0.0")}" + (!string.IsNullOrWhiteSpace(username)? $" : {username}": "");
    }

    [RelayCommand]
    private void ShowMainWindow() => StrongReferenceMessenger.Default.Send<ShowMainWindowMessage>();
}
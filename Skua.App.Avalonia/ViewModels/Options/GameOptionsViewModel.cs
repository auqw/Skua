using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Skua.Core.Interfaces;
using Skua.Core.Models.Servers;
using Skua.Shared.Avalonia.ViewModels;
using Skua.Shared.Avalonia.ViewModels.Options;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

namespace Skua.App.Avalonia.ViewModels.Options;

public class GameOptionsViewModel : BotControlViewModelBase
{
    private readonly IScriptServers _servers;
    private readonly IScriptOption _options;

    public GameOptionsViewModel(List<DisplayOptionItemViewModelBase> gameOptions, IScriptServers servers, IScriptOption options)
        : base("Game Options", 420, 500)
    {
        _servers = servers;
        _options = options;
        GameOptions = gameOptions;
        FilteredGameOptions = new ObservableCollection<DisplayOptionItemViewModelBase>(SortOptions(gameOptions));
        ResetOptionsCommand = new RelayCommand(_options.Reset);
        ResetDefaultOptionsCommand = new RelayCommand(_options.ResetToDefault);
        SaveOptionsCommand = new RelayCommand(_options.Save);
    }

    protected override void OnActivated()
    {
        Messenger.Register<GameOptionsViewModel, PropertyChangedMessage<List<Server>>>(this, ServersChanged);
        Messenger.Register<GameOptionsViewModel, PropertyChangedMessage<string>>(this, OptionServerChanged);
    }

    public List<DisplayOptionItemViewModelBase> GameOptions { get; }
    public ObservableCollection<DisplayOptionItemViewModelBase> FilteredGameOptions { get; }
    private string _searchText = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplySearch();
        }
    }
    public List<string> ServersList
    {
        get
        {
            List<string> servers = new(_servers.CachedServers.Count);
            foreach (Server server in _servers.CachedServers)
                servers.Add(server.Name);
            return servers;
        }
    }
    private string? _selectedServer;

    public string? SelectedServer
    {
        get => _selectedServer;
        set
        {
            if (SetProperty(ref _selectedServer, value) && value is not null && _options.ReloginServer != value)
                _options.ReloginServer = value;
        }
    }

    private int _columns = 2;

    public int Columns
    {
        get => _columns;
        set
        {
            int clamped = Math.Max(1, value);
            SetProperty(ref _columns, clamped);
        }
    }

    public IRelayCommand ResetOptionsCommand { get; }
    public IRelayCommand ResetDefaultOptionsCommand { get; }
    public IRelayCommand SaveOptionsCommand { get; }

    private void ServersChanged(GameOptionsViewModel recipient, PropertyChangedMessage<List<Server>> message)
    {
        if (message.PropertyName == nameof(IScriptServers.CachedServers))
            recipient.OnPropertyChanged(nameof(recipient.ServersList));
    }

    private void OptionServerChanged(GameOptionsViewModel recipient, PropertyChangedMessage<string> message)
    {
        if (message.PropertyName == nameof(IScriptOption.ReloginServer) && message.NewValue != recipient.SelectedServer)
            recipient.SelectedServer = message.NewValue;
    }

    private void ApplySearch()
    {
        IEnumerable<DisplayOptionItemViewModelBase> filtered = GameOptions.Where(MatchesSearch);
        List<DisplayOptionItemViewModelBase> sorted = SortOptions(filtered).ToList();
        FilteredGameOptions.Clear();
        foreach (DisplayOptionItemViewModelBase option in sorted)
            FilteredGameOptions.Add(option);
    }

    private bool MatchesSearch(DisplayOptionItemViewModelBase option)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        return option.Content?.Contains(SearchText) == true;
    }

    private static IEnumerable<DisplayOptionItemViewModelBase> SortOptions(IEnumerable<DisplayOptionItemViewModelBase> options)
    {
        return options
            .OrderBy(o => o.Tag)
            .ThenBy(o => o.Content, StringComparer.OrdinalIgnoreCase);
    }
}

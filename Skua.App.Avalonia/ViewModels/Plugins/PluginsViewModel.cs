using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces;
using Skua.Core.Messaging;
using Skua.Core.Utils;
using Skua.Shared.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skua.App.Avalonia.ViewModels.Plugins;

public partial class PluginsViewModel : BotControlViewModelBase
{
    public PluginsViewModel(IPluginManager pluginManager, IFileDialogService fileService)
        : base("Plugins")
    {
        PluginManager = pluginManager;
        _fileService = fileService;
    }

    protected override void OnActivated()
    {
        StrongReferenceMessenger.Default.Register<PluginsViewModel, PluginLoadedMessage, int>(this, (int)MessageChannels.Plugins, PluginLoaded);
        StrongReferenceMessenger.Default.Register<PluginsViewModel, PluginUnloadedMessage, int>(this, (int)MessageChannels.Plugins, PluginUnLoaded);

        _allPlugins.Clear();
        _plugins.Clear();
        foreach (IPluginContainer container in PluginManager.Containers)
            _allPlugins.Add(new(container, container.OptionContainer.Options.Count > 0));
        ApplySearch();
    }

    protected override void OnDeactivated()
    {
        StrongReferenceMessenger.Default.UnregisterAll(this);
    }

    private readonly IFileDialogService _fileService;
    private readonly List<PluginItemViewModel> _allPlugins = [];

    [ObservableProperty]
    private RangedObservableCollection<PluginItemViewModel> _plugins = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    public IPluginManager PluginManager { get; }

    [RelayCommand]
    private void UnloadAllPlugins()
    {
        foreach (IPluginContainer container in PluginManager.Containers)
            PluginManager.Unload(container.Plugin);
    }

    [RelayCommand]
    private void LoadPlugin()
    {
        string? file = _fileService.OpenFile("DLL files |*.dll");

        if (string.IsNullOrEmpty(file))
            return;

        PluginManager.Load(file);
    }

    private void PluginUnLoaded(PluginsViewModel recipient, PluginUnloadedMessage message)
    {
        PluginItemViewModel? plugin = recipient._allPlugins.FirstOrDefault(p => p.Container.Plugin.Name == message.Container.Plugin.Name);
        if (plugin is not null)
            recipient._allPlugins.Remove(plugin);

        recipient.Plugins.Clear();
        recipient.Plugins.AddRange(recipient._allPlugins.Where(recipient.MatchesSearch));
    }

    private void PluginLoaded(PluginsViewModel recipient, PluginLoadedMessage message)
    {
        PluginItemViewModel item = new(message.Container, message.Container.OptionContainer.Options.Count > 0);
        recipient._allPlugins.Add(item);
        if (recipient.MatchesSearch(item))
            recipient.Plugins.Add(item);
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplySearch();
    }

    private void ApplySearch()
    {
        Plugins.Clear();
        Plugins.AddRange(_allPlugins.Where(MatchesSearch));
    }

    private bool MatchesSearch(PluginItemViewModel item)
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;

        return item.Container.Plugin.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Author.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
            || item.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
    }
}

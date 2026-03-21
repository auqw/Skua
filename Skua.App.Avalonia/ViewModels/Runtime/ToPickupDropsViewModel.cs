using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Skua.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.ViewModels.Runtime;

public partial class ToPickupDropsViewModel : ObservableRecipient
{
    private readonly char[] _dropsSeparator = { '|' };
    private readonly IWindowService _windowService;

    public ToPickupDropsViewModel(IScriptDrop drops, IScriptOption options, IWindowService windowService)
    {
        Drops = drops;
        Options = options;
        _windowService = windowService;
        RemoveAllDropsCommand = new RelayCommand(Drops.Clear);

        // Subscribe to property changes directly
        Drops.PropertyChanged += OnDropsPropertyChanged;
    }

    protected override void OnActivated()
    {
        Messenger.Register<ToPickupDropsViewModel, PropertyChangedMessage<IEnumerable<string>>>(this, ToPickupChanged);
        Messenger.Register<ToPickupDropsViewModel, PropertyChangedMessage<IEnumerable<int>>>(this, ToPickupIDsChanged);
        // Force refresh to show any drops added while the window was closed
        OnPropertyChanged(nameof(ToPickup));
    }

    [ObservableProperty]
    private string _addDropInput = string.Empty;

    public List<string> ToPickup
    {
        get
        {
            List<string> items = new();
            foreach (string name in Drops.ToPickup)
                items.Add(name);
            foreach (int id in Drops.ToPickupIDs)
                items.Add(id.ToString());
            return items;
        }
    }
    public IScriptDrop Drops { get; }
    public IScriptOption Options { get; }
    public IRelayCommand RemoveAllDropsCommand { get; }

    [RelayCommand]
    private void RemoveDrops(IList<object>? items)
    {
        if (items is null)
            return;

        List<string> names = new();
        List<int> ids = new();

        foreach (object item in items)
        {
            string itemStr = (string)item;
            if (int.TryParse(itemStr, out int itemId))
                ids.Add(itemId);
            else
                names.Add(itemStr);
        }

        if (names.Count > 0)
            Drops.Remove(names.ToArray());
        if (ids.Count > 0)
            Drops.Remove(ids.ToArray());
    }

    [RelayCommand]
    private async Task ToggleDrops()
    {
        if (Drops.Enabled)
            await Drops.StopAsync();
        else
            Drops.Start();
    }

    [RelayCommand]
    private void AddDrop()
    {
        if (string.IsNullOrWhiteSpace(AddDropInput))
            return;

        string[] inputs = AddDropInput.Split(_dropsSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        List<string> names = new();
        List<int> ids = new();

        foreach (string input in inputs)
        {
            if (int.TryParse(input.Trim(), out int itemId))
                ids.Add(itemId);
            else
                names.Add(input);
        }

        if (names.Count > 0)
            Drops.Add(names.ToArray());
        if (ids.Count > 0)
            Drops.Add(ids.ToArray());

        AddDropInput = string.Empty;
    }

    private void ToPickupChanged(ToPickupDropsViewModel recipient, PropertyChangedMessage<IEnumerable<string>> message)
    {
        if (message.PropertyName == nameof(IScriptDrop.ToPickup))
            recipient.OnPropertyChanged(nameof(recipient.ToPickup));
    }

    private void ToPickupIDsChanged(ToPickupDropsViewModel recipient, PropertyChangedMessage<IEnumerable<int>> message)
    {
        // Check for both the interface name and the property name
        if (message.PropertyName is (nameof(IScriptDrop.ToPickupIDs)) or "ToPickupIDs")
            recipient.OnPropertyChanged(nameof(recipient.ToPickup));
    }

    [RelayCommand]
    private void OpenNotifyDrop()
    {
        _windowService.ShowManagedWindow("Notify Drop");
    }

    private void OnDropsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is (nameof(IScriptDrop.ToPickup)) or (nameof(IScriptDrop.ToPickupIDs)))
            OnPropertyChanged(nameof(ToPickup));
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces;
using Skua.Core.Models.Items;
using System.ComponentModel;

namespace Skua.Core.ViewModels;

public partial class CurrentDropsViewModel : BotControlViewModelBase
{
    public CurrentDropsViewModel(IScriptDrop drops, IScriptPlayer player)
        : base("Current Drops", 500, 400)
    {
        _drops = drops;
        _player = player;
    }

    protected override void OnActivated()
    {
        _drops.PropertyChanged += CurrentDropsChanged;
        OnPropertyChanged(nameof(CurrentDrops));
    }

    protected override void OnDeactivated()
    {
        _drops.PropertyChanged -= CurrentDropsChanged;
    }

    private readonly IScriptPlayer _player;
    private readonly IScriptDrop _drops;

    [ObservableProperty]
    private ItemBase? _selectedDrop;

    public List<ItemBase> CurrentDrops
    {
        get
        {
            List<ItemBase> drops = new();
            foreach (ItemBase drop in _drops.CurrentDropInfos)
                drops.Add(drop);
            return drops;
        }
    }

    [RelayCommand]
    private void Pickup()
    {
        if (SelectedDrop is null || !_player.Playing)
            return;

        _drops.Pickup(SelectedDrop.ID);
    }

    [RelayCommand]
    private void PickupSelected(IList<object>? items)
    {
        if (items is null || !_player.Playing)
            return;

        int[] dropIds = new int[items.Count];
        for (int i = 0; i < items.Count; i++)
            dropIds[i] = ((ItemBase)items[i]).ID;
        _drops.Pickup(dropIds);
    }

    [RelayCommand]
    private void PickupAll()
    {
        _drops.PickupAll(true);
    }

    [RelayCommand]
    private void PickupAC()
    {
        _drops.PickupACItems();
    }

    private void CurrentDropsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IScriptDrop.CurrentDropInfos))
            OnPropertyChanged(nameof(CurrentDrops));
    }
}
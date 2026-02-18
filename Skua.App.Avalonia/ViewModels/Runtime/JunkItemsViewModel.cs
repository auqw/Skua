using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces;
using Skua.Core.Models.Items;
using Skua.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skua.App.Avalonia.ViewModels.Runtime;

public sealed partial class JunkItemsViewModel : BotControlViewModelBase
{
    private readonly IScriptInventory _inventory;
    private readonly IScriptBank _bank;
    private readonly IJunkService _junkService;
    private readonly IDialogService _dialogService;
    private readonly ISettingsService _settings;

    public JunkItemsViewModel(
        IScriptInventory inventory,
        IScriptBank bank,
        IJunkService junkService,
        IDialogService dialogService,
        ISettingsService settings)
       : base("Junk Items")
    {
        _inventory = inventory;
        _bank = bank;
        _junkService = junkService;
        _dialogService = dialogService;
        _settings = settings;
        Items = new RangedObservableCollection<JunkItemEntry>();
        _skipSellWarning = _settings.Get("JunkSkipSellWarning", false);
    }

    public RangedObservableCollection<JunkItemEntry> Items { get; }

    [ObservableProperty]
    private bool _skipSellWarning;

    public int TotalJunk => Items.Count(i => i.IsJunk);

    protected override void OnActivated()
    {
        base.OnActivated();
        Refresh();
    }

    [RelayCommand]
    private async Task Refresh()
    {
        _junkService.Load();

        var junkIds = _junkService.JunkItems
            .GroupBy(j => j.ID)
            .Select(g => g.Key)
            .ToHashSet();

        // Snapshot current selection state so we can restore it after reload
        var previousStates = Items
            .ToList()
            .ToDictionary<JunkItemEntry, (int ID, string Category), bool>(
                e => (e.ID, e.Category),
                e => e.IsSelected);

        List<JunkItemEntry> entries = await Task.Run(() =>
        {
            // Ensure bank is loaded so items list is accurate, but off the UI thread
            if (!_bank.Loaded)
                _bank.Load();

            // Take snapshots to avoid repeated enumeration while we build the list
            List<InventoryItem> inventoryItems = _inventory.Items.ToList();
            List<InventoryItem> bankItems = _bank.Items.ToList();

            IEnumerable<InventoryItem> allItems = inventoryItems.Concat(bankItems);

            return allItems
                .GroupBy(i => i.ID)
                .Select(g => g.First())
                .OrderBy(i => i.Name)
                .Select(item => new JunkItemEntry(item.ID, item.Name, item.Category.ToString(), junkIds.Contains(item.ID)))
                .ToList();
        });

        // Restore selection state where possible (same ID + category)
        foreach (JunkItemEntry entry in entries)
        {
            if (previousStates.TryGetValue((entry.ID, entry.Category), out bool isSelected))
                entry.IsSelected = isSelected;
        }

        Items.ReplaceRange(entries);
        OnPropertyChanged(nameof(TotalJunk));
    }

    [RelayCommand]
    private void MarkAsJunk()
    {
        if (Items.Count == 0)
            return;

        var updated = Items.ToList();
        foreach (JunkItemEntry entry in updated.Where(e => e.IsSelected))
        {
            entry.IsJunk = true;
        }

        PersistFromEntries(updated);
    }

    [RelayCommand]
    private void UnmarkAsJunk()
    {
        if (Items.Count == 0)
            return;

        var updated = Items.ToList();
        foreach (JunkItemEntry entry in updated.Where(e => e.IsSelected))
        {
            entry.IsJunk = false;
            entry.IsSelected = false;
        }

        PersistFromEntries(updated);
    }

    [RelayCommand]
    private void UnmarkAllJunk()
    {
        if (Items.Count == 0)
            return;

        var updated = Items.ToList();
        foreach (JunkItemEntry entry in updated)
        {
            entry.IsJunk = false;
            entry.IsSelected = false;
        }

        PersistFromEntries(updated);
    }

    [RelayCommand]
    private void SellAllJunk()
    {
        if (Items.Count == 0)
            return;

        if (TotalJunk == 0)
            return;

        if (!SkipSellWarning)
        {
            bool? confirm = _dialogService.ShowMessageBox(
                "This will sell all items marked as junk in your inventory. This cannot be undone. Continue?",
                "Sell All Junk",
                yesAndNo: true);

            if (confirm != true)
                return;
        }

        _junkService.SellAllJunk();
        Refresh();
    }

    partial void OnSkipSellWarningChanged(bool value)
    {
        // When enabling skip, double-check user intent once.
        if (value)
        {
            bool? confirm = _dialogService.ShowMessageBox(
                "Are you sure you want to skip the confirmation when selling all junk?",
                "Skip Sell Warning",
                yesAndNo: true);

            if (confirm != true)
            {
                _skipSellWarning = false;
                OnPropertyChanged(nameof(SkipSellWarning));
                return;
            }
        }

        _settings.Set("JunkSkipSellWarning", value);
    }

    private void PersistFromEntries(List<JunkItemEntry> entries)
    {
        var configs = entries
            .Where(e => e.IsJunk)
            .Select(e => new JunkItemConfig
            {
                ID = e.ID,
                Name = e.Name,
                Category = e.Category,
                Meta = string.Empty
            })
            .ToList();

        _junkService.SetJunk(configs);

        OnPropertyChanged(nameof(TotalJunk));
    }
}

public sealed partial class JunkItemEntry : ObservableObject
{
    public JunkItemEntry(int id, string name, string category, bool isJunk)
    {
        ID = id;
        Name = name;
        Category = category;
        _isJunk = isJunk;
    }

    public int ID { get; }
    public string Name { get; }
    public string Category { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isJunk;

    public string Display => $"[{ID}] {Name} ({Category})";
}
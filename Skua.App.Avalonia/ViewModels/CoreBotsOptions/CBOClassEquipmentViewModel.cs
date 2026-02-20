using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Interfaces;
using Skua.Core.Models.Items;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Skua.App.Avalonia.ViewModels.CoreBotsOptions;

public partial class CBOClassEquipmentViewModel : ObservableObject, IManageCBOptions
{
    public CBOClassEquipmentViewModel(IScriptInventory inventory)
    {
        _inventory = inventory;
    }

    private readonly IScriptInventory _inventory;

    public List<string> Helms { get; private set; } = new();
    public List<string> Armors { get; private set; } = new();
    public List<string> Capes { get; private set; } = new();
    public List<string> Weapons { get; private set; } = new();
    public List<string> Pets { get; private set; } = new();
    public List<string> GroundItems { get; private set; } = new();

    [ObservableProperty]
    private string? _selectedFarmHelm;

    [ObservableProperty]
    private string? _selectedFarmArmor;

    [ObservableProperty]
    private string? _selectedFarmCape;

    [ObservableProperty]
    private string? _selectedFarmWeapon;

    [ObservableProperty]
    private string? _selectedFarmPet;

    [ObservableProperty]
    private string? _selectedFarmGroundItem;

    [ObservableProperty]
    private string? _selectedSoloHelm;

    [ObservableProperty]
    private string? _selectedSoloArmor;

    [ObservableProperty]
    private string? _selectedSoloCape;

    [ObservableProperty]
    private string? _selectedSoloWeapon;

    [ObservableProperty]
    private string? _selectedSoloPet;

    [ObservableProperty]
    private string? _selectedSoloGroundItem;

    [ObservableProperty]
    private string? _selectedDodgeHelm;

    [ObservableProperty]
    private string? _selectedDodgeArmor;

    [ObservableProperty]
    private string? _selectedDodgeCape;

    [ObservableProperty]
    private string? _selectedDodgeWeapon;

    [ObservableProperty]
    private string? _selectedDodgePet;

    [ObservableProperty]
    private string? _selectedDodgeGroundItem;

    [ObservableProperty]
    private string? _selectedBossHelm;

    [ObservableProperty]
    private string? _selectedBossArmor;

    [ObservableProperty]
    private string? _selectedBossCape;

    [ObservableProperty]
    private string? _selectedBossWeapon;

    [ObservableProperty]
    private string? _selectedBossPet;

    [ObservableProperty]
    private string? _selectedBossGroundItem;

    [RelayCommand]
    private void RefreshInventory()
    {
        Helms = _inventory.Items?.Where(i => i.Category == ItemCategory.Helm && i.EnhancementLevel > 0).Select(i => i.Name).OrderBy(name => name).ToList() ?? new List<string>();
        Armors = _inventory.Items?.Where(i => i.Category == ItemCategory.Armor).Select(i => i.Name).OrderBy(name => name).ToList() ?? new List<string>();
        Capes = _inventory.Items?.Where(i => i.Category == ItemCategory.Cape && i.EnhancementLevel > 0).Select(i => i.Name).OrderBy(name => name).ToList() ?? new List<string>();
        Weapons = _inventory.Items?.Where(i => i.ItemGroup == "Weapon" && i.EnhancementLevel > 0).Select(i => i.Name).OrderBy(name => name).ToList() ?? new List<string>();
        Pets = _inventory.Items?.Where(i => i.Category == ItemCategory.Pet).Select(i => i.Name).OrderBy(name => name).ToList() ?? new List<string>();
        GroundItems = _inventory.Items?.Where(i => i.Category == ItemCategory.Misc).Select(i => i.Name).OrderBy(name => name).ToList() ?? new List<string>();

        OnPropertyChanged(nameof(Helms));
        OnPropertyChanged(nameof(Armors));
        OnPropertyChanged(nameof(Capes));
        OnPropertyChanged(nameof(Weapons));
        OnPropertyChanged(nameof(Pets));
        OnPropertyChanged(nameof(GroundItems));
    }

    public StringBuilder Save(StringBuilder builder)
    {
        builder.AppendLine($"Helm1Select: {SelectedSoloHelm}");
        builder.AppendLine($"Armor1Select: {SelectedSoloArmor}");
        builder.AppendLine($"Cape1Select: {SelectedSoloCape}");
        builder.AppendLine($"Weapon1Select: {SelectedSoloWeapon}");
        builder.AppendLine($"Pet1Select: {SelectedSoloPet}");
        builder.AppendLine($"GroundItem1Select: {SelectedSoloGroundItem}");

        builder.AppendLine($"Helm2Select: {SelectedFarmHelm}");
        builder.AppendLine($"Armor2Select: {SelectedFarmArmor}");
        builder.AppendLine($"Cape2Select: {SelectedFarmCape}");
        builder.AppendLine($"Weapon2Select: {SelectedFarmWeapon}");
        builder.AppendLine($"Pet2Select: {SelectedFarmPet}");
        builder.AppendLine($"GroundItem2Select: {SelectedFarmGroundItem}");

        builder.AppendLine($"Helm3Select: {SelectedDodgeHelm}");
        builder.AppendLine($"Armor3Select: {SelectedDodgeArmor}");
        builder.AppendLine($"Cape3Select: {SelectedDodgeCape}");
        builder.AppendLine($"Weapon3Select: {SelectedDodgeWeapon}");
        builder.AppendLine($"Pet3Select: {SelectedDodgePet}");
        builder.AppendLine($"GroundItem3Select: {SelectedDodgeGroundItem}");

        builder.AppendLine($"Helm4Select: {SelectedBossHelm}");
        builder.AppendLine($"Armor4Select: {SelectedBossArmor}");
        builder.AppendLine($"Cape4Select: {SelectedBossCape}");
        builder.AppendLine($"Weapon4Select: {SelectedBossWeapon}");
        builder.AppendLine($"Pet4Select: {SelectedBossPet}");
        builder.AppendLine($"GroundItem4Select: {SelectedBossGroundItem}");

        return builder;
    }

    public void SetValues(Dictionary<string, string> values)
    {
        if (!string.IsNullOrEmpty(SelectedSoloHelm = GetValue("Helm1Select")))
            Helms.Add(SelectedSoloHelm);
        if (!string.IsNullOrEmpty(SelectedSoloArmor = GetValue("Armor1Select")))
            Armors.Add(SelectedSoloArmor);
        if (!string.IsNullOrEmpty(SelectedSoloCape = GetValue("Cape1Select")))
            Capes.Add(SelectedSoloCape);
        if (!string.IsNullOrEmpty(SelectedSoloWeapon = GetValue("Weapon1Select")))
            Weapons.Add(SelectedSoloWeapon);
        if (!string.IsNullOrEmpty(SelectedSoloPet = GetValue("Pet1Select")))
            Pets.Add(SelectedSoloPet);
        if (!string.IsNullOrEmpty(SelectedSoloGroundItem = GetValue("GroundItem1Select")))
            GroundItems.Add(SelectedSoloGroundItem);

        if (!string.IsNullOrEmpty(SelectedFarmHelm = GetValue("Helm2Select")))
            Helms.Add(SelectedFarmHelm);
        if (!string.IsNullOrEmpty(SelectedFarmArmor = GetValue("Armor2Select")))
            Armors.Add(SelectedFarmArmor);
        if (!string.IsNullOrEmpty(SelectedFarmCape = GetValue("Cape2Select")))
            Capes.Add(SelectedFarmCape);
        if (!string.IsNullOrEmpty(SelectedFarmWeapon = GetValue("Weapon2Select")))
            Weapons.Add(SelectedFarmWeapon);
        if (!string.IsNullOrEmpty(SelectedFarmPet = GetValue("Pet2Select")))
            Pets.Add(SelectedFarmPet);
        if (!string.IsNullOrEmpty(SelectedFarmGroundItem = GetValue("GroundItem2Select")))
            GroundItems.Add(SelectedFarmGroundItem);

        if (!string.IsNullOrEmpty(SelectedDodgeHelm = GetValue("Helm3Select")))
            Helms.Add(SelectedDodgeHelm);
        if (!string.IsNullOrEmpty(SelectedDodgeArmor = GetValue("Armor3Select")))
            Armors.Add(SelectedDodgeArmor);
        if (!string.IsNullOrEmpty(SelectedDodgeCape = GetValue("Cape3Select")))
            Capes.Add(SelectedDodgeCape);
        if (!string.IsNullOrEmpty(SelectedDodgeWeapon = GetValue("Weapon3Select")))
            Weapons.Add(SelectedDodgeWeapon);
        if (!string.IsNullOrEmpty(SelectedDodgePet = GetValue("Pet3Select")))
            Pets.Add(SelectedDodgePet);
        if (!string.IsNullOrEmpty(SelectedDodgeGroundItem = GetValue("GroundItem3Select")))
            GroundItems.Add(SelectedDodgeGroundItem);

        if (!string.IsNullOrEmpty(SelectedBossHelm = GetValue("Helm4Select")))
            Helms.Add(SelectedBossHelm);
        if (!string.IsNullOrEmpty(SelectedBossArmor = GetValue("Armor4Select")))
            Armors.Add(SelectedBossArmor);
        if (!string.IsNullOrEmpty(SelectedBossCape = GetValue("Cape4Select")))
            Capes.Add(SelectedBossCape);
        if (!string.IsNullOrEmpty(SelectedBossWeapon = GetValue("Weapon4Select")))
            Weapons.Add(SelectedBossWeapon);
        if (!string.IsNullOrEmpty(SelectedBossPet = GetValue("Pet4Select")))
            Pets.Add(SelectedBossPet);
        if (!string.IsNullOrEmpty(SelectedBossGroundItem = GetValue("GroundItem4Select")))
            GroundItems.Add(SelectedBossGroundItem);

        Helms.Sort(System.StringComparer.OrdinalIgnoreCase);
        Armors.Sort(System.StringComparer.OrdinalIgnoreCase);
        Capes.Sort(System.StringComparer.OrdinalIgnoreCase);
        Weapons.Sort(System.StringComparer.OrdinalIgnoreCase);
        Pets.Sort(System.StringComparer.OrdinalIgnoreCase);
        GroundItems.Sort(System.StringComparer.OrdinalIgnoreCase);

        string GetValue(string key) => values.TryGetValue(key, out string? value) ? value : string.Empty;
    }
}
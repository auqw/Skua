using Skua.Core.Flash;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.Models.Items;
using Skua.Core.Models.Shops;

namespace Skua.Core.Scripts;

public class ScriptEnhancement : IScriptEnhancement
{
    private readonly Lazy<IFlashUtil> _lazyFlash;
    private readonly Lazy<IScriptPlayer> _lazyPlayer;
    private readonly Lazy<IScriptInventory> _lazyInventory;
    private readonly Lazy<IScriptBank> _lazyBank;
    private readonly Lazy<IScriptMap> _lazyMap;
    private readonly Lazy<IScriptShop> _lazyShops;
    private readonly Lazy<IScriptWait> _lazyWait;
    private readonly Lazy<IScriptQuest> _lazyQuests;
    private readonly Lazy<IScriptCombat> _lazyCombat;
    private readonly Lazy<IScriptOption> _lazyOptions;
    private readonly Lazy<IScriptSend> _lazySend;

    private IFlashUtil Flash => _lazyFlash.Value;
    private IScriptPlayer Player => _lazyPlayer.Value;
    private IScriptInventory Inventory => _lazyInventory.Value;
    private IScriptBank Bank => _lazyBank.Value;
    private IScriptMap Map => _lazyMap.Value;
    private IScriptShop Shops => _lazyShops.Value;
    private IScriptWait Wait => _lazyWait.Value;
    private IScriptQuest Quests => _lazyQuests.Value;
    private IScriptCombat Combat => _lazyCombat.Value;
    private IScriptOption Options => _lazyOptions.Value;
    private IScriptSend Send => _lazySend.Value;

    public ScriptEnhancement(
        Lazy<IFlashUtil> flash,
        Lazy<IScriptPlayer> player,
        Lazy<IScriptInventory> inventory,
        Lazy<IScriptBank> bank,
        Lazy<IScriptMap> map,
        Lazy<IScriptShop> shops,
        Lazy<IScriptWait> wait,
        Lazy<IScriptQuest> quests,
        Lazy<IScriptCombat> combat,
        Lazy<IScriptOption> options,
        Lazy<IScriptSend> send)
    {
        _lazyFlash = flash;
        _lazyPlayer = player;
        _lazyInventory = inventory;
        _lazyBank = bank;
        _lazyMap = map;
        _lazyShops = shops;
        _lazyWait = wait;
        _lazyQuests = quests;
        _lazyCombat = combat;
        _lazyOptions = options;
        _lazySend = send;
    }

    private static readonly ItemCategory[] EnhanceableCatagories =
    {
        ItemCategory.Sword, ItemCategory.Axe, ItemCategory.Dagger, ItemCategory.Gun,
        ItemCategory.HandGun, ItemCategory.Rifle, ItemCategory.Bow, ItemCategory.Mace,
        ItemCategory.Gauntlet, ItemCategory.Polearm, ItemCategory.Staff, ItemCategory.Wand,
        ItemCategory.Whip, ItemCategory.Class, ItemCategory.Helm, ItemCategory.Cape,
    };

    private readonly ItemCategory[] WeaponCatagories = EnhanceableCatagories[..12];

    #region Public API

    public void SmartEnhance(string? className, bool forceEnhance = false)
    {
        if (string.IsNullOrEmpty(className))
        {
            Log("SmartEnhance: className is null");
            return;
        }

        if (!Inventory.Contains(className) && !Bank.Contains(className))
        {
            Log($"SmartEnhance Failed: Class {className} was not found in inventory");
            return;
        }

        if (Player.InCombat)
            JumpWait();

        className = className.Trim().ToLower();
        InventoryItem? selectedClass = Inventory.Items.Find(i =>
            i.Name.ToLower().Trim() == className && i.Category == ItemCategory.Class
        );
        if (selectedClass == null)
        {
            Log($"SmartEnhance Failed: Class {className} was not found in inventory");
            return;
        }

        className = selectedClass.Name.ToLower();
        EnhancementType? type = null;
        CapeSpecial cSpecial = CapeSpecial.None;
        HelmSpecial hSpecial = HelmSpecial.None;
        WeaponSpecial wSpecial = WeaponSpecial.None;

        if (!ForgeEnhancementLibrary(ref type, ref cSpecial, ref hSpecial, ref wSpecial, className))
            AweEnhancementLibrary(ref type, ref cSpecial, ref hSpecial, ref wSpecial, className);

        if (type == null)
        {
            Log($"SmartEnhance Failed: enhancement type for {className} is NULL");
            return;
        }

        if (selectedClass.EnhancementLevel <= 0)
        {
            EnhanceItem(selectedClass.Name, (EnhancementType)type, cSpecial, hSpecial, wSpecial);
            if (forceEnhance)
                return;
        }

        EquipClass(selectedClass.Name);
        Wait.ForTrue(() => Player.CurrentClass?.Name?.ToLower() == className, 40);
        EnhanceEquipped((EnhancementType)type, cSpecial, hSpecial, wSpecial);
    }

    public void EnhanceEquipped(
        EnhancementType type,
        CapeSpecial cSpecial = CapeSpecial.None,
        HelmSpecial hSpecial = HelmSpecial.None,
        WeaponSpecial wSpecial = WeaponSpecial.None)
    {
        List<InventoryItem> equippedItems = Inventory.Items.FindAll(i =>
            i.Equipped && EnhanceableCatagories.Contains(i.Category));
        AutoEnhance(equippedItems, type, cSpecial, hSpecial, wSpecial);
    }

    public void EnhanceItem(
        string item,
        EnhancementType type,
        CapeSpecial cSpecial = CapeSpecial.None,
        HelmSpecial hSpecial = HelmSpecial.None,
        WeaponSpecial wSpecial = WeaponSpecial.None,
        bool logging = false)
    {
        InventoryItem? target = Inventory.Items.Find(i =>
            i.Name.Equals(item, StringComparison.OrdinalIgnoreCase)
            && EnhanceableCatagories.Contains(i.Category));
        if (target == null)
        {
            Log($"EnhanceItem Failed: \"{item}\" not found");
            return;
        }
        AutoEnhance(new List<InventoryItem> { target }, type, cSpecial, hSpecial, wSpecial, logging);
    }

    public EnhancementType CurrentClassEnh()
    {
        InventoryItem? cape = Inventory.Items.Find(i => i.Category == ItemCategory.Cape && i.Equipped);
        if (cape == null) return 0;

        int pattern = cape.EnhancementPatternID;
        if (Enum.IsDefined(typeof(EnhancementType), pattern))
            return (EnhancementType)pattern;

        InventoryItem? helm = Inventory.Items.Find(i => i.Category == ItemCategory.Helm && i.Equipped);
        if (helm == null) return 0;

        pattern = helm.EnhancementPatternID;
        if (Enum.IsDefined(typeof(EnhancementType), pattern))
            return (EnhancementType)pattern;

        InventoryItem? weapon = Inventory.Items.Find(i => i.ItemGroup == "Weapon" && i.Equipped);
        if (weapon == null) return 0;

        pattern = weapon.EnhancementPatternID;
        if (Enum.IsDefined(typeof(EnhancementType), pattern))
            return (EnhancementType)pattern;

        return 0;
    }

    public CapeSpecial CurrentCapeSpecial()
    {
        InventoryItem? cape = Inventory.Items.Find(i => i.Category == ItemCategory.Cape && i.Equipped);
        if (cape == null) return CapeSpecial.None;
        int pattern = cape.EnhancementPatternID;
        if (Enum.IsDefined(typeof(CapeSpecial), pattern))
            return (CapeSpecial)pattern;
        return CapeSpecial.None;
    }

    public HelmSpecial CurrentHelmSpecial()
    {
        InventoryItem? helm = Inventory.Items.Find(i => i.Category == ItemCategory.Helm && i.Equipped);
        if (helm == null) return HelmSpecial.None;
        int pattern = helm.EnhancementPatternID;
        if (Enum.IsDefined(typeof(HelmSpecial), pattern))
            return (HelmSpecial)pattern;
        return HelmSpecial.None;
    }

    public WeaponSpecial CurrentWeaponSpecial()
    {
        InventoryItem? weapon = Inventory.Items.Find(i => i.ItemGroup == "Weapon" && i.Equipped);
        if (weapon == null) return WeaponSpecial.None;
        int proc = GetProcID(weapon);
        if (Enum.IsDefined(typeof(WeaponSpecial), proc))
            return (WeaponSpecial)proc;
        return WeaponSpecial.None;
    }

    public bool IsAweUnlocked() => IsQuestCompleted(2937);

    #endregion

    #region SmartEnhance Library

    private bool ForgeEnhancementLibrary(
        ref EnhancementType? type,
        ref CapeSpecial cSpecial,
        ref HelmSpecial hSpecial,
        ref WeaponSpecial wSpecial,
        string className)
    {
        switch (className.ToLower())
        {
            #region Lucky Region

            #region Lucky - Vim - vainglory - lacerate
            case "horc evader":
                if (!uLacerate() || !uVim() || !uVainglory())
                    return false;

                type = EnhancementType.Thief;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Lacerate;
                hSpecial = HelmSpecial.Vim;
                break;
            #endregion

            #region Luck - Awe_Blast | Arcanas_Concerto - ForgeHelm - Penitence
            case "lord of order":
                if (!uAwe() || !uForgeHelm() || !uPenitence())
                    return false;

                type = EnhancementType.Lucky;
                wSpecial = uArcanasConcerto()
                    ? WeaponSpecial.Arcanas_Concerto
                    : WeaponSpecial.Awe_Blast;
                hSpecial = HelmSpecial.Forge;
                cSpecial = CapeSpecial.Penitence;
                break;
            #endregion

            #region Lucky - Dauntless - Vim - Lament
            case "great thief":
                if (!uDauntless() || !uVim() || !uLament())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Lament;
                wSpecial = WeaponSpecial.Dauntless;
                hSpecial = HelmSpecial.Vim;
                break;
            #endregion

            #region Lucky - Lacerate - Vim - Lament
            case "timekeeper":
            case "timekiller":
                if (!uLacerate() || !uVim() || !uLament())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Lament;
                wSpecial = WeaponSpecial.Lacerate;
                hSpecial = HelmSpecial.Vim;
                break;
            #endregion

            #region Lucky - Forge - Spiral Carve
            case "corrupted chronomancer":
            case "underworld chronomancer":
            case "eternal chronomancer":
            case "immortal chronomancer":
            case "dark metal necro":
                if (!uForgeCape())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Forge;
                wSpecial = WeaponSpecial.Spiral_Carve;
                break;
            #endregion

            #region Lucky - Forge - Awe Blast
            case "glacial berserker":
                if (!uForgeCape())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Forge;
                wSpecial = WeaponSpecial.Awe_Blast;
                break;
            #endregion

            #region Lucky - Forge - Mana Vamp
            case "legendary elemental warrior":
            case "mythic elemental warrior":
            case "ultra elemental warrior":
                if (!uForgeCape())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Forge;
                wSpecial = WeaponSpecial.Mana_Vamp;
                break;
            #endregion

            #region Lucky - Forge - Smite
            case "draconic chronomancer":
                if (!uSmite() || !uForgeCape())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Forge;
                wSpecial = WeaponSpecial.Smite;
                break;
            #endregion

            #region Lucky - Forge - Elysium
            case "ultra omniknight":
            case "dark ultra omninight":
                if (!uElysium() || !uForgeCape())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Forge;
                wSpecial = WeaponSpecial.Elysium;
                break;
            #endregion

            #region Lucky - Vainglory - Valiance - Anima
            case "archfiend":
            case "eternal inversionist":
            case "dragonlord":
                if (!uVainglory() || !uValiance() || !uAnima())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Anima;
                break;
            #endregion

            #region Lucky - Vainglory - Valiance/Ravenous/Dauntless - Anima
            case "continuum chronomancer":
            case "quantum chronomancer":
                if (!uVainglory() || !uAnima())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = uRavenous() ? WeaponSpecial.Ravenous
                    : uDauntless() ? WeaponSpecial.Dauntless
                    : WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Anima;
                break;
            #endregion

            #region Lucky - Vainglory - Dauntless - Anima
            case "chaos avenger":
                if (!uAnima() || !uVainglory())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = uDauntless() ? WeaponSpecial.Dauntless : WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Anima;
                break;
            #endregion

            #region Lucky - Lacerate - Forge - Lament
            case "doom metal necro":
            case "neo metal necro":
                if (!uLacerate() || !uForgeHelm() || !uLament())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = uLament() ? CapeSpecial.Lament : CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Lacerate;
                hSpecial = HelmSpecial.Forge;
                break;
            #endregion

            #region Lucky - Vainglory - Dauntless|Valiance|Smite - Vim
            case "martial artist":
            case "master martial artist":
                if ((!uDauntless() && !uValiance() && !uSmite()) || !uVainglory() || !uVim())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = uDauntless() ? WeaponSpecial.Dauntless
                    : uValiance() ? WeaponSpecial.Valiance
                    : WeaponSpecial.Smite;
                hSpecial = HelmSpecial.Vim;
                break;
            #endregion

            #region Lucky - Vainglory - Praxis - Vim
            case "yami no ronin":
                if (!uPraxis() || !uVainglory() || !uVim())
                    return false;
                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Praxis;
                hSpecial = HelmSpecial.Vim;
                break;
            #endregion

            #region Lucky - Vainglory - Valiance - Anima
            case "nechronomancer":
            case "necrotic chronomancer":
                if (!uVainglory() || !uArcanasConcerto() || !uAnima())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Anima;
                break;
            #endregion

            #region Lucky - Vainglory - Elysium - Vim
            case "shadowwalker of time":
            case "shadowstalker of time":
            case "shadowweaver of time":
                if (!uVainglory() || !uElysium() || !uVim())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Elysium;
                hSpecial = HelmSpecial.Vim;
                break;
            #endregion

            #region Lucky - Vainglory - Valiance - None
            case "legion doomknight":
                if (!uVainglory() || !uValiance())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Valiance;
                hSpecial = CurrentHelmSpecial();
                break;
            #endregion

            #region Lucky - Vainglory - Elysium - Pneuma
            case "antique hunter":
            case "artifact hunter":
                if (!uVainglory() || !uElysium() || !uPneuma())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Elysium;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Lucky - Lament - Elysium - Pneuma
            case "abyssal angel":
            case "abyssal angel's shadow":
                if (!uLament() || !uElysium() || !uPneuma())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Elysium;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Lucky - Dauntless | Ravenous - Anima | ForgeHelm - Vainglory
            case "verus doomknight":
                if (!uRavenous() || !uForgeHelm() || !uVainglory())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = uDauntless() ? WeaponSpecial.Dauntless : WeaponSpecial.Ravenous;
                hSpecial = uAnima() ? HelmSpecial.Anima : HelmSpecial.Forge;
                break;
            #endregion

            #region Lucky - Vainglory - Dauntless/Valiance - Anima
            case "debris highlord":
            case "void highlord":
            case "void highlord (ioda)":
                if (!uAnima() || !uValiance() || !uVainglory())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = !uDauntless()
                    ? (uRavenous() ? WeaponSpecial.Ravenous : (uValiance() ? WeaponSpecial.Valiance : WeaponSpecial.Forge))
                    : WeaponSpecial.Dauntless;
                hSpecial = HelmSpecial.Anima;
                break;
            #endregion

            #region Lucky - Avarice - Dauntless - Anima
            case "flame dragon warrior":
                if (!uAvarice() || !uDauntless() || !uAnima())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Avarice;
                wSpecial = WeaponSpecial.Dauntless;
                hSpecial = HelmSpecial.Anima;
                break;
            #endregion

            #region Lucky - Avarice - Elysium - Anima
            case "chaos slayer":
            case "chaos slayer berserker":
            case "chaos slayer cleric":
            case "chaos slayer mystic":
            case "chaos slayer thief":
                if (!uAvarice() || !uElysium() || !uAnima())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Avarice;
                wSpecial = WeaponSpecial.Elysium;
                hSpecial = HelmSpecial.Anima;
                break;
            #endregion

            #region Lucky - Penitence - Ravenous | Praxis | Lacerate - Forge | None
            case "archpaladin":
                if (!uLacerate() || !uForgeHelm() || !uPenitence())
                    return false;

                type = EnhancementType.Lucky;
                wSpecial = uValiance() ? WeaponSpecial.Valiance
                    : (uPraxis() ? WeaponSpecial.Praxis : WeaponSpecial.Lacerate);
                hSpecial = uForgeHelm() ? HelmSpecial.Forge : HelmSpecial.None;
                cSpecial = CapeSpecial.Penitence;
                break;
            #endregion

            #region Fighter - Ravenous | Valiance - Anima - Absolution
            case "stonecrusher":
                if (!uValiance() || !uAnima() || !uAbsolution())
                    return false;

                type = EnhancementType.Fighter;
                wSpecial = WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Anima;
                cSpecial = CapeSpecial.Absolution;
                break;
            #endregion

            #endregion

            #region Wizard Region

            #region Wizard - Valiance|Praxis - Pneuna - Vainglory|Lament
            case "lightcaster":
                if (!uValiance() || !uPneuma() || !uVainglory())
                {
                    if (!uLament() || !uPraxis())
                        return false;
                }
                type = EnhancementType.Wizard;
                cSpecial = !uVainglory() ? CapeSpecial.Lament : CapeSpecial.Vainglory;
                wSpecial = !uValiance() ? WeaponSpecial.Praxis : WeaponSpecial.Valiance;
                hSpecial = !uPneuma() ? CurrentHelmSpecial() : HelmSpecial.Pneuma;
                break;
            #endregion

            case "archivist of time":
                if (!uValiance() || !uPneuma() || !uVainglory())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Pneuma;
                break;

            #region Wizard - Forge - Awe Blast
            case "infinity knight":
                if (!uForgeCape())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Forge;
                wSpecial = WeaponSpecial.Awe_Blast;
                hSpecial = CurrentHelmSpecial();
                break;
            #endregion

            #region Wizard - Vainglory - Valiance - Pneuma
            case "archmage":
            case "darklord":
            case "arcana invoker":
                if (!uVainglory() || !uValiance() || !uPneuma())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = uRavenous() ? WeaponSpecial.Ravenous : WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Wizard - Penitence - Acheron - Pneuma
            case "master of moglins":
            case "dark master of moglins":
                if (!uPenitence() || !uAcheron() || !uPneuma())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Penitence;
                wSpecial = WeaponSpecial.Acheron;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Wizard - Vainglory - Ravenous | Valiance - Pneuma
            case "legion revenant":
            case "legion revenant (ioda)":
                if (!uVainglory() || !uValiance() || !uPneuma())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = uRavenous() ? WeaponSpecial.Ravenous : WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Wizard - Avarice - Elysium - Pneuma
            case "vampire lord":
            case "enchanted vampire lord":
            case "royal vampire lord":
            case "darkside":
            case "dark lord":
                if (!uAvarice() || !uElysium() || !uPneuma())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Avarice;
                wSpecial = WeaponSpecial.Elysium;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Wizard - Vainglory - Elysium - Pneuma
            case "shaman":
                if (!uVainglory() || !uElysium() || !uPneuma())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Elysium;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Wizard - Avarice - Acheron - Pneuma
            case "blaze binder":
                if (!uAvarice() || !uAcheron() || !uPneuma())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Avarice;
                wSpecial = WeaponSpecial.Acheron;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Wizard - Lament - Elysium - Pneuma
            case "royal battlemage":
                if (!uLament() || !uElysium() || !uPneuma())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Lament;
                wSpecial = WeaponSpecial.Elysium;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Wizard - Lament - Valiance - Pneuma
            case "scarlet sorceress":
                if (!uLament() || !uValiance() || !uPneuma())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Lament;
                wSpecial = WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Wizard - Vainglory / Forge - Daunt / Ravenous / Forge - Pneuma / Forge
            case "sovereign of storms":
                if (!uVainglory() || !uDauntless() || !uRavenous() || !uPneuma())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = uVainglory() ? CapeSpecial.Vainglory : CapeSpecial.Forge;
                wSpecial = uDauntless() ? WeaponSpecial.Dauntless
                    : (uRavenous() ? WeaponSpecial.Ravenous : WeaponSpecial.Forge);
                hSpecial = uPneuma() ? HelmSpecial.Pneuma : HelmSpecial.Forge;
                break;
            #endregion

            #region Wizard - Ravenous - Lament - Examen
            case "lich":
                if (!(uRavenous() && uLament() && uExamen()))
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Lament;
                wSpecial = WeaponSpecial.Ravenous;
                hSpecial = HelmSpecial.Examen;
                break;
            #endregion

            #endregion

            #region Healer Region

            #region Healer - Avarice - Elysium - Pneuma
            case "dragon of time":
                if (!uAvarice() || !uElysium() || !uPneuma())
                    return false;

                type = EnhancementType.Healer;
                cSpecial = CapeSpecial.Avarice;
                wSpecial = WeaponSpecial.Elysium;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Healer - None - Valiance - None
            case "obsidian paladin chronomancer":
            case "paladin chronomancer":
                if (!uValiance())
                    return false;

                type = EnhancementType.Healer;
                cSpecial = CapeSpecial.None;
                wSpecial = WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.None;
                break;
            #endregion

            #region Fighter - Ravenous | Valiance - Anima - Absolution
            case "frostval barbarian":
                if (!uAbsolution() || !uValiance() || !uAnima())
                    return false;
                type = EnhancementType.Fighter;
                cSpecial = CapeSpecial.Absolution;
                wSpecial = uRavenous() ? WeaponSpecial.Ravenous : WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Anima;
                break;
            #endregion

            #endregion

            #region Lucky - Penitence | Absolution - Elysium | Valiance - Vim
            case "arachnomancer":
                if (!uAbsolution() || !uAbsolution() || !uVim())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = uPenitence() ? CapeSpecial.Penitence : CapeSpecial.Absolution;
                wSpecial = uElysium() ? WeaponSpecial.Elysium : WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Vim;
                break;
            #endregion

            #region Wizard - Elysium/Ravenous - Examen - Lament
            case "phantom chronomancer":
            case "phantasm chronomancer":
                if (!uElysium() && !uRavenous())
                    return false;
                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Lament;
                wSpecial = uElysium() ? WeaponSpecial.Elysium : WeaponSpecial.Ravenous;
                hSpecial = HelmSpecial.Examen;
                break;
            #endregion

            case "scion of flames":
                if ((!uVainglory() || !uLament())
                && (!uPneuma() || !uForgeHelm())
                && (!uRavenous() || !uValiance()))
                    return false;
                type = EnhancementType.Wizard;
                cSpecial = uVainglory() ? CapeSpecial.Vainglory : CapeSpecial.Lament;
                wSpecial = uRavenous() ? WeaponSpecial.Ravenous : WeaponSpecial.Valiance;
                hSpecial = uPneuma() ? HelmSpecial.Pneuma : HelmSpecial.Forge;
                break;

            #region Healer - Current - Valiance/Awe - Current
            case "healer":
            case "healer (rare)":
                type = EnhancementType.Healer;
                cSpecial = CurrentCapeSpecial();
                wSpecial = uValiance() ? WeaponSpecial.Valiance : WeaponSpecial.Awe_Blast;
                hSpecial = CurrentHelmSpecial();
                break;
            #endregion

            #region Lucky - Vim - Lam - Rav
            case "chrono shadowslayer":
            case "chrono shadowhunter":
                type = EnhancementType.Lucky;
                cSpecial = uLament() ? CapeSpecial.Lament
                    : (uForgeCape() ? CapeSpecial.Forge : CurrentCapeSpecial());
                wSpecial = uRavenous() ? WeaponSpecial.Ravenous
                    : (uArcanasConcerto() ? WeaponSpecial.Arcanas_Concerto
                    : (uForgeWeapon() ? WeaponSpecial.Forge : WeaponSpecial.Awe_Blast));
                hSpecial = uVim() ? HelmSpecial.Vim
                    : (uForgeHelm() ? HelmSpecial.Forge : CurrentHelmSpecial());
                break;
            #endregion

            #region Lucky - Vainglory - Valiance / Dauntless - Anima
            case "glacial warlord":
            case "glaceran warlord":
            case "dark glaceran warlord":
            case "savage glaceran warlord":
                if (!uVainglory() || !uValiance() || !uAnima())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = uDauntless() ? WeaponSpecial.Dauntless : WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Anima;
                break;
            #endregion

            #region King's Echo - Healer - Elysium/Mana - Examen - Lament/Vainglory
            case "king's echo":
                if (!uValiance() || !uExamen() || !uVainglory())
                    return false;

                type = EnhancementType.Healer;
                cSpecial = uLament() ? CapeSpecial.Lament : CapeSpecial.Vainglory;
                wSpecial = uElysium() ? WeaponSpecial.Elysium : WeaponSpecial.Mana_Vamp;
                hSpecial = HelmSpecial.Examen;
                break;
            #endregion

            #region Lucky - Val/Smite/Mana - Anima - Vg
            case "dragonslayer general":
                type = EnhancementType.Lucky;
                cSpecial = uVainglory() ? CapeSpecial.Vainglory
                    : uForgeCape() ? CapeSpecial.Forge
                    : CurrentCapeSpecial();
                wSpecial = uValiance() ? WeaponSpecial.Valiance
                    : uSmite() ? WeaponSpecial.Smite
                    : WeaponSpecial.Mana_Vamp;
                hSpecial = uAnima() ? HelmSpecial.Anima
                    : uForgeHelm() ? HelmSpecial.Forge
                    : CurrentHelmSpecial();
                break;
            #endregion

            #region Lucky - Dauntless | Ravenous - Anima - Vainglory
            case "chrono chaorruptor":
                if (!uRavenous() || !uAnima() || !uVainglory())
                    return false;

                type = EnhancementType.Lucky;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = uDauntless() ? WeaponSpecial.Dauntless : WeaponSpecial.Ravenous;
                hSpecial = HelmSpecial.Anima;
                break;
            #endregion

            #region Wizard - Ravenous - Pneuma - Vainglory
            case "chrono dataknight":
            case "chrono dragonknight":
                if (!uRavenous() || !uPneuma() || !uVainglory())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Vainglory;
                wSpecial = WeaponSpecial.Ravenous;
                hSpecial = HelmSpecial.Pneuma;
                break;
            #endregion

            #region Wizard - Ravenous | Valiance - ForgeHelm - Absolution
            case "legendary hero":
                if (!uValiance() || !uForgeHelm() || !uAbsolution())
                    return false;

                type = EnhancementType.Wizard;
                cSpecial = CapeSpecial.Absolution;
                wSpecial = uRavenous() ? WeaponSpecial.Ravenous : WeaponSpecial.Valiance;
                hSpecial = HelmSpecial.Forge;
                break;
            #endregion

            #region Unassigned - default to Awe
            default:
                type = EnhancementType.Lucky;
                return false;
            #endregion
        }
        return true;
    }

    private void AweEnhancementLibrary(
        ref EnhancementType? type,
        ref CapeSpecial cSpecial,
        ref HelmSpecial hSpecial,
        ref WeaponSpecial wSpecial,
        string className)
    {
        switch (className.ToLower())
        {
            #region Lucky Region

            #region Lucky - Spiral Carve
            case "abyssal angel":
            case "abyssal angel's shadow":
            case "artifact hunter":
            case "assassin":
            case "archmage":
            case "beastmaster":
            case "berserker":
            case "beta berserker":
            case "blademaster assassin":
            case "blademaster":
            case "blood titan":
            case "frostblood titan":
            case "cardclasher":
            case "chaos avenger member preview":
            case "chaos champion prime":
            case "chaos slayer":
            case "chaos slayer berserker":
            case "chaos slayer cleric":
            case "chaos slayer mystic":
            case "chaos slayer thief":
            case "chrono chaorruptor":
            case "chrono commandant":
            case "chronocommander":
            case "chronocorrupter":
            case "chunin":
            case "classic alpha pirate":
            case "classic barber":
            case "classic doomknight":
            case "classic exalted soul cleaver":
            case "classic guardian":
            case "classic paladin":
            case "classic pirate":
            case "classic soul cleaver":
            case "continuum chronomancer":
            case "corrupted chronomancer":
            case "dark chaos berserker":
            case "dark harbinger":
            case "doomknight":
            case "empyrean chronomancer":
            case "eternal chronomancer":
            case "evolved clawsuit":
            case "evolved dark caster":
            case "evolved leprechaun":
            case "exalted harbinger":
            case "exalted soul cleaver":
            case "glaceran warlord":
            case "dark glaceran warlord":
            case "savage glaceran warlord":
            case "glacial warlord":
            case "great thief":
            case "immortal chronomancer":
            case "imperial chunin":
            case "infinite dark caster":
            case "infinite legion dark caster":
            case "infinity titan":
            case "legion blademaster assassin":
            case "legion evolved dark caster":
            case "legion swordmaster assassin":
            case "leprechaun":
            case "lycan":
            case "master ranger":
            case "mechajouster":
            case "necromancer":
            case "ninja warrior":
            case "not a mod":
            case "overworld chronomancer":
            case "pinkomancer":
            case "prismatic clawsuit":
            case "quantum chronomancer":
            case "ranger":
            case "renegade":
            case "rogue":
            case "classic rogue":
            case "rogue (rare)":
            case "scarlet sorceress":
            case "shadowscythe general":
            case "skycharged grenadier":
            case "skyguard grenadier":
            case "sovereign of storms":
            case "soul cleaver":
            case "starlord":
            case "swordmaster assassin":
            case "swordmaster":
            case "timekeeper":
            case "timekiller":
            case "timeless chronomancer":
            case "undead leperchaun":
            case "undeadslayer":
            case "underworld chronomancer":
            case "unlucky leperchaun":
            case "debris highlord":
            case "void highlord":
            case "void highlord (ioda)":
            case "verus doomknight":
                type = EnhancementType.Lucky;
                wSpecial = WeaponSpecial.Spiral_Carve;
                break;
            #endregion

            #region Lucky - Mana Vamp
            case "alpha doommega":
            case "alpha omega":
            case "alpha pirate":
            case "beast warrior":
            case "blood ancient":
            case "chaos avenger":
            case "chaos shaper":
            case "classic defender":
            case "clawsuit":
            case "cryomancer mini pet coming soon":
            case "dark legendary hero":
            case "dragonsoul shinobi":
            case "ultra omniknight":
            case "dark ultra omninight":
            case "doomknight overlord":
            case "dragonslayer general":
            case "drakel warlord":
            case "glacial berserker test":
            case "heroic naval commander":
            case "legendary elemental warrior":
            case "mythic elemental warrior":
            case "legendary naval commander":
            case "legion revenant member test":
            case "naval commander":
            case "paladin high lord":
            case "paladin":
            case "paladinslayer":
            case "pirate":
            case "pumpkin lord":
            case "shadowflame dragonlord":
            case "shadowstalker of time":
            case "shadowwalker of time":
            case "shadowweaver of time":
            case "silver paladin":
            case "thief of hours":
            case "ultra elemental warrior":
            case "void highlord tester":
            case "warlord":
            case "warrior":
            case "warrior (rare)":
            case "warriorscythe general":
            case "yami no ronin":
            case "arachnomancer":
                type = EnhancementType.Lucky;
                wSpecial = WeaponSpecial.Mana_Vamp;
                break;
            #endregion

            #region Lucky - Awe Blast
            case "archpaladin":
            case "bard":
            case "chrono assassin":
            case "chronomancer":
            case "chronomancer prime":
            case "dark metal necro":
            case "deathknight lord":
            case "dragon shinobi":
            case "dragonlord":
            case "evolved pumpkin lord":
            case "glacial berserker":
            case "grunge rocker":
            case "guardian":
            case "heavy metal necro":
            case "heavy metal rockstar":
            case "hobo highlord":
            case "lord of order":
            case "legendary hero":
            case "nechronomancer":
            case "necrotic chronomancer":
            case "draconic chronomancer":
            case "no class":
            case "nu metal necro":
            case "obsidian no class":
            case "protosartorium":
            case "shadow dragon shinobi":
            case "shadow ripper":
            case "shadow rocker":
            case "star captain":
            case "troubador of love":
            case "unchained rocker":
            case "unchained rockstar":
            case "undead goat":
            case "unundead goat":
            case "doom metal necro":
            case "neo metal necro":
            case "martial artist":
            case "master martial artist":
            case "antique hunter":
            case "archivist of time":
                type = EnhancementType.Lucky;
                wSpecial = WeaponSpecial.Awe_Blast;
                break;
            #endregion

            #region Lucky - Health Vamp
            case "eternal inversionist":
            case "archfiend":
            case "barber":
            case "classic dragonlord":
            case "dragonslayer":
            case "enforcer":
            case "flame dragon warrior":
            case "rustbucket":
            case "sentinel":
            case "vampire":
            case "vampire lord":
            case "enchanted vampire lord":
            case "royal vampire lord":
            case "chrono shadowhunter":
                type = EnhancementType.Lucky;
                wSpecial = WeaponSpecial.Health_Vamp;
                break;
            #endregion

            #endregion

            #region Thief Region

            #region Thief - Mana Vamp
            case "ninja":
            case "classic ninja":
            case "ninja (rare)":
            case "horc evader":
                type = EnhancementType.Thief;
                wSpecial = WeaponSpecial.Mana_Vamp;
                break;
            #endregion

            #endregion

            #region Wizard Region

            #region Wizard - Awe Blast
            case "acolyte":
            case "arcane dark caster":
            case "battlemage":
            case "battlemage of love":
            case "blaze binder":
            case "blood sorceress":
            case "dark battlemage":
            case "dragon knight":
            case "firelord summoner":
            case "grim necromancer":
            case "highseas commander":
            case "infinity knight":
            case "interstellar knight":
            case "master of moglins":
            case "dark master of moglins":
            case "lich":
            case "mystical dark caster":
            case "northlands monk":
            case "royal battlemage":
            case "timeless dark caster":
            case "witch":
            case "stonecrusher":
            case "scion of flames":
                type = EnhancementType.Wizard;
                wSpecial = WeaponSpecial.Awe_Blast;
                break;
            #endregion

            #region Wizard - Spiral Carve
            case "chrono dataknight":
            case "chrono dragonknight":
            case "cryomancer":
            case "dark caster":
            case "dark cryomancer":
            case "darkblood stormking":
            case "darkside":
            case "defender":
            case "frost spiritreaver":
            case "immortal dark caster":
            case "legion paladin":
            case "legion revenant":
            case "legion revenant (ioda)":
            case "lightcaster":
            case "pink romancer":
            case "psionic mindbreaker":
            case "pyromancer":
            case "sakura cryomancer":
            case "troll spellsmith":
            case "classic legion doomknight":
            case "legion doomknight":
            case "legion doomknight tester":
            case "arcana invoker":
            case "king's echo":
                type = EnhancementType.Wizard;
                wSpecial = WeaponSpecial.Spiral_Carve;
                break;
            #endregion

            #region Wizard - Health Vamp
            case "daimon":
            case "dark lord":
            case "evolved shaman":
            case "lightmage":
            case "mindbreaker":
            case "vindicator of they":
            case "elemental dracomancer":
            case "lightcaster test":
            case "love caster":
            case "mage":
            case "classic mage":
            case "mage (rare)":
            case "sorcerer":
            case "the collector":
                type = EnhancementType.Wizard;
                wSpecial = WeaponSpecial.Health_Vamp;
                break;
            #endregion

            #region Wizard - Mana Vamp
            case "oracle":
            case "shaman":
                type = EnhancementType.Wizard;
                wSpecial = WeaponSpecial.Mana_Vamp;
                break;
            #endregion

            #endregion

            #region Fighter Region

            #region Fighter - Awe Blast
            case "deathknight":
            case "frostval barbarian":
                type = EnhancementType.Fighter;
                wSpecial = WeaponSpecial.Awe_Blast;
                break;
            #endregion

            #endregion

            #region Healer Region

            #region Healer - Health Vamp
            case "dragon of time":
                type = EnhancementType.Healer;
                wSpecial = WeaponSpecial.Health_Vamp;
                break;
            #endregion

            #region Healer - Mana Vamp
            case "obsidian paladin chronomancer":
            case "paladin chronomancer":
                type = EnhancementType.Healer;
                wSpecial = WeaponSpecial.Mana_Vamp;
                break;
            #endregion

            #endregion

            default:
                Log($"SmartEnhance Failed: \"{className}\" is not found in the Smart Enhance Library");
                return;
        }
    }

    #endregion

    #region AutoEnhance

    private void AutoEnhance(
        List<InventoryItem> itemList,
        EnhancementType type,
        CapeSpecial cSpecial,
        HelmSpecial hSpecial,
        WeaponSpecial wSpecial,
        bool logging = false)
    {
        if ((int)type == 0)
            return;

        if (itemList.Count == 0)
        {
            Log("AutoEnhance: ItemList is empty");
            return;
        }

        InventoryItem? cape = null;
        if (cSpecial != CapeSpecial.None && itemList.Any(i => i.Category == ItemCategory.Cape))
        {
            cape = itemList.Find(i => i.Category == ItemCategory.Cape);
            if (cape != null) itemList.Remove(cape);
        }

        InventoryItem? helm = null;
        if (hSpecial != HelmSpecial.None && itemList.Any(i => i.Category == ItemCategory.Helm))
        {
            helm = itemList.Find(i => i.Category == ItemCategory.Helm);
            if (helm != null) itemList.Remove(helm);
        }

        InventoryItem? weapon = null;
        if (wSpecial != WeaponSpecial.None
            && itemList.Any(i => i.ItemGroup == "Weapon")
            && (IsQuestCompleted(2937) || wSpecial == WeaponSpecial.Forge || (int)wSpecial > 6))
        {
            weapon = itemList.Find(i => i.ItemGroup == "Weapon");
            if (weapon != null) itemList.Remove(weapon);
        }

        int skipCounter = 0;

        if (itemList.Count > 0)
        {
            int shopID = GetEnhancementShopID(type);
            if (shopID == 0) return;

            foreach (InventoryItem item in itemList)
            {
                if (AlreadyHasRequestedEnhancement(item, type, cSpecial, hSpecial, wSpecial))
                {
                    skipCounter++;
                    continue;
                }
                _AutoEnhance(item, shopID, Map.Name, logging);
                Sleep(700);
            }
        }

        // Cape
        if (cape != null)
        {
            if (AlreadyHasRequestedEnhancement(cape, type, cSpecial, hSpecial, wSpecial))
                skipCounter++;
            else if (CanEnhanceCape(cSpecial))
                _AutoEnhance(cape, 2143, "forge", logging);
            else
                skipCounter++;
        }

        // Helm
        if (helm != null)
        {
            if (AlreadyHasRequestedEnhancement(helm, type, cSpecial, hSpecial, wSpecial))
                skipCounter++;
            else if (CanEnhanceHelm(hSpecial))
                _AutoEnhance(helm, 2164, "forge");
            else
                skipCounter++;
        }

        // Weapon
        if (weapon != null)
        {
            int shopID = GetWeaponShopID(type, wSpecial);
            bool canEnhance = CanEnhanceWeapon(wSpecial);
            if (AlreadyHasRequestedEnhancement(weapon, type, cSpecial, hSpecial, wSpecial))
                skipCounter++;
            else if (canEnhance && shopID > 0)
                _AutoEnhance(weapon, shopID, (wSpecial == WeaponSpecial.Forge || (int)wSpecial > 6) ? "forge" : null, logging);
            else
                skipCounter++;
        }

        if (skipCounter > 0)
            Log($"Enhancement Skipped: {skipCounter} item(s)");

        void _AutoEnhance(InventoryItem item, int shopID, string? map = null, bool logging = false)
        {
            bool specialOnCape = item.Category == ItemCategory.Cape && cSpecial != CapeSpecial.None;
            bool specialOnHelm = item.Category == ItemCategory.Helm && hSpecial != HelmSpecial.None;
            bool specialOnWeapon = item.ItemGroup == "Weapon" && wSpecial != WeaponSpecial.None;
            string mapName = map ?? Map.Name ?? "whitemap";
            List<ShopItem> shopItems = GetShopItems(mapName, shopID);

            if (!shopItems.Any(x => x.Category == ItemCategory.Enhancement) || shopItems.Count == 0)
            {
                Log($"Enhancement Failed for {item.Name}[{item.ID}], (map: {mapName}, shopID: {shopID}): shop empty");
                return;
            }

            // Check if already optimally enhanced
            if (Player.Level == item.EnhancementLevel)
            {
                if (specialOnCape && (int)cSpecial == item.EnhancementPatternID)
                {
                    skipCounter++;
                    return;
                }
                if (specialOnHelm && (int)hSpecial == item.EnhancementPatternID)
                {
                    skipCounter++;
                    return;
                }
                if (specialOnWeapon)
                {
                    int checkPattern = ((int)wSpecial > 0 && (int)wSpecial <= 6) ? (int)type : 10;
                    if (checkPattern == item.EnhancementPatternID && (int)wSpecial == GetProcID(item))
                    {
                        skipCounter++;
                        return;
                    }
                }
                if (!specialOnCape && !specialOnHelm && !specialOnWeapon && (int)type == item.EnhancementPatternID)
                {
                    skipCounter++;
                    return;
                }
            }

            if (logging)
            {
                if (specialOnCape)
                    Log($"Searching Enhancement: Forge/{cSpecial.ToString().Replace("_", " ")} - \"{item.Name}\"");
                else if (specialOnWeapon)
                    Log($"Searching Enhancement: {((int)wSpecial > 0 && (int)wSpecial <= 6 ? type.ToString() : "Forge")}/{wSpecial.ToString().Replace("_", " ")} - \"{item.Name}\"");
                else
                    Log($"Searching Enhancement: {type} - \"{item.Name}\"");
            }

            List<ShopItem> availableEnh = [];
            foreach (ShopItem enh in shopItems)
            {
                if ((!Player.IsMember && enh.Upgrade) || enh.Level > Player.Level)
                    continue;

                string enhName = enh.Name.Replace(" ", "").Replace("\'", "").ToLower();

                if (specialOnCape && enhName.Contains(cSpecial.ToString().Replace("_", "").ToLower()))
                    availableEnh.Add(enh);
                else if (specialOnWeapon && enhName.Contains(wSpecial.ToString().Replace("_", "").ToLower()))
                    availableEnh.Add(enh);
                else if (specialOnHelm && enhName.Contains(hSpecial.ToString().Replace("_", "").ToLower()))
                    availableEnh.Add(enh);
                else if (item.Category == ItemCategory.Class && enhName.Contains("armor"))
                    availableEnh.Add(enh);
                else if (item.Category == ItemCategory.Helm && enhName.Contains("helm"))
                    availableEnh.Add(enh);
                else if (item.Category == ItemCategory.Cape && enhName.Contains("cape"))
                    availableEnh.Add(enh);
                else if (item.ItemGroup == "Weapon" && enhName.Contains("weapon"))
                    availableEnh.Add(enh);
            }

            if (availableEnh.Count == 0)
            {
                if (logging)
                    Log($"Enhancement Failed: no valid enhancement found for \"{item.Name}\"");
                return;
            }

            ShopItem? best = availableEnh.Count == 1
                ? availableEnh.First()
                : availableEnh.OrderByDescending(x => x.Level).ThenByDescending(x => x.Upgrade ? 1 : 0).First();

            if (best == null)
            {
                if (logging)
                    Log($"Enhancement Failed: could not determine best enhancement for \"{item.Name}\"");
                return;
            }

            if (best.ID == GetEnhID(item) && item.EnhancementLevel > 0 && best.Level == item.EnhancementLevel)
            {
                if (logging)
                    Log($"Enhancement Canceled: best enhancement already applied for \"{item.Name}\"");
                return;
            }

            int roomId = Map.RoomID;
            Send.Packet($"%xt%zm%enhanceItemShop%{roomId}%{item.ID}%{best.ID}%{shopID}%");
            Sleep(700);

            if (logging)
                Log($"Enhancement Applied: {best.Name} - {item.Name} (Lvl {best.Level})");
        }
    }

    #endregion

    #region Helpers

    private bool AlreadyHasRequestedEnhancement(
        InventoryItem item,
        EnhancementType type,
        CapeSpecial cSpecial,
        HelmSpecial hSpecial,
        WeaponSpecial wSpecial)
    {
        if (item == null || item.EnhancementLevel <= 0 || item.EnhancementLevel != Player.Level)
            return false;

        if (item.Category == ItemCategory.Cape)
        {
            if (cSpecial != CapeSpecial.None)
                return (int)cSpecial == item.EnhancementPatternID;
            return (int)type == item.EnhancementPatternID;
        }

        if (item.Category == ItemCategory.Helm)
        {
            if (hSpecial != HelmSpecial.None)
                return (int)hSpecial == item.EnhancementPatternID;
            return (int)type == item.EnhancementPatternID;
        }

        if (item.ItemGroup == "Weapon")
        {
            if (wSpecial == WeaponSpecial.None)
                return (int)type == item.EnhancementPatternID;
            return WeaponHasEnhancement(item, type, wSpecial);
        }

        if (item.Category == ItemCategory.Class)
            return (int)type == item.EnhancementPatternID;

        return false;
    }

    private bool WeaponHasEnhancement(InventoryItem item, EnhancementType type, WeaponSpecial wSpecial)
    {
        if (item == null || item.ItemGroup != "Weapon" || item.EnhancementLevel <= 0)
            return false;

        int currentPattern = item.EnhancementPatternID;
        int currentProc = GetProcID(item);

        if (wSpecial == WeaponSpecial.None)
            return currentPattern == (int)type;

        if ((int)wSpecial > 0 && (int)wSpecial <= 6)
            return currentPattern == (int)type && currentProc == (int)wSpecial;

        if (wSpecial == WeaponSpecial.Forge)
            return currentProc == 0 && (currentPattern == (int)type || currentPattern == 10);

        return currentProc == (int)wSpecial;
    }

    private int GetProcID(InventoryItem? item)
        => item == null ? 0 : Flash.GetGameObject<int>($"world.invTree.{item.ID}.ProcID");

    private int GetEnhID(InventoryItem? item)
        => item == null ? 0 : Flash.GetGameObject<int>($"world.invTree.{item.ID}.iEnh");

    private int GetEnhancementShopID(EnhancementType type)
    {
        return type switch
        {
            EnhancementType.Fighter => Player.Level >= 50 ? 768 : 141,
            EnhancementType.Thief => Player.Level >= 50 ? 767 : 142,
            EnhancementType.Hybrid => Player.Level >= 50 ? 766 : 143,
            EnhancementType.Wizard => Player.Level >= 50 ? 765 : 144,
            EnhancementType.Healer => Player.Level >= 50 ? 762 : 145,
            EnhancementType.SpellBreaker => Player.Level >= 50 ? 764 : 146,
            EnhancementType.Lucky => Player.Level >= 50 ? 763 : 147,
            _ => 0,
        };
    }

    private int GetWeaponShopID(EnhancementType type, WeaponSpecial wSpecial)
    {
        if ((int)wSpecial > 0 && (int)wSpecial <= 6)
        {
            return type switch
            {
                EnhancementType.Fighter => 635,
                EnhancementType.Thief => 637,
                EnhancementType.Hybrid => 633,
                EnhancementType.Wizard or EnhancementType.SpellBreaker => 636,
                EnhancementType.Healer => 638,
                EnhancementType.Lucky => 639,
                _ => 0,
            };
        }
        return 2142; // Forge weapon shop
    }

    private bool CanEnhanceCape(CapeSpecial cSpecial)
    {
        return cSpecial switch
        {
            CapeSpecial.Forge => IsQuestCompleted(8758),
            CapeSpecial.Absolution => IsQuestCompleted(8743),
            CapeSpecial.Avarice => IsQuestCompleted(8745),
            CapeSpecial.Vainglory => IsQuestCompleted(8744),
            CapeSpecial.Penitence => IsQuestCompleted(8822),
            CapeSpecial.Lament => IsQuestCompleted(8823),
            _ => true,
        };
    }

    private bool CanEnhanceHelm(HelmSpecial hSpecial)
    {
        return hSpecial switch
        {
            HelmSpecial.Vim => IsQuestCompleted(8824),
            HelmSpecial.Examen => IsQuestCompleted(8825),
            HelmSpecial.Forge => IsQuestCompleted(8828),
            HelmSpecial.Anima => IsQuestCompleted(8826),
            HelmSpecial.Pneuma => IsQuestCompleted(8827),
            HelmSpecial.Hearty => IsQuestCompleted(9466),
            _ => true,
        };
    }

    private bool CanEnhanceWeapon(WeaponSpecial wSpecial)
    {
        return wSpecial switch
        {
            WeaponSpecial.Forge => IsQuestCompleted(8738),
            WeaponSpecial.Lacerate => IsQuestCompleted(8739),
            WeaponSpecial.Smite => IsQuestCompleted(8740),
            WeaponSpecial.Valiance => IsQuestCompleted(8741),
            WeaponSpecial.Arcanas_Concerto => IsQuestCompleted(8742),
            WeaponSpecial.Elysium => IsQuestCompleted(8821),
            WeaponSpecial.Acheron => IsQuestCompleted(8820),
            WeaponSpecial.Praxis => IsQuestCompleted(9171),
            WeaponSpecial.Dauntless => IsQuestCompleted(9172),
            WeaponSpecial.Ravenous => IsQuestCompleted(9560),
            _ => true,
        };
    }

    private List<ShopItem> GetShopItems(string map, int shopID)
    {
        if (!string.Equals(Map.Name, map, StringComparison.OrdinalIgnoreCase))
        {
            string targetMap = $"{map}-100000";
            Map.Join(targetMap);
            Wait.ForMapLoad(map);
        }

        int retry = 0;
        while (!Player.Loaded && retry++ < 20)
        {
            Thread.Sleep(100);
        }

        retry = 0;
        while (retry++ < 20)
        {
            if (Shops.IsLoaded && Shops.ID == shopID)
                break;
            Shops.Load(shopID);
            Wait.ForActionCooldown(Models.GameActions.LoadShop);
            Thread.Sleep(1000);
        }

        if (!Shops.IsLoaded || Shops.ID != shopID)
        {
            Log($"Failed to load shop {shopID} in map {map}");
            return [];
        }

        return [.. Shops.Items];
    }

    private void EquipClass(string className)
    {
        if (!Player.Alive) return;
        if (!Inventory.TryGetItem(className, out var item)) return;
        Inventory.EquipItem(item.ID);
        Thread.Sleep(1500);
    }

    private void JumpWait()
    {
        Options.AttackWithoutTarget = false;
        Options.AggroAllMonsters = false;
        Options.AggroMonsters = false;

        string safeCell = Map.Cells.FirstOrDefault(c =>
            !string.IsNullOrEmpty(c) && c.Contains("Enter", StringComparison.OrdinalIgnoreCase))
            ?? Map.Cells.FirstOrDefault() ?? "Enter";

        Map.Jump(safeCell, "Spawn");
        Wait.ForCellChange(safeCell);
    }

    private bool IsQuestCompleted(int questID)
    {
        return Quests.HasBeenCompleted(questID);
    }

    private void Log(string message)
    {
        IScriptInterface.Instance?.Log($"[Enhancement] {message}");
    }

    private void Sleep(int ms)
    {
        Thread.Sleep(ms);
    }

    #region Unlock Checks

    private bool uAwe() => IsQuestCompleted(2937);
    private bool uForgeWeapon() => IsQuestCompleted(8738);
    private bool uLacerate() => IsQuestCompleted(8739);
    private bool uSmite() => IsQuestCompleted(8740);
    private bool uValiance() => IsQuestCompleted(8741);
    private bool uArcanasConcerto() => IsQuestCompleted(8742);
    private bool uAbsolution() => IsQuestCompleted(8743);
    private bool uVainglory() => IsQuestCompleted(8744);
    private bool uAvarice() => IsQuestCompleted(8745);
    private bool uForgeCape() => IsQuestCompleted(8758);
    private bool uElysium() => IsQuestCompleted(8821);
    private bool uAcheron() => IsQuestCompleted(8820);
    private bool uPenitence() => IsQuestCompleted(8822);
    private bool uLament() => IsQuestCompleted(8823);
    private bool uVim() => IsQuestCompleted(8824);
    private bool uExamen() => IsQuestCompleted(8825);
    private bool uForgeHelm() => IsQuestCompleted(8828);
    private bool uPneuma() => IsQuestCompleted(8827);
    private bool uAnima() => IsQuestCompleted(8826);
    private bool uDauntless() => IsQuestCompleted(9172);
    private bool uPraxis() => IsQuestCompleted(9171);
    private bool uRavenous() => IsQuestCompleted(9560);

    #endregion

    #endregion
}

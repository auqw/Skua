using Skua.Core.Models.Items;

namespace Skua.Core.Interfaces;

/// <summary>
/// Represents the inventory.
/// </summary>
/// <remarks>Implementations of this interface provide functionality for storing and equipping script items. This
/// interface extends <see cref="ICanEquip"/>, indicating that it supports equipping operations.</remarks>
public interface IScriptInventory : ICanEquip
{
    /// <summary>Whether the game provides separate bag, misc, class and house pools.</summary>
    bool HasCategories { get; }

    /// <summary>The current misc capacity, including server overrides; zero on older games.</summary>
    int MiscSlots { get; }
    int MiscUsedSlots { get; }
    int MiscFreeSlots { get; }

    /// <summary>Returns bag, misc, class or house. Older games return bag.</summary>
    string GetPool(ItemBase item);

    /// <summary>Checks the destination pool, existing stack and purchase materials.
    /// Quantity is the amount purchased, not the desired final total.
    /// If the check is unavailable, permits the request so the game can validate it.</summary>
    bool HasSpaceFor(ItemBase item, int quantity = 1);

    /// <summary>Classes cannot be deposited on games with category inventories.</summary>
    bool CanBank(InventoryItem item);

    /// <summary>Checks the current character's local favorite mark.</summary>
    bool IsFavorited(int itemId);
}
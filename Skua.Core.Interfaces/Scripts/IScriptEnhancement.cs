using Skua.Core.Models.Items;

namespace Skua.Core.Interfaces;

/// <summary>
/// Provides enhancement methods for equipping items with the best available enhancements.
/// </summary>
public interface IScriptEnhancement
{
    /// <summary>
    /// Automatically finds the best Enhancement for the given class and enhances all equipped gear.
    /// </summary>
    /// <param name="className">Name of the class to enhance.</param>
    /// <param name="forceEnhance">For classes that are received unenhanced.</param>
    void SmartEnhance(string? className, bool forceEnhance = false);

    /// <summary>
    /// Enhances all currently equipped gear with the specified enhancement types.
    /// </summary>
    void EnhanceEquipped(EnhancementType type, CapeSpecial cSpecial = CapeSpecial.None, HelmSpecial hSpecial = HelmSpecial.None, WeaponSpecial wSpecial = WeaponSpecial.None);

    /// <summary>
    /// Enhances a single item by name with the specified enhancement types.
    /// </summary>
    void EnhanceItem(string item, EnhancementType type, CapeSpecial cSpecial = CapeSpecial.None, HelmSpecial hSpecial = HelmSpecial.None, WeaponSpecial wSpecial = WeaponSpecial.None, bool logging = false);

    /// <summary>
    /// Gets the current enhancement type of the equipped class.
    /// </summary>
    EnhancementType CurrentClassEnh();

    /// <summary>
    /// Gets the current cape special of the equipped cape.
    /// </summary>
    CapeSpecial CurrentCapeSpecial();

    /// <summary>
    /// Gets the current helm special of the equipped helm.
    /// </summary>
    HelmSpecial CurrentHelmSpecial();

    /// <summary>
    /// Gets the current weapon special of the equipped weapon.
    /// </summary>
    WeaponSpecial CurrentWeaponSpecial();

    /// <summary>
    /// Checks if the Awe Enhancement is unlocked.
    /// </summary>
    bool IsAweUnlocked();
}

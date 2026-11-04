using Skua.Core.Models.Auras;

namespace Skua.Core.Interfaces;

/// <summary>
/// Defines methods and properties for querying and retrieving aura effects applied to a subject.
/// </summary>
/// <remarks>
/// Implementations of this interface provide access to information about active auras, including their
/// presence, stack counts, durations, and retrieval by name or pattern. This interface is typically used in systems
/// where entities can have multiple status effects (auras) that may influence behavior or attributes.
/// </remarks>
public interface IScriptAuras
{
    /// <summary>
    /// The list of auras.
    /// </summary>
    List<Aura> Auras { get; }

    /// <summary>The current HUD aura snapshots, including effects received through auSnap.</summary>
    List<AuraSnapshot> Snapshots { get; }

    /// <summary>Gets a HUD aura snapshot by name, or null when it is absent.</summary>
    AuraSnapshot? GetAuraSnapshot(string auraName);

    /// <summary>Gets the HUD stack count, or zero when the aura is absent.</summary>
    int GetAuraStacks(string auraName);

    /// <summary>
    /// Gets the amount Advanced Skills aura rules compare: the HUD stack count or the summed aura values, whichever is higher, read in one go. Zero when the aura is absent.
    /// </summary>
    float GetAuraRuleStacks(string auraName);

    /// <summary>
    /// Checks if the subtype has active <paramref name="auraName"/>.
    /// </summary>
    /// <param name="auraName">The aura name.</param>
    /// <returns>
    /// <see cref="bool"/> true if subject has the aura; otherwise false
    /// </returns>
    bool HasActiveAura(string auraName);

    /// <summary>
    /// Gets the aura of a subject type with the specified aura name.
    /// </summary>
    /// <param name="auraName">The aura name.</param>
    /// <returns>
    /// <see cref="Aura"/> object
    /// </returns>
    Aura? GetAura(string auraName);

    /// <summary>
    /// Retrieves the current value associated with the specified aura.
    /// </summary>
    /// <param name="auraName">The name of the aura for which to obtain the value. Cannot be null or empty.</param>
    /// <returns>The value of the specified aura as a floating-point number. Returns 0 if the aura is not found.</returns>
    float GetAuraValue(string auraName);

    /// <summary>
    /// Determines whether any of the specified aura names are currently active.
    /// </summary>
    /// <param name="auraNames">An array of aura names to check for activity. Each name represents an aura to be evaluated. Cannot be null.</param>
    /// <returns>true if at least one of the specified auras is active; otherwise, false.</returns>
    bool HasAnyActiveAura(params string[] auraNames);

    /// <summary>
    /// Tried to get the aura of a subject type with the specified aura name.
    /// </summary>
    /// <param name="auraName">The aura name.</param>
    /// <param name="aura">Here it returns the aura object if the aura is found.</param>
    /// <returns>
    /// <see cref="bool"/> true if subject has the aura; otherwise false
    /// </returns>
    bool TryGetAura(string auraName, out Aura? aura)
    {
        if (HasActiveAura(auraName))
        {
            aura = GetAura(auraName);
            return true;
        }
        aura = null;
        return false;
    }
}
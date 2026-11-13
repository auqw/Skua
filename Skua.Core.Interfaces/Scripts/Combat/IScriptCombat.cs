using Skua.Core.Models.Monsters;

namespace Skua.Core.Interfaces;

/// <summary>
/// Defines methods and properties for controlling combat actions in a scriptable environment, including targeting,
/// attacking, and managing combat state.
/// </summary>
/// <remarks>
/// This interface provides a standardized set of combat-related operations for use in scripting
/// scenarios, such as automating player actions or implementing custom combat logic. Implementations are expected to
/// handle targeting, attacking monsters or players, and managing the player's combat state.
/// </remarks>
public interface IScriptCombat
{
    /// <summary>
    /// Wether the counter attack handler is enabled.
    /// </summary>
    bool EnableCounterHandler { get; set; }

    /// <summary>
    /// Whether the player should stop using skills.
    /// </summary>
    bool StopAttacking { get; set; }

    /// <summary>
    /// Walks towards (approaches) the currently selected target.
    /// </summary>
    void ApproachTarget();

    /// <summary>
    /// Attacks the monster with the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The id of the monster to attack.</param>
    /// <remarks>This will not wait until the monster is killed, but simply select it and start attacking it.</remarks>
    bool Attack(int id);

    /// <summary>
    /// Attacks the specified instance of a <paramref name="monster"/>.
    /// </summary>
    /// <param name="monster">Monster to attack.</param>
    /// <remarks>This will not wait until the monster is killed, but simply select it and start attacking it.</remarks>
    bool Attack(Monster monster)
    {
        return Attack(monster.MapID);
    }

    /// <summary>
    /// Attacks the monster with specified <paramref name="name"/>.
    /// </summary>
    /// <param name="name">Name of the monster to attack.</param>
    /// <remarks>This will not wait until the monster is killed, but simply select it and start attacking it.</remarks>
    bool Attack(string name);

    /// <summary>
    /// Selects (targets) the monster with the specified <paramref name="id"/> without attacking it.
    /// </summary>
    /// <param name="id">The map id (or monster id) of the monster to select.</param>
    /// <returns><see langword="true"/> if a matching monster was found in the current cell and selected.</returns>
    /// <remarks>Does not walk to the monster or start auto attack. An auto attack that is already running is not cancelled; call <see cref="CancelAutoAttack"/> first if needed.</remarks>
    bool SelectMonster(int id);

    /// <summary>
    /// Selects (targets) the specified instance of a <paramref name="monster"/> without attacking it.
    /// </summary>
    /// <param name="monster">Monster to select.</param>
    /// <returns><see langword="true"/> if the monster was found in the current cell and selected.</returns>
    /// <remarks>Does not walk to the monster or start auto attack. An auto attack that is already running is not cancelled; call <see cref="CancelAutoAttack"/> first if needed.</remarks>
    bool SelectMonster(Monster monster)
    {
        return SelectMonster(monster.MapID);
    }

    /// <summary>
    /// Selects (targets) the monster with the specified <paramref name="name"/> without attacking it.
    /// </summary>
    /// <param name="name">Name of the monster to select, or "*" for any monster in the cell.</param>
    /// <returns><see langword="true"/> if a matching monster was found in the current cell and selected.</returns>
    /// <remarks>Does not walk to the monster or start auto attack. An auto attack that is already running is not cancelled; call <see cref="CancelAutoAttack"/> first if needed.</remarks>
    bool SelectMonster(string name);

    /// <summary>
    /// Attacks the player with specified <paramref name="name"/>. If not in PVP mode, this will only target the player, and not attack them.
    /// </summary>
    /// <param name="name">Name of the player to attack.</param>
    /// <remarks>This will not wait until the player is killed, but simply select it.</remarks>
    bool AttackPlayer(string name);

    /// <summary>
    /// Cancel the player's auto attack.
    /// </summary>
    void CancelAutoAttack();

    /// <summary>
    /// Deselects the currently selected target.
    /// </summary>
    void CancelTarget();

    /// <summary>
    /// Jumps to the current cell and waits for the player to exit combat.
    /// </summary>
    void Exit();

    /// <summary>
    /// Un-targets the player if they are currently targeted.
    /// </summary>
    void UntargetSelf();
}

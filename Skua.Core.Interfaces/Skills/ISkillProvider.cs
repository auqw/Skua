namespace Skua.Core.Interfaces;

/// <summary>
/// Defines the contract for a skill provider that determines when and how a bot should use skills, and manages
/// skill-related state in response to game events.
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for providing logic to select and trigger skills
/// based on game state, as well as handling events such as target changes and player death. This interface is intended
/// for use in systems where automated or programmatic skill usage is required.
/// </remarks>
public interface ISkillProvider
{
    /// <summary>
    /// Gets the number of skills loaded in this provider.
    /// </summary>
    int SkillCount { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the state should be reset when the target is changed.
    /// </summary>
    bool ResetOnTarget { get; set; }

    /// <summary>
    /// This method should return true if the bot should attempt to use a skill at the given time.
    /// </summary>
    /// <returns>Whether the bot should attempt to use a skill.</returns>
    bool? ShouldUseSkill(int skillIndex, bool canUse);

    /// <summary>
    /// This method should return the index of the next skill the bot should try and use. The mode parameter should be set to indicate how the skill should be used.
    /// </summary>
    /// <returns>The index of the skill to be used.</returns>
    (int, int) GetNextSkill();

    /// <summary>
    /// Checks whether the current target has been lost or changed and resets provider state if needed.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the provider reset its skill state and the current skill attempt should be aborted;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool OnTargetReset();

    /// <summary>
    /// This method is called when the player dies.
    /// </summary>
    void OnPlayerDeath();

    /// <summary>
    /// This method is called when the skill timer is stopped.
    /// </summary>
    void Stop();

    /// <summary>
    /// Loads this skill provider from the given file.
    /// </summary>
    /// <param name="file">The file to load this provider from.</param>
    void Load(string file);

    /// <summary>
    /// Saves this skill provider to the given file.
    /// </summary>
    /// <param name="file">The file to save this provider to.</param>
    void Save(string file);
}
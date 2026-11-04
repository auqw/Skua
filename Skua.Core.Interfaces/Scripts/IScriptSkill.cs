using Skua.Core.Models.Skills;

namespace Skua.Core.Interfaces;

/// <summary>
/// Defines the contract for managing and executing skills using configurable providers, intervals, and usage modes.
/// Supports loading, starting, and stopping skill execution, as well as customizing skill behavior for different
/// classes and scenarios.
/// </summary>
/// <remarks>
/// Implementations of this interface allow for flexible skill automation by supporting both default and
/// override providers, advanced skill loading from configuration, and runtime control over skill execution. Thread
/// safety and provider management may vary by implementation. The interface is designed to be used in bot frameworks or
/// automation systems that require dynamic skill usage strategies.
/// </remarks>
public interface IScriptSkill
{
    /// <summary>
    /// The default provider used if no override is set.
    /// </summary>
    ISkillProvider BaseProvider { get; }

    /// <summary>
    /// This provider is always used over any set through SetProvider.
    /// </summary>
    ISkillProvider? OverrideProvider { get; set; }

    /// <summary>
    /// How long <see cref="SkillUseMode.WaitForCooldown"/> waits for the current skill's cooldown before skipping it, in milliseconds; 0 or less waits as long as it takes.
    /// A skill held up by anything other than its cooldown (mana, a stun, an empty potion slot, a locked skill) is skipped at once.
    /// </summary>
    /// <remarks>
    /// The default implementation converts to and from <see cref="SkillTimeout"/>, so implementations written before this member existed still compile.
    /// </remarks>
    int SkillTimeoutMs
    {
        get => SkillTimeout <= 0 ? SkillTimeout : (int)Math.Min(int.MaxValue, (long)SkillTimeout * Math.Max(1, SkillInterval));
        set => SkillTimeout = value <= 0 ? value : (int)(((long)value + Math.Max(1, SkillInterval) - 1) / Math.Max(1, SkillInterval));
    }

    /// <summary>
    /// <see cref="SkillTimeoutMs"/> in the older unit, multiples of <see cref="SkillInterval"/> (100 ms by default), as older scripts and
    /// <see cref="StartAdvanced(string, int, SkillUseMode)"/> give it. A timeout set this way stays a count of intervals, so it follows a later change of
    /// <see cref="SkillInterval"/>; read in this unit, a millisecond timeout is rounded up.
    /// </summary>
    int SkillTimeout { get; set; }

    /// <summary>
    /// The longest the skill thread sleeps between checks, in milliseconds (default 100). It is not the rate at which skills are used:
    /// the thread wakes as soon as the global cooldown or the awaited skill's cooldown ends, so skills are used as soon as they are ready.
    /// It is also the unit of the older <see cref="SkillTimeout"/>.
    /// </summary>
    int SkillInterval { get; set; }

    /// <summary>
    /// Determines whether the skill thread is currently running.
    /// </summary>
    bool TimerRunning { get; }

    /// <summary>
    /// The way the bot will use skills:
    /// <list type="bullet">
    /// <item><see cref="SkillUseMode.WaitForCooldown"/>
    /// <description>If not set to skip, will wait for the skill to be available and then use it.</description>
    /// </item>
    /// <item><see cref="SkillUseMode.UseIfAvailable"/>
    /// <description>Whenever a skill is available it will use the skill, if not will skip it.</description>
    /// </item>
    /// </list>
    /// </summary>
    SkillUseMode SkillUseMode { get; set; }

    /// <summary>
    /// Checks if the skill with specified <paramref name="index"/> has cooled down.
    /// </summary>
    /// <param name="index">Index of the skill to check.</param>
    /// <returns><see langword="true"/> if the skill is ready to use.</returns>
    bool CanUseSkill(int index);

    /// <summary>
    /// Gets how long until the skill with the specified <paramref name="index"/> has cooled down: its own cooldown (with haste) or the global cooldown, whichever ends later.
    /// </summary>
    /// <param name="index">Index of the skill to check.</param>
    /// <returns>The time left in milliseconds, 0 if the skill has cooled down.</returns>
    int SkillCooldownRemaining(int index);

    /// <summary>
    /// Gets how long until the global cooldown, which starts with every skill used, has ended.
    /// </summary>
    /// <returns>The time left in milliseconds, 0 if no global cooldown is running.</returns>
    int GlobalCooldownRemaining();

    /// <summary>
    /// Uses the skill with the specified <paramref name="index"/> (1-4).
    /// </summary>
    /// <param name="index">Index of the skill.</param>
    /// <returns>
    /// <see langword="true"/> if the skill was used, <see langword="false"/> if the skill was not used.
    /// </returns>
    bool UseSkill(int index);

    void Resume();
    void Pause();

    /// <summary>
    /// Loads the skills from the specified <paramref name="skills"/> string.
    /// </summary>
    /// <param name="skills">String of the skills</param>
    /// <param name="skillTimeout">Timeout in multiples of <see cref="SkillInterval"/> milliseconds before skipping the current unavailable skill when using <see cref="SkillUseMode.WaitForCooldown"/>.</param>
    /// <param name="skillMode">Sets what <see cref="SkillUseMode"/> to use.</param>
    void LoadAdvanced(string skills, int skillTimeout = -1, SkillUseMode skillMode = SkillUseMode.UseIfAvailable);

    /// <summary>
    /// Loads the skills of the specified <paramref name="className"/> from AdvancedSkills.json.
    /// </summary>
    /// <param name="className">Name of the class to use</param>
    /// <param name="autoEquip">Whether to equip the class, useful if you want to use multiple skill sets for 1 class</param>
    /// <param name="useMode">Some classes can have different use modes:
    /// <list type="bullet">
    /// <item>
    /// <see cref="ClassUseMode.Base"/>
    /// <description>Default combo</description>
    /// </item>
    /// <item>
    /// <see cref="ClassUseMode.Atk"/>
    /// <description>Full damage combo</description>
    /// </item>
    /// <item>
    /// <see cref="ClassUseMode.Def"/>
    /// <description>Defensive combo</description>
    /// </item>
    /// <item>
    /// <see cref="ClassUseMode.Farm"/>
    /// <description>Farming combo</description>
    /// </item>
    /// <item>
    /// <see cref="ClassUseMode.Solo"/>
    /// <description>Soloing combo</description>
    /// </item>
    /// <item>
    /// <see cref="ClassUseMode.Supp"/>
    /// <description>Support combo</description>
    /// </item>
    /// </list>
    /// </param>
    /// <remarks>If skills from the desired class doesn't exist, generic skills will be used instead.</remarks>
    void LoadAdvanced(string className, bool autoEquip, ClassUseMode useMode = ClassUseMode.Base);

    /// <summary>
    /// Loads the skills of the specified <paramref name="className"/> and <paramref name="mode"/> from AdvancedSkills configuration.
    /// </summary>
    /// <param name="className">Name of the class to use</param>
    /// <param name="mode">The mode as a string (e.g., "Base", "Solo", "Farm", "Atk", "Def", "Supp")</param>
    /// <param name="autoEquip">Whether to equip the class</param>
    void LoadAdvanced(string className, string mode, bool autoEquip = true);

    /// <summary>
    /// Loads the skills of the specified <paramref name="className"/> and <paramref name="mode"/> and starts the skill thread.
    /// </summary>
    /// <param name="className">Name of the class to use</param>
    /// <param name="mode">The mode as a string (e.g., "Base", "Solo", "Farm", "Atk", "Def", "Supp")</param>
    /// <param name="autoEquip">Whether to equip the class</param>
    void StartAdvanced(string className, string mode, bool autoEquip = true)
    {
        LoadAdvanced(className, mode, autoEquip);
        Start();
    }

    /// <summary>
    /// Sets the current skill provider (<see cref="OverrideProvider"/>).
    /// </summary>
    /// <param name="provider">Desired provider that implements <see cref="ISkillProvider"/></param>
    void SetProvider(ISkillProvider provider);

    /// <summary>
    /// Loads the skills from the specified <paramref name="skills"/> string and starts the skill thread.
    /// </summary>
    /// <param name="skills">String of the skills</param>
    /// <param name="skillTimeout">Timeout in multiples of <see cref="SkillInterval"/> milliseconds before skipping the current unavailable skill when using <see cref="SkillUseMode.WaitForCooldown"/>.</param>
    /// <param name="skillMode">Sets what <see cref="SkillUseMode"/> to use.</param>
    void StartAdvanced(string skills, int skillTimeout = -1, SkillUseMode skillMode = SkillUseMode.UseIfAvailable)
    {
        LoadAdvanced(skills, skillTimeout, skillMode);
        Start();
    }

    /// <summary>
    /// Loads the skills of the specified <paramref name="className"/> from AdvancedSkills.json and starts the skill thread.
    /// </summary>
    /// <param name="className">Name of the class to use</param>
    /// <param name="autoEquip">Whether to equip the class, useful if you want to use multiple skill sets for 1 class</param>
    /// <param name="useMode">Some classes can have different use modes:
    /// <list type="bullet">
    /// <item>
    /// <see cref="ClassUseMode.Base"/>
    /// <description>Default combo</description>
    /// </item>
    /// <item>
    /// <see cref="ClassUseMode.Atk"/>
    /// <description>Full damage combo</description>
    /// </item>
    /// <item>
    /// <see cref="ClassUseMode.Def"/>
    /// <description>Defensive combo</description>
    /// </item>
    /// <item>
    /// <see cref="ClassUseMode.Farm"/>
    /// <description>Farming combo</description>
    /// </item>
    /// <item>
    /// <see cref="ClassUseMode.Solo"/>
    /// <description>Soloing combo</description>
    /// </item>
    /// <item>
    /// <see cref="ClassUseMode.Supp"/>
    /// <description>Support combo</description>
    /// </item>
    /// </list>
    /// </param>
    /// <remarks>If skills from the desired class doesn't exist, generic skills will be used instead.</remarks>
    void StartAdvanced(string className, bool autoEquip, ClassUseMode useMode = ClassUseMode.Base)
    {
        LoadAdvanced(className, autoEquip, useMode);
        Start();
    }

    /// <summary>
    /// Starts the skill thread, which uses the registered skills as soon as they are ready, checking at least every <see cref="SkillInterval"/> milliseconds.
    /// </summary>
    /// <remarks>The skill thread is automatically stopped when the bot is stopped.</remarks>
    void Start();

    /// <summary>
    /// Stops the skill thread.
    /// </summary>
    void Stop();

    /// <summary>
    /// Sets the current skill provider to the base provider.
    /// </summary>
    void UseBaseProvider();
}
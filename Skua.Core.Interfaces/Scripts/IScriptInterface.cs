using Skua.Core.Interfaces.Services;
using Skua.Core.Models;

namespace Skua.Core.Interfaces;

/// <summary>
/// Defines methods for initializing and managing script-related timers within a component.
/// </summary>
public interface IScriptInterfaceManager
{
    /// <summary>
    /// Initializes the component and prepares it for use.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Asynchronously stops the running timer, if it is active.
    /// </summary>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    Task StopTimerAsync();
}

/// <summary>
/// Defines the contract for the main scripting interface, providing access to core bot services, game automation
/// features, and script management utilities.
/// </summary>
/// <remarks>
/// The IScriptInterface exposes a comprehensive set of properties and methods for interacting with game
/// elements, managing script execution, and customizing bot behavior. It serves as the primary entry point for scripts,
/// aggregating functionality such as combat automation, inventory management, quest handling, event monitoring, and
/// user interface interactions. Implementations of this interface are expected to be thread-safe only if explicitly
/// documented. Consumers should use the provided properties and methods to interact with the game environment and
/// control script flow, rather than accessing underlying implementations directly.
/// </remarks>
public interface IScriptInterface
{
    /// <summary>
    /// Static instance of the current <see cref="IScriptInterface"/>.
    /// </summary>
    static IScriptInterface Instance { get; protected set; }

    /// <summary>
    /// Class for Flash operations.
    /// </summary>
    IFlashUtil Flash { get; }

    /// <summary>
    /// Class to manage the status of the current script.
    /// </summary>
    IScriptStatus Manager { get; }

    /// <summary>
    /// Class to manage the state of the Auto Attack/Hunt functions.
    /// </summary>
    IScriptAuto Auto { get; }

    /// <summary>
    /// Class to control consumable Boosts.
    /// </summary>
    IScriptBoost Boosts { get; }

    /// <summary>
    /// Stats of the current application runtime.
    /// </summary>
    IScriptBotStats Stats { get; }

    /// <summary>
    /// Gets the collection of aura effects currently applied to the player.
    /// </summary>
    IScriptSelfAuras Self { get; }

    /// <summary>
    /// Gets the collection of auras currently applied to the target entity.
    /// </summary>
    IScriptTargetAuras Target { get; }

    /// <summary>
    /// Service to monitor aura changes and receive events when auras are activated, deactivated, or stack values change.
    /// </summary>
    IAuraMonitorService AuraMonitor { get; }

    /// <summary>
    /// Helper for managing ultra boss mechanics including counter-attacks and aura monitoring.
    /// </summary>
    IUltraBossHelper UltraBossHelper { get; }

    /// <summary>
    /// Class to control combat mechanics.
    /// </summary>
    IScriptCombat Combat { get; }

    /// <summary>
    /// Class with methods for killing monsters/players.
    /// </summary>
    IScriptKill Kill { get; }

    /// <summary>
    /// Class with methods for hunting (teleport while killing) monsters.
    /// </summary>
    IScriptHunt Hunt { get; }

    /// <summary>
    /// Class to manage drops.
    /// </summary>
    IScriptDrop Drops { get; }

    /// <summary>
    /// Class with events that trigger with in-game events.
    /// </summary>
    IScriptEvent Events { get; }

    /// <summary>
    /// Class to manage the reputation list.
    /// </summary>
    IScriptFaction Reputation { get; }

    /// <summary>
    /// Class to manage the House inventory.
    /// </summary>
    IScriptHouseInv House { get; }

    /// <summary>
    /// Class to manage the Player Inventory.
    /// </summary>
    IScriptInventory Inventory { get; }

    /// <summary>
    /// Class to manage the player Temporary Inventory.
    /// </summary>
    IScriptTempInv TempInv { get; }

    /// <summary>
    /// Class to manage the player Bank.
    /// </summary>
    IScriptBank Bank { get; }

    /// <summary>
    /// Class to help inventory management.
    /// </summary>
    IScriptInventoryHelper InvHelper { get; }

    /// <summary>
    /// Class with options that reflect the in-game Advanced Options.
    /// </summary>
    IScriptLite Lite { get; }

    /// <summary>
    /// Class with options to customize the runtime of the bot.
    /// </summary>
    IScriptOption Options { get; }

    /// <summary>
    /// Class with properties of the current map and methods to travel to/in them.
    /// </summary>
    IScriptMap Map { get; }

    /// <summary>
    /// Class to manage the Monsters in the current map.
    /// </summary>
    IScriptMonster Monsters { get; }

    /// <summary>
    /// Class with properties of the current player.
    /// </summary>
    IScriptPlayer Player { get; }

    /// <summary>
    /// Class to manage Quests.
    /// </summary>
    IScriptQuest Quests { get; }

    /// <summary>
    /// Class with methods to send messages/packets.
    /// </summary>
    IScriptSend Send { get; }

    /// <summary>
    /// Class with properties of the current shop and methods to load, buy and sell items.
    /// </summary>
    IScriptShop Shops { get; }

    /// <summary>
    /// Class to control how the bot will use skills.
    /// </summary>
    IScriptSkill Skills { get; }

    /// <summary>
    /// Class to enhance items and equipped gear.
    /// </summary>
    IScriptEnhancement Enhancements { get; }

    /// <summary>
    /// Class with methods to wait certain actions of the game.
    /// </summary>
    IScriptWait Wait { get; }

    /// <summary>
    /// Class with properties of servers and methods to connect to them.
    /// </summary>
    IScriptServers Servers { get; }

    /// <summary>
    /// Class to control handlers which run in specific timings.
    /// </summary>
    IScriptHandlers Handlers { get; }

    /// <summary>
    /// Class to connect to a proxy and get packets sent between client and server.
    /// </summary>
    ICaptureProxy GameProxy { get; }

    /// <summary>
    /// Class to access and manage account tags.
    /// </summary>
    IScriptAccounts Accounts { get; }

    /// <summary>
    /// Options within the compiled script.
    /// </summary>
    IScriptOptionContainer? Config { get; }

    /// <summary>
    /// Whether the bot should stop.
    /// </summary>
    bool ShouldExit { get; }

    /// <summary>
    /// Current version of Skua.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// A random instance for the script.
    /// </summary>
    Random Random { get; set; }

    /// <summary>
    /// Sells all items marked as junk in the Junk Items configuration.
    /// </summary>
    void SellJunk();

    /// <summary>
    /// Sleeps the bot for the specified time period.
    /// </summary>
    /// <param name="ms">Time in milliseconds for the bot to sleep.</param>
    void Sleep(int ms);

    /// <summary>
    /// Asynchronously sleeps the bot for the specified time period without blocking the thread.
    /// </summary>
    /// <param name="ms">Time in milliseconds for the bot to sleep.</param>
    /// <returns>A task representing the asynchronous sleep operation.</returns>
    Task SleepAsync(int ms);

    /// <summary>
    /// Stops the script
    /// </summary>
    /// <param name="runScriptStoppingEvent">Whether to fire the <see cref="IScriptEvent.ScriptStopping"/> event.</param>
    /// <remarks>This method is deprecated. Use <see cref="StopAsync"/> instead for proper async/await pattern.</remarks>
    [Obsolete("Use StopAsync instead for proper async/await pattern.")]
    void Stop(bool runScriptStoppingEvent = true);

    /// <summary>
    /// Asynchronously stops the script
    /// </summary>
    /// <param name="runScriptStoppingEvent">Whether to fire the <see cref="IScriptEvent.ScriptStopping"/> event.</param>
    Task StopAsync(bool runScriptStoppingEvent = true);

    /// <summary>
    /// Writes a message to the script logs.
    /// </summary>
    /// <param name="message">Message to log.</param>
    void Log(string message);

    /// <summary>
    /// Schedules the specified <paramref name="action"/> to run after the desired <paramref name="delay"/> in ms.
    /// </summary>
    /// <param name="delay">Time to wait before invoking the action.</param>
    /// <param name="action">Action to run. This can be passed as a lambda expression.</param>
    Task Schedule(int delay, Action<IScriptInterface> action);

    /// <summary>
    /// Schedules the specified <paramref name="function"/> to run after the desired <paramref name="delay"/> in ms.
    /// </summary>
    /// <param name="delay">Time to wait before invoking the action.</param>
    /// <param name="function">Action to run. This can be passed as a lambda expression.</param>
    Task Schedule(int delay, Func<IScriptInterface, Task> function);

    /// <summary>
    /// Shows a message box.
    /// </summary>
    /// <param name="message">Message in the popup.</param>
    /// <param name="caption">Title of the popup.</param>
    /// <param name="yesAndNo">Whether it should have 'Yes' and 'No' buttons. If <see langword="false"/> will only have the 'Ok' button.</param>
    /// <returns><see langword="true"/> if the 'Yes' or 'Ok' button was clicked.</returns>
    bool? ShowMessageBox(string message, string caption, bool yesAndNo = false);

    /// <summary>
    /// Shows a message box with the specified buttons.
    /// </summary>
    /// <param name="message">Message in the popup.</param>
    /// <param name="caption">Title of the popup.</param>
    /// <param name="buttons">A list of buttons that will be shown.</param>
    /// <returns>A <see cref="DialogResult"/> object with the text and value of the button. The value is the index of the button in the array that was passed, meaning that -1 is no button found.</returns>
    DialogResult ShowMessageBox(string message, string caption, params string[] buttons);
}

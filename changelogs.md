# Skua 1.4.3.0
## Released: March 21, 2026

# Drops Fix Summary

## What was fixed
- Stabilized the drop handling loop to avoid breaking on bad states or interruptions.
- Improved handling for pickup names/IDs and `Reject Else` behavior.

## General changes
- Updated drop-task state checks so start/enable logic tracks an actively running task.
- Added safer cancellation/exception handling in the drop processing loop.
- Switched pickup operations to snapshot-based arrays each cycle.
- Improved whitelist construction for `Reject Else` to account for configured names, IDs, and active drops.
- Corrected drop update broadcasts for pickup IDs.

## Result
- More reliable drop processing.
- Cleaner pickup/reject behavior.
- Reduced chance of stuck or failed drop handling.

<!-- --------------------------- -->
# Hotkey Fix Summary

## What was fixed
- Prevented invalid modifier-only combos like `Ctrl+Ctrl` from being accepted.
- Stopped crashes caused by invalid hotkey strings during hotkey parsing/reload.

## General changes
- Added validation in the hotkey assignment dialog to require a non-modifier key.
- Added inline hint text to guide users during key capture and invalid input.
- Hardened hotkey parsing to safely return invalid instead of throwing exceptions.

## Result
- Invalid combinations are now rejected gracefully.
- Valid hotkeys continue to save/reload normally.
- Hotkey setup UX is clearer and safer.

<!-- --------------------------- -->
# Options Fix Summary

## What was fixed
- Enum-based options could miss category assignment because constructor flow returned early for enums.

## General changes
- Updated `Skua.Core/ViewModels/OptionContainerItemViewModel.cs`.
- Moved `Category = option.Category;` earlier in the constructor (before the enum early-return path).

## Result
- Enum options now keep their category metadata consistently.
- Option grouping/display behavior is now aligned with non-enum options.

## New Contributors
* @robmart made their first contribution in https://github.com/auqw/Skua/pull/37

**Full Changelog**: https://github.com/auqw/Skua/compare/1.4.2.0...1.4.3.0

## What's Changed
* fix enum options by @ArrowDev123 in https://github.com/auqw/Skua/pull/36
* Give access to missing packets by @robmart in https://github.com/auqw/Skua/pull/37

---

# Skua 1.4.2.0
## Released: February 12, 2026

# Additions:
 - `Tools > Junk Items`
 
<img width="128" height="141" alt="image" src="https://github.com/user-attachments/assets/b581be1d-d021-4ea6-9acd-85b915954b6f" /> 

   * A new window to tag items as junk & sell them once your done.
   
<img width="796" height="449" alt="image" src="https://github.com/user-attachments/assets/27ba9e2d-f547-4898-be50-255791c12bb5" />
   
   * A Search Feature to narrow down items to `ItemName/ItemID/Category`
   
 <img width="278" height="205" alt="image" src="https://github.com/user-attachments/assets/976b1601-3b48-4129-85e3-252d0fcb06dd" />
 
   * Marked items will also show with the [junk] tag in the tools > grabber > inventory
   
<img width="322" height="252" alt="image" src="https://github.com/user-attachments/assets/87b9574c-8d57-406e-b98e-0319c99455ca" />

   
   * the `JunkItems.json` file is in `%appdata%/skua/scripts/JunkItems.json`, and can be updated and pushed by the devs to add more junk items that will be marked as junk by default -- as well as helpful lists added by the community.
 - A small fix to the show & hide that had a potential crash related to it

**Full Changelog**: https://github.com/auqw/Skua/compare/1.4.1.1...1.4.2.0

---

# Skua 1.4.1.1
## Released: February 05, 2026

## What's Changed
* Fix: Add handler for group start by @ArrowDev123 in https://github.com/auqw/Skua/pull/34


**Full Changelog**: https://github.com/auqw/Skua/compare/1.4.1.0...1.4.1.1

---

# Skua 1.4.1.0
## Released: February 05, 2026

## Changes
- UI/account manager refresh by @ArrowDev123 in https://github.com/auqw/Skua/pull/33

## New Contributors
- @ArrowDev123 made their first contribution in https://github.com/auqw/Skua/pull/33

**Full Changelog**: [`1.4.0.5...1.4.1.0`](<https://github.com/auqw/Skua/compare/1.4.0.5...1.4.1.0>)

---

# Skua 1.4.0.5
## Released: January 28, 2026

# Things Added

## UI / Menu Changes
- `Options > CoreBots` & the Class dropdown from the `Auto` menu
  - Equipment & Classes are now sorted alphabetically

## Tools
- Tool for us Script-Devs / Those that wish to see a Boss/Shop that is quest locked;
  - Available in:
    - `Tools > Grabber > Quests`
    - `Tools > Loader`
  - Added a `Fake Complete` button
    - Runs `Bot.Quests.UpdateQuest(ID)`

  - **Previously** this had to be done via:
    - `Tools > Console`
    - Manually typing `Bot.Quests.UpdateQuest(<QuestID>);`

  - **Now** you can:
    - Open either `Tools > Loader` **or** `Tools > Grabber > Quests`
    - For **Grabber > Quests**:
      - Quests must already be loaded in-game
      - Press **Grab**
      - Select the quest
      - Click **Fake Complete**
    - For **Loader**:
      - Select the quest
      - Click **Fake Complete**

  - Usage notes:
    - Run this **outside** the map you want to see the shop/mob in, then join the map
    - You will **not** be able to:
      - Buy items (they are still quest locked)
      - Complete quests you have "fake" progressed to
    - *But* you **will** be able to see the boss/shop

## Script-Dev Stuff
- Added the rest of the `Bot.Lite.*` options for code fill-in  
  - **Script-dev only** (normal users can ignore this)
  - Mirrors the in-game `Options > Advanced` menu

**Full Changelog**: https://github.com/auqw/Skua/compare/1.4.0.4...1.4.0.5

---

# Skua 1.4.0.4
## Released: January 23, 2026

## Changes/Fixes

- Compiler race conditions hopefully fixed
- Search scripts causing a lockup should be fixed
- Refactored Skua.AS3 for quicker navigation
- Auras should be more reliable in advanced skills
  - Apparently, AQW doesn't handle their expired auras, and they stay inside the aura array, thus causing overflow issues

**Full Changelog**:[`1.4.0.3...1.4.0.4`](<https://github.com/auqw/Skua/compare/1.4.0.3...1.4.0.4>)

---

# Skua 1.4.0.3
## Released: January 19, 2026

## Actually fixed compiler

### Code that was the issue:
```cs
Span<Range> lineRanges = stackalloc Range[256];
```
This only read 256 imports, while a few had more than 256, and I was not aware of that.

Scripts tested:
- `0AllClasses.cs`
- `0AllStories.cs`
- `0FarmerJoeDoAll.cs`
- `0VoidHighlord.cs`
- `JoePrepsForUltras.cs` (this is for Insert's scripts for Grim)

**Full Changelog**:[`1.4.0.2...1.4.0.3`](<https://github.com/auqw/Skua/compare/1.4.0.2...1.4.0.3>)

---

# Skua 1.4.0.2
## Released: January 18, 2026

# Auto-Hunt & Auto-Attack Changelog

Fixed:
•  Auto-Hunt now properly moves between cells when targeted monsters are killed
•  Auto-Hunt continues hunting same monster type after killing individual instances
•  Priority MapID hunting now camps specific cells and waits for respawns
•  Reduced hunt delays for faster monster targeting (1000ms → 200ms) 
•  Optimized cell traversal to reduce unnecessary full map rescans

Technical:
•  Changed from MapID-based to name-based hunting for targeted monsters
•  Priority MapIDs now camp single cells instead of searching multiple locations 

**Full Changelog**: https://github.com/auqw/Skua/compare/1.4.0.0...1.4.0.2

---

# Skua 1.4.0.1
## Released: January 18, 2026

- Compiler Fixed & performance "boosted" 
 - Preload should be good ( issue was most likely the compiler.. shenanigans) 
 - Fix crash upon clicking `usage guide` / `build guide` in about tab of manager

**Full Changelog**: https://github.com/auqw/Skua/compare/1.3.3.2...1.4.0.1

---

# Skua 1.4.0.0
## Released: January 18, 2026

## Changes/Fixes

### [**.NET 10**](<https://dotnet.microsoft.com/en-us/download/dotnet/10.0>)  I am sorry, Windows 7 users, it was bound to happen. 
Please go ahead and download the new net version for [Skua 1.4.0.0](<https://github.com/auqw/Skua/releases/tag/1.4.0.0>).

### Compiler Changes
- Added a check for `version: #.#.#.#` in the first 6 lines of a script.
  - If a version number is present and it's higher than your current Skua version, it'll prompt you to update and open the Skua release page.
    - This will stop compilation immediately so no errors are thrown
 
- Added a Mutex for the compiler so only one account will compile while the others wait for that one to finish.

- The compiler now builds each `//cs_include` file

With these compiler changes come higher initial compile times (expected)
Using `0FarmerJoeKitDoAll.cs` as a performance test between 1.3.3.2 and 1.4.0.0

1.3.3.2: ~7 seconds
1.4.0.0: ~14-16 seconds

However, if any `Core` files update, we'll only need to recompile the one that updated; thus, consecutive compiles can get as low as ~2 seconds.

## Additions
- Tools > Loader 
  - Added updating from the scripts repo to always have the latest if it gets updated there aswell
  - Added `Update Range` with a textbox for a range of `QuestIDs`
  - Added `Copy Name+IDS` output is `// {questName} | {questID}`

This update is "small", however, we are dropping support for Windows 7, so I think the bump to 1.4.0.0 was a good idea

**Full Changelog**: [`1.3.3.2...1.4.0.0`](<https://github.com/auqw/Skua/compare/1.3.3.2...1.4.0.0>)

---

# Skua 1.3.3.2
## Released: January 14, 2026

# Quest.txt is now QuestData.json and it updates from scripts [repo ](https://github.com/auqw/Scripts/blob/Skua/QuestData.json) now

**Full Changelog**: https://github.com/auqw/Skua/compare/1.3.3.1...1.3.3.2

---

# Skua 1.3.3.1
## Released: January 10, 2026


# Features/Changes

### Auto-Attack | Auto-Hunt
  - `Manual MapIDs` now work properly, and will attack MID[index 0], then MID[index 1]. If MID[index 0] respawns while MID[index 1] is alive, it will swap mobs 👍 

### Packet Interceptor 
  - Packet logging when `Log packets` is unchecked is now fixed

### Compiler Changes/Script Caching
  - Scripts get cached to `%APPDATA%/Skua/Scripts/Cached-Scripts`
  - This improves startup time for re-running scripts (assuming `auqw/Scripts` isn't updating as you are running them)
  - There are still planned changes to the compiler

### Planned changes for the compiler
  - Currently, the compiler takes each script we use for a certain script into one single file, then compiles that into the final running script.
  - Example: `0NecroticSwordOfDoom.cs` uses `CoreNSOD.cs` and that needs. `CoreBots.cs` (`0NecroticSwordOfDoom`>`CoreNSOD`>`CoreBots`)
  - This way of compiling for cached scripts is terrible. Anytime any script in that flow changes, it'll need to recompile everything.
  - So, to remedy this, I want to change the compiler to compile each script separately

### Minor Optimizations

### UI Changes
  - Accounts with tags will now align correctly
  - Whenever you get the `443` error for scripts, a pop-up will open saying
    - "Unable to connect to GitHub."
    - "Please check your internet connection and try again."
    - "If the problem persists, GitHub may be temporarily unavailable."

**Full Changelog**: https://github.com/auqw/Skua/compare/1.3.3.0...1.3.3.1

---

# Skua 1.3.3.0
## Released: December 20, 2025

### Changes
 - [***"Downloaded 1 Scripts" popup***](https://imgur.com/2GnQUbI) will no longer appear when *no* scripts are actually downloaded.
   - Minor Skill improvements ( don't ask, I don't remember).
   - Added "Dodge" class Use Mode back, as before this, it was crashing the client when trying to save as a non-existent mode.
 - Auras returning `NULL` during long sessions
   - Flash function `rebuildAuraArray` - filters out null/invalid auras for all the "get aura" functions
- Helpers > Runtime;
   - If a quest is registered, automatically enable `Pick Drops`
### ***__New Feature:__***
 - [Account Tags](https://imgur.com/x4FpfUz) & conditional checks to go along with it (for us coders).

**Full Changelog**: https://github.com/auqw/Skua/compare/1.3.2.0...1.3.3.0

---

# Skua 1.3.2.0
## Released: December 18, 2025

### Fixes
 - Auto > Attack/Hunt;
   - Targeting now works properly to what you click on and doesn't stray from it.
 - More Aura Fixes for the same issue as last time ( hopefully we're good now)
 - Some fixes to Advanced skills
### Additions
 - Tools > Grabber > Inventory > Sell button; 
   - Fixed it selling "all" of [item]
 - Helpers > runtime;
   - Quests can now have an optional `RewardID` ( for those "choose reward" quests), reward + accept/requirement's id will also be added.
   - Turn-ins will now use multi-turn-in. ( more turn-ins at once, before it did it one at a time)
 - `Bot.Quests.RegisterQuests();` can now also accept reward ids alongside the id... 
 E.G.:
 ```cs
 Bot.Quests.RegisterQuests((1,1), (2,3));
 ```
 - Helpers > Current Drops;
   - Search function added.
 - Faster AA[0] for CSH/CSS/other
 - Auto > Attack/Hunt;
   - Faster target swapping
   - You can insert a `MonsterMapID` array ( e.g., 1,2,3), and it'll attack them in order, going back to the beginning of the order if and when it respawns.

---

# Skua 1.3.1.0
## Released: December 08, 2025

## Fixes
1. Hopefully fixed a random crash from auras
2. Hopefully Fixed a false positive from `Skua.WPF.dll`
4. HP, Mana, and PartyHeal percentage/absolute check actually works and saves now
5. Jump panel causing hitches when you jump cells
6. Party Heal actually exists and saves now
7. "Search Scripts" would sometimes cause hitches; this **should be fixed**

## Changes
1. Login backgrounds now saves and loads from `Skua.Settings.json` instead of the separate file `background-config.json` (you will need to re-set your background)
2. The last server you selected in the manager will now save, and next time you open the manager, it'll re-select it

### Minor changes (not important)
1. Added a flash trust file for skua
2. Centralized version change in `Directory.Build.props`

**Full Changelog**: https://github.com/auqw/Skua/compare/1.3.0.3...1.3.1.0

---

# Skua 1.3.0.3
## Released: December 03, 2025

- Packet Interceptor: 
  - now connects to the correct proxy port
- Auras de-serialization:
  -  *should* be fixed

**Full Changelog**: https://github.com/auqw/Skua/compare/1.3.0.2...1.3.0.3

---

# Skua 1.3.0.2
## Released: November 22, 2025

Fixed regex error

Added Wearing bool to ItemBase to check what items we're wearing (It's good for CoreBots not removing cosmetics)

**Full Changelog**: https://github.com/auqw/Skua/compare/1.3.0.1...1.3.0.2

---

# Skua 1.3.0.1
## Released: November 21, 2025

Fixed the interceptor, only using port 5588, which caused us not to be able to connect to servers that didn't use that port
updated most nuget packages which intern could help performance

**Full Changelog**: https://github.com/auqw/Skua/compare/1.3.0.0...1.3.0.1

---

# Skua 1.3.0.0
## Released: November 10, 2025

## What's Changed
* Aura support for scripts and advskills
* Rounded Corners for Windows 11 users
* More memory leaks have been fixed
* Update InventoryItem.cs by @SharpTheNightmare in https://github.com/auqw/Skua/pull/5
* Canuseskill skill check by @SharpTheNightmare in https://github.com/auqw/Skua/pull/6
* CollectionViewer will not have full priority by @SharpTheNightmare in https://github.com/auqw/Skua/pull/7
* Forced skill.auto to false by @SharpTheNightmare in https://github.com/auqw/Skua/pull/11
* Added ProcID And updated Documentation by @SharpTheNightmare in https://github.com/auqw/Skua/pull/12
* added wikilinks (limited) by @SharpTheNightmare in https://github.com/auqw/Skua/pull/18
* added death reset to advskills by @SharpTheNightmare in https://github.com/auqw/Skua/pull/19
* `%LOCALAPPDATA%` config files have moved to `%APPDATA%`. The whole config system had to be written from scratch, so now the problem is that sometimes something randomly goes wrong and resets the config that just will not happen (`%APPDATA%\Skua\ManagerSettings.json` and `%APPDATA%\Skua\ClientSettings.json`)

### If you know how to get your accounts from the `Skua.Manager` config folder, the new format is

From this
```xml
<string>DisplayerName{=}AccName{=}Password</string>
```
to 
```json
"DisplayName{=}AccName{=}Password"
```
e.g., new config for multiple

```json
"ManagedAccounts": [
    "User1{=}User1{=}Password1",
    "User2{=}User2{=}Password2",
    "User3{=}User3{=}Password3"
  ],
```
## TATO JOINED SKUA TEAM!!!!

**Full Changelog**: https://github.com/auqw/Skua/compare/1.2.5.4...1.3.0.0

---


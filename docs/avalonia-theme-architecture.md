# Avalonia Theme Architecture

This note explains how the Avalonia client builds and applies theme state. The main code paths are:

- `Skua.App.Avalonia/App.axaml`
- `Skua.App.Avalonia/App.axaml.cs`
- `Skua.Shared.Avalonia/Services/AvaloniaThemeService.cs`
- `Skua.Shared.Avalonia/Services/ThemeItem.cs`
- `Skua.Shared.Avalonia/Services/ThemeResourceApplicator.cs`
- `Skua.Shared.Avalonia/Theming/Styles/Theme.axaml`

## Color Sources

Avalonia theme colors come from three layers:

1. Static shared resources in `Skua.Shared.Avalonia/Theming/Styles/Theme.axaml`.
   This file defines the base `Skua*` tokens used by shell controls, including `SkuaAccentColor`, `SkuaPageBrush`, `SkuaSurfaceBrush`, `SkuaTextBrush`, and their light/dark variants.
2. Settings-backed theme state in `AvaloniaThemeService`.
   The service owns the current accent, foreground, dark/light mode, and contrast-related state.
3. Runtime injection through `ThemeResourceApplicator`.
   This is the last writer and keeps Skua resources and Material.Avalonia resources in sync.

The default accent fallback is `#FF607D8B` in both `AvaloniaThemeService` and `ThemeResourceApplicator`, so startup has a stable color even before settings are loaded.

## Available Theme Colors And Knobs

At runtime, the editor and services currently expose:

- `Primary` color
- `Secondary` color (kept in lockstep with primary for current WPF parity behavior unless explicitly set)
- `Text on Primary` color
- `Text on Secondary` color (serialized and applied, even when UI mostly edits primary text color)
- `Dark/Light` base theme toggle
- Color adjustment toggle + ratio + contrast preset + selection target (`Primary`, `Secondary`, `All`, `None`)

Built-in preset library is seeded in `BuildDefaultThemesCollection()` in `Skua.Shared.Avalonia/Services/AvaloniaThemeService.cs` and currently includes `Skua`, `RBot`, `Grimoire`, `Purple`, `Phonk`, and `Arrow`.

## Theme Persistence Format

`AvaloniaThemeService` persists theme state through `ISettingsService` using these keys:

- `CurrentTheme` - active theme snapshot, stored as a serialized string
- `DefaultThemes` - built-in preset list, stored as `StringCollection`
- `UserThemes` - user-saved themes, stored as `StringCollection`

The serialized shape is defined by `Skua.Shared.Avalonia/Services/ThemeItem.cs` and is WPF-compatible:

```text
Name,Dark|Light,#primary,#secondary,#primaryFg,#secondaryFg[,useAdj,ratio,contrast,colorSel]
```

Colors are written as `#AARRGGBB`. The extra fields are optional and only present when color adjustment is enabled.

## Runtime Flow

Startup and updates follow the same pattern:

1. `Skua.App.Avalonia/App.axaml` loads `MaterialTheme` plus the shared Skua style files.
2. `Skua.App.Avalonia/App.axaml.cs` registers `IThemeService` as `AvaloniaThemeService`.
3. `AvaloniaThemeService` loads `CurrentTheme`, `DefaultThemes`, and `UserThemes`, then applies the current snapshot.
4. `App.OnFrameworkInitializationCompleted()` subscribes to `ThemeChanged` and `SchemeChanged`.
5. `ApplyThemeFromService()` runs before the first window render so the app starts with the saved variant and accent.
6. Later theme edits flow through `ChangeCustomColor()`, `ChangeScheme()`, `SetCurrentTheme()`, or `SaveTheme()` in `AvaloniaThemeService`.
7. Those methods persist a new snapshot and raise `ThemeChanged` / `SchemeChanged`.
8. The app handlers republish the updated resources through `ThemeResourceApplicator`.

The practical result is that one theme change updates startup state, shell brushes, and Material colors together.

## How `ThemeResourceApplicator` Injects Material Resources

`ThemeResourceApplicator.ApplyAccentBrushes()` writes directly into `Application.Resources` and, when needed, into theme dictionaries.

It updates three groups:

- Skua resources such as `SkuaAccentColor`, `SkuaAccentBrush`, `SkuaAccentHoverBrush`, `SkuaSelectionBrush`
- Material palette resources such as `MaterialPrimaryMidColor`, `MaterialSecondaryMidColor`, `MaterialPrimaryForegroundColor`, `MaterialSelectionColor`
- Material.Avalonia theme object properties and methods by reflection, including `BaseTheme`, `PrimaryColor`, `SecondaryColor`, `SetPrimaryColor()`, and `SetSecondaryColor()`

The important mapping rule is: Skua and Material keys are both written on each apply cycle so controls styled by either keyspace stay in sync.

Common mappings:

- `SkuaAccentColor` -> `MaterialPrimaryColor` / `MaterialPrimaryMidColor`
- `SkuaAccentForegroundColor` -> `MaterialPrimaryForegroundColor` / `MaterialPrimaryMidForegroundColor`
- Secondary equivalents -> `MaterialSecondary*`
- `SkuaSelectionBrush` and Material selection/ripple resources are all recomputed from active accent

This is why `ThemeResourceApplicator` sits at the end of the flow: some controls bind to Skua tokens, while Material templates bind to Material tokens. The applicator republishes both so they do not drift.

The `Theme.axaml` resource dictionary is still the baseline source for shell brushes, but runtime values from `AvaloniaThemeService` override the accent-related entries and the selection brush to match the active theme.

## Contributor Notes

- Update `ThemeItem.cs` first if you change the persisted shape.
- Keep `Theme.axaml` and `ThemeResourceApplicator.cs` aligned when adding or renaming resource keys.
- If you add a new theme color, make sure it is written in both the Skua resource namespace and the Material namespace.

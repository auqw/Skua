# Avalonia Color Reference

This is the quick reference for color tokens we can use in Avalonia styles and controls.

## Skua Theme Tokens (Use These First)

Defined in `Skua.Shared.Avalonia/Theming/Styles/Theme.axaml`.

Always prefer these semantic tokens in app styles:

- `SkuaPageBrush`
- `SkuaSurfaceBrush`
- `SkuaSurfaceAltBrush`
- `SkuaBorderBrush`
- `SkuaNavHoverBrush`
- `SkuaTextBrush`
- `SkuaMutedTextBrush`
- `SkuaSelectionBrush`
- `SkuaHoverBrush`
- `SkuaHoverPressedBrush`
- `SkuaNavChipBrush`

Accent and accent-foreground tokens:

- `SkuaAccentColor`
- `SkuaAccentForegroundColor`
- `SkuaAccentBrush`
- `SkuaAccentForegroundBrush`
- `SkuaAccentHoverBrush`
- `SkuaAccentPressedBrush`

Utility tokens:

- `SkuaDangerHoverBrush`
- `SkuaDangerForegroundBrush`

## Default Light/Dark Values

From `Theme.axaml` theme dictionaries:

| Token | Light | Dark |
|---|---|---|
| `SkuaPageBrush` | `#FFFAFAFA` | `#424242` |
| `SkuaSurfaceBrush` | `#FFFFFFFF` | `#4E4E4E` |
| `SkuaSurfaceAltBrush` | `#FFF5F5F5` | `#303030` |
| `SkuaBorderBrush` | `#FFE0E0E0` | `#303030` |
| `SkuaNavHoverBrush` | `#FFECEFF1` | `#303030` |
| `SkuaTextBrush` | `#FF212121` | `#ECEFF4` |
| `SkuaMutedTextBrush` | `#FF616161` | `#C3CAD0` |
| `SkuaSelectionBrush` | `#33607D8B` | `#66607D8B` |
| `SkuaHoverBrush` | `#14000000` | `#1FFFFFFF` |
| `SkuaHoverPressedBrush` | `#22000000` | `#2AFFFFFF` |
| `SkuaNavChipBrush` | `#FFF1F3F4` | `#303030` |

Base accent defaults:

- `SkuaAccentColor`: `#FF607D8B`
- `SkuaAccentForegroundColor`: `#FF000000`

## Material Keys Published At Runtime

`Skua.Shared.Avalonia/Services/ThemeResourceApplicator.cs` republishes Material color resources on every theme apply.

Primary:

- `MaterialPrimaryLightColor`
- `MaterialPrimaryMidColor`
- `MaterialPrimaryDarkColor`
- `MaterialPrimaryLightForegroundColor`
- `MaterialPrimaryMidForegroundColor`
- `MaterialPrimaryForegroundColor`
- `MaterialPrimaryColor`

Secondary:

- `MaterialSecondaryLightColor`
- `MaterialSecondaryMidColor`
- `MaterialSecondaryDarkColor`
- `MaterialSecondaryLightForegroundColor`
- `MaterialSecondaryMidForegroundColor`
- `MaterialSecondaryDarkForegroundColor`
- `MaterialSecondaryColor`

Interaction:

- `MaterialSelectionColor`
- `MaterialFlatButtonClickColor`
- `MaterialFlatButtonRippleColor`

Note: when secondary is not explicitly provided, runtime now mirrors secondary from primary to avoid stale fallback colors.

## Named Accent Options In Service

`AvaloniaThemeService` supports these built-in named accent picks (`AccentMap`):

- `Default` -> `#FF607D8B`
- `Pink` -> `#C9479A`
- `Ocean` -> `#2E6DD8`
- `Forest` -> `#2E9D57`
- `Crimson` -> `#C94747`
- `Blue` -> `#2E6DD8`
- `Green` -> `#2E9D57`
- `Orange` -> `#D8842E`
- `Red` -> `#C94747`
- `Gray` -> `#6E7685`

## Practical Usage Rules

- Prefer `DynamicResource` + `Skua*` semantic tokens in styles and views.
- Do not hardcode color literals in control markup unless there is a one-off visual reason.
- Let `ThemeResourceApplicator` handle Material key synchronization instead of writing `Material*` keys in feature UI code.

## Basic Usage Example (Frontend Customization)

When customizing UI, use `Skua*` named tokens in your control/style markup.
Think of `Material*` keys as runtime plumbing that `ThemeResourceApplicator` keeps in sync for Material templates.

```xml
<!-- Good: app styles use Skua semantic colors -->
<Style Selector="Button.my-feature-action">
  <Setter Property="Background" Value="{DynamicResource SkuaSurfaceAltBrush}" />
  <Setter Property="BorderBrush" Value="{DynamicResource SkuaBorderBrush}" />
  <Setter Property="Foreground" Value="{DynamicResource SkuaTextBrush}" />
</Style>

<Style Selector="Button.my-feature-action:pointerover">
  <Setter Property="Background" Value="{DynamicResource SkuaHoverBrush}" />
</Style>

<Style Selector="Button.my-feature-action:pressed">
  <Setter Property="Background" Value="{DynamicResource SkuaSelectionBrush}" />
  <Setter Property="BorderBrush" Value="{DynamicResource SkuaAccentBrush}" />
</Style>
```

Rule of thumb:

- Feature/UI code: use `Skua*`
- Theme runtime service code: writes `Skua*` and `Material*`
- Avoid setting `MaterialPrimary*`/`MaterialSecondary*` directly in feature controls

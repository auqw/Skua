# Avalonia Style Scoping Guide

This guide explains how styling is organized in the Avalonia projects in this repo, where styles are loaded, and how to add new selectors without creating stale or duplicated rules.

## Where styles are loaded

The two app shells load the shared Avalonia theme files in `Application.Styles`.

- `Skua.App.Avalonia/App.axaml`
- `Skua.Manager.Avalonia/App.axaml`

Both files load the same shared style stack:

- `avares://Avalonia.Controls.ColorPicker/Themes/Fluent/Fluent.xaml`
- `avares://Skua.Shared.Avalonia/Theming/Styles/Theme.axaml`
- `avares://Skua.Shared.Avalonia/Theming/Styles/Controls.axaml`
- `avares://Skua.Shared.Avalonia/Theming/Styles/Navigation.axaml`
- `avares://Skua.Shared.Avalonia/Theming/Styles/Containers.axaml`

`Theme.axaml` is the root shared theme file. It defines the base resource tokens and then loads more focused shared dictionaries:

- `Skua.Shared.Avalonia/Theming/Styles/TextBlocks.axaml`
- `Skua.Shared.Avalonia/Theming/Styles/TextBoxes.axaml`
- `Skua.Shared.Avalonia/Theming/Styles/ComboBoxes.axaml`
- `Skua.Shared.Avalonia/Theming/Styles/ScrollViewers.axaml`

That split matters. Put palette tokens and app-wide shared resources in `Theme.axaml`, then put control-specific rules in the smaller files when possible.

## Scoping convention: `Window.skua`

The main shell convention is to opt a window into shared styling with `Classes="skua"` and then scope most shell rules under `Window.skua`.

Example from `Skua.Manager.Avalonia/Views/MainWindow.axaml`:

```xml
<Window ... Classes="skua">
```

And the shared theme targets that class in `Theme.axaml`:

```xml
<Style Selector="Window.skua">
  <Setter Property="Background" Value="{DynamicResource SkuaPageBrush}" />
  <Setter Property="Foreground" Value="{DynamicResource SkuaTextBrush}" />
  <Setter Property="FontSize" Value="12" />
</Style>
```

This is the safest default pattern for shell-level styling because it keeps the rules inside the intended window scope.

You can see the same idea used for control defaults:

- `Window.skua TextBlock` in `TextBlocks.axaml`
- `Window.skua TextBox` in `TextBoxes.axaml`
- `Window.skua ComboBox` in `ComboBoxes.axaml`
- `Window.skua ScrollViewer` in `ScrollViewers.axaml`
- `Window.skua ListBox`, `Window.skua ListBoxItem`, `Window.skua CheckBox` in `Theme.axaml`

Use the `Window.skua` prefix when the rule should only affect the shell UI. Do not make a rule global unless it really belongs everywhere.

## Class selector patterns

This repo uses type + class selectors for reusable styling hooks.

Typical examples:

- `Button.topbar-action`
- `ToggleButton.app-toggle`
- `ListBox.top-tabs`
- `Border.account-row-list`
- `Grid.group-account-text.tags-visible`
- `Button.icon-muted`

Use this pattern when a control needs a reusable visual role. The class name should describe the role, not the page.

State selectors are then layered on top of the base class:

- `Button.topbar-action:pointerover`
- `ToggleButton.app-toggle:checked`
- `Border.account-row-list.selected`
- `ListBox.top-tabs ListBoxItem:selected`

The repo also uses comma-separated selectors when the same declaration should apply to multiple related roles:

```xml
<Style Selector="Button.danger-icon, Button.icon">
```

This is a good way to avoid duplicated rules when two classes share the same base appearance.

## Template-part selector cautions

Use `/template/` selectors only when you need to reach inside a control template and the public control properties are not enough.

Current examples:

- `MenuItem.top-nav-item /template/ Border#PART_Presenter`
- `MenuItem.top-nav-item:pointerover /template/ ContentPresenter#PART_HeaderPresenter`
- `ListBox.top-tabs ListBoxItem:selected /template/ Border`
- `TabControl.account-groups-switcher /template/ Panel#PART_TabsPanel > Rectangle#PART_Separator`
- `TabControl.account-groups-switcher TabItem /template/ ContentPresenter#PART_ContentPresenter`

These selectors are powerful but fragile. They can break if the template changes in Avalonia or in a third-party theme such as Material.

Practical cautions:

- Prefer normal control properties first.
- Use `/template/` only when the outer selector cannot achieve the look.
- Keep template-part rules near the control that depends on them.
- Verify the part names against the actual template, not just memory.
- Expect these selectors to need maintenance when a theme package updates.

## How to avoid stale or duplicate styles

When adding a new style, work through this checklist:

1. Check whether an existing selector already covers the control, state, or template part.
1. Decide the right scope: global app style, `Window.skua`, or a local component class.
1. Put shared tokens and common resources in `Theme.axaml`, not in a one-off control file.
1. Put reusable control rules in the smallest shared dictionary that still makes sense.
1. Use a descriptive class name when the style is meant for a specific role, such as `top-tabs` or `account-row-list`.
1. Add state selectors only where the state truly changes the visuals.
1. Prefer `DynamicResource` for theme colors so palette changes stay live.
1. Avoid copying the same setters into multiple files if a shared base selector would work.
1. If you need a template-part selector, confirm the control template has not changed.
1. Test the rule in the actual shell window, not just in the previewer.

## Concrete examples from the current code

`Skua.Shared.Avalonia/Theming/Styles/Theme.axaml`

- Defines the shared resource tokens such as `SkuaPageBrush`, `SkuaSurfaceBrush`, `SkuaTextBrush`, and `SkuaAccentBrush`.
- Loads the focused shared style dictionaries.
- Sets shell-wide defaults like `Window.skua`, `Button.skua-button`, and `ToolTip`.

`Skua.Shared.Avalonia/Theming/Styles/Navigation.axaml`

- Styles the manager top tab strip with `ListBox.top-tabs`.
- Uses both class selectors and template-part selectors to keep selected and hover states aligned with the control template.
- Shows the `TabControl.account-groups-switcher` pattern for a component-specific theme override.

`Skua.Shared.Avalonia/Theming/Styles/Controls.axaml`

- Uses compact reusable classes such as `topbar-action`, `app-toggle-compact`, `icon`, `icon-muted`, and `tag-chip`.
- Groups shared declarations for multiple controls with commas.

`Skua.Shared.Avalonia/Theming/Styles/TextBoxes.axaml`

- Applies one shell-scoped default for `Window.skua TextBox`.
- Adds a focused-state override with `Window.skua TextBox:focus`.

`Skua.Manager.Avalonia/Views/MainWindow.axaml`

- Opts the window into the shared shell theme with `Classes="skua"`.
- Uses a local `top-tabs` class on the `ListBox` that is styled in `Navigation.axaml`.

## Rule of thumb

If a style is reusable across the shell, make it class-based and scoped. If it is only for the shell default, use `Window.skua`. If it reaches into a template, treat it as fragile and keep it narrowly targeted.

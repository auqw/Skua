# Avalonia Shared UI Guide

This guide explains how to use `Skua.Shared.Avalonia` for reusable controls, styles, and theme plumbing across the Avalonia apps.

## What belongs in the shared library

Put code here when it is useful in more than one Avalonia app, or when it is a generic building block that should stay stable over time.

Good fits for `Skua.Shared.Avalonia`:

- Reusable controls with no app-specific workflow.
- Shared style dictionaries and theme tokens.
- Small UI helpers that only deal with visual behavior or theme state.
- Runtime theme/resource services used by both shells.

Keep app- or manager-specific code out of the shared library when it depends on a single screen, feature, or business flow.

Good fits for the app or manager projects:

- Page layout and navigation.
- Feature-specific user controls.
- Workflow code tied to one shell.
- Wiring, startup, and screen-level service composition.

## Current folder map

The shared project is organized around three main areas:

- `Controls/*` - reusable Avalonia controls and panels, grouped by feature area such as `Buttons`, `Content`, `Options`, `Settings`, `Shell`, and `Theming`.
- `Theming/Styles/*` - shared style dictionaries and theme selectors such as `Theme.axaml`, `Controls.axaml`, `Navigation.axaml`, `Containers.axaml`, `TextBlocks.axaml`, `TextBoxes.axaml`, `ComboBoxes.axaml`, and `ScrollViewers.axaml`.
- `Services/*` - shared theme and resource services, including `AvaloniaThemeService`, `ThemeItem`, and `ThemeResourceApplicator`.

## Recommended reuse pattern

1. Build the control or style locally in the app or manager project first if the scope is still unclear.
2. Move it into `Skua.Shared.Avalonia` once it is genuinely reusable or duplicated across shells.
3. Keep the shared surface small: expose bindable properties, not app-specific assumptions.
4. Style reusable pieces with class selectors and named roles, not page-only selectors.
5. Load shared dictionaries from both shells so behavior stays consistent.

The practical rule is: if the UI needs app state, keep it app-specific; if it only needs data and theme resources, shared is usually the right home.

## Scoping and theming expectations

Shared UI should be safe by default. Prefer selectors that are narrow enough to avoid affecting unrelated screens.

- Scope shell defaults with `Window.skua` or another explicit opt-in class when the style is meant for the Skua shell only.
- Use `DynamicResource` for theme-driven colors and brushes so theme changes update live.
- Keep template selectors small and local to the control that needs them.
- Avoid global selectors unless the rule is truly universal across both apps.
- If a style depends on a control template, treat it as fragile and verify the template name before extending it.

## Do and don't

Do:

- Do reuse shared controls when the same UI appears in both shells.
- Do keep shared controls generic and parameter-driven.
- Do prefer class-based styles for reusable visual roles.
- Do keep shared theme resources in one place instead of copying them.

Don't:

- Don't move a control into shared just because it feels neat.
- Don't bake one screen's business logic into a reusable control.
- Don't add global selectors for a problem that only exists in one window.
- Don't over-split files unless the split makes reuse or maintenance easier.

When in doubt, favor the smallest shared surface that still removes duplication.

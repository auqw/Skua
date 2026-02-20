using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using System;

namespace Skua.Shared.Avalonia.Services;

public static class ThemeResourceApplicator
{
    public static void ApplyAccentBrushes(Application app, string? accentHex = null, string? accentForegroundHex = null, string fallbackAccentHex = "#7D9AA9")
    {
        Color accent;
        Color accentForeground;
        try
        {
            accent = Color.Parse(string.IsNullOrWhiteSpace(accentHex) ? GetResourceColor(app, "SkuaAccentColor", fallbackAccentHex).ToString() : accentHex);
        }
        catch
        {
            accent = Color.Parse(fallbackAccentHex);
        }

        try
        {
            accentForeground = Color.Parse(string.IsNullOrWhiteSpace(accentForegroundHex) ? GetResourceColor(app, "SkuaAccentForegroundColor", "#FFFFFFFF").ToString() : accentForegroundHex);
        }
        catch
        {
            accentForeground = Color.Parse("#FFFFFFFF");
        }

        Color hover = Lighten(accent, 0.1);
        Color pressed = Darken(accent, 0.12);
        Color accentLight1 = Lighten(accent, 0.2);
        Color accentLight2 = Lighten(accent, 0.35);
        Color accentLight3 = Lighten(accent, 0.5);
        Color accentDark1 = Darken(accent, 0.18);
        Color accentDark2 = Darken(accent, 0.3);
        Color accentDark3 = Darken(accent, 0.42);
        Color selection = Color.FromArgb(110, accent.R, accent.G, accent.B);

        app.Resources["SkuaAccentColor"] = accent;
        app.Resources["SkuaAccentForegroundColor"] = accentForeground;
        app.Resources["SkuaAccentBrush"] = new SolidColorBrush(accent);
        app.Resources["SkuaAccentForegroundBrush"] = new SolidColorBrush(accentForeground);
        app.Resources["SkuaAccentHoverBrush"] = new SolidColorBrush(hover);
        app.Resources["SkuaAccentPressedBrush"] = new SolidColorBrush(pressed);
        app.Resources["SkuaSelectionBrush"] = new SolidColorBrush(selection);

        app.Resources["SystemAccentColor"] = accent;
        app.Resources["SystemAccentColorLight1"] = accentLight1;
        app.Resources["SystemAccentColorLight2"] = accentLight2;
        app.Resources["SystemAccentColorLight3"] = accentLight3;
        app.Resources["SystemAccentColorDark1"] = accentDark1;
        app.Resources["SystemAccentColorDark2"] = accentDark2;
        app.Resources["SystemAccentColorDark3"] = accentDark3;
        app.Resources["SystemAccentColorBrush"] = new SolidColorBrush(accent);
        app.Resources["SystemAccentColorLight1Brush"] = new SolidColorBrush(accentLight1);
        app.Resources["SystemAccentColorLight2Brush"] = new SolidColorBrush(accentLight2);
        app.Resources["SystemAccentColorLight3Brush"] = new SolidColorBrush(accentLight3);
        app.Resources["SystemAccentColorDark1Brush"] = new SolidColorBrush(accentDark1);
        app.Resources["SystemAccentColorDark2Brush"] = new SolidColorBrush(accentDark2);
        app.Resources["SystemAccentColorDark3Brush"] = new SolidColorBrush(accentDark3);

        ApplyFluentAccent(app, accent);
    }

    private static Color GetResourceColor(Application app, string key, string fallbackHex)
    {
        if (app.Resources.TryGetResource(key, ThemeVariant.Default, out object? value))
        {
            if (value is Color c)
                return c;
            if (value is ISolidColorBrush b)
                return b.Color;
        }
        return Color.Parse(fallbackHex);
    }

    private static void ApplyFluentAccent(Application app, Color accent)
    {
        foreach (var style in app.Styles)
        {
            if (style is not FluentTheme fluentTheme)
                continue;

            if (fluentTheme.Palettes.TryGetValue(ThemeVariant.Light, out ColorPaletteResources? light))
                light.Accent = accent;

            if (fluentTheme.Palettes.TryGetValue(ThemeVariant.Dark, out ColorPaletteResources? dark))
                dark.Accent = accent;
        }
    }

    private static Color Lighten(Color color, double amount)
    {
        byte L(byte c) => (byte)Math.Clamp(c + (255 - c) * amount, 0, 255);
        return Color.FromArgb(color.A, L(color.R), L(color.G), L(color.B));
    }

    private static Color Darken(Color color, double amount)
    {
        byte D(byte c) => (byte)Math.Clamp(c * (1 - amount), 0, 255);
        return Color.FromArgb(color.A, D(color.R), D(color.G), D(color.B));
    }
}

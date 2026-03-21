using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using System;
using System.Reflection;

namespace Skua.Shared.Avalonia.Services;

public static class ThemeResourceApplicator
{
    public static void ApplyAccentBrushes(Application app, string? accentHex = null, string? accentForegroundHex = null, string fallbackAccentHex = "#7D9AA9", bool? isDarkTheme = null)
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
        Color switchTrackOn = Color.FromArgb(140, accent.R, accent.G, accent.B);
        Color switchTrackOff = Color.FromArgb(80, pressed.R, pressed.G, pressed.B);

        // Scheme Target mapping from theme editor:
        // Primary -> SkuaAccentColor / SkuaAccentBrush
        // Text on Primary -> SkuaAccentForegroundColor / SkuaAccentForegroundBrush
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
        ApplyMaterialAccent(app, accent, accentForeground, switchTrackOn, switchTrackOff, isDarkTheme);
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

    private static void ApplyMaterialAccent(Application app, Color accent, Color accentForeground, Color switchTrackOn, Color switchTrackOff, bool? isDarkTheme)
    {
        // Provide direct resource overrides that Material styles can consume.
        SetColorResource(app, "MaterialPrimaryColor", accent);
        SetColorResource(app, "MaterialSecondaryColor", accent);
        SetColorResource(app, "MaterialPrimaryMid", accent);
        SetColorResource(app, "MaterialSecondaryMid", accent);
        SetColorResource(app, "PrimaryColor", accent);
        SetColorResource(app, "SecondaryColor", accent);
        SetColorResource(app, "PrimaryLight", Lighten(accent, 0.18));
        SetColorResource(app, "PrimaryMid", accent);
        SetColorResource(app, "PrimaryDark", Darken(accent, 0.18));
        SetColorResource(app, "SecondaryLight", Lighten(accent, 0.18));
        SetColorResource(app, "SecondaryMid", accent);
        SetColorResource(app, "SecondaryDark", Darken(accent, 0.18));

        SetColorResource(app, "SwitchThumbOnBackground", accent);
        SetColorResource(app, "SwitchTrackOnBackground", switchTrackOn);
        SetColorResource(app, "SwitchThumbOffBackground", accentForeground);
        SetColorResource(app, "SwitchTrackOffBackground", switchTrackOff);
        SetColorResource(app, "ClickFeedbackColor", accent);
        SetColorResource(app, "HoverColor", Lighten(accent, 0.12));

        foreach (var style in app.Styles)
        {
            Type type = style.GetType();
            string? fullName = type.FullName;
            if (string.IsNullOrWhiteSpace(fullName) || !fullName.StartsWith("Material.Styles.Themes.", StringComparison.Ordinal))
                continue;

            TrySetThemeColor(style, type, "PrimaryColor", accent);
            TrySetThemeColor(style, type, "SecondaryColor", accent);
            if (isDarkTheme is bool dark)
                TrySetThemeBase(style, type, dark);

            // Handle both instance methods and static extension methods that Material exposes.
            TryInvokeThemeMethod(style, type, "SetPrimaryColor", accent);
            TryInvokeThemeMethod(style, type, "SetSecondaryColor", accent);
            if (isDarkTheme is bool isDark)
                TryInvokeThemeMethod(style, type, "SetBaseTheme", isDark ? "Dark" : "Light");
        }

        app.Resources["SkuaAccentForegroundColor"] = accentForeground;
        app.Resources["SkuaAccentForegroundBrush"] = new SolidColorBrush(accentForeground);
    }

    private static void SetColorResource(Application app, string key, Color color)
    {
        app.Resources[key] = color;
        app.Resources[$"{key}Brush"] = new SolidColorBrush(color);
    }

    private static void TrySetThemeColor(object theme, Type themeType, string propertyName, Color color)
    {
        try
        {
            PropertyInfo? property = themeType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanWrite != true)
                return;

            object? value = CoerceValue(property.PropertyType, color);
            if (value is not null)
                property.SetValue(theme, value);
        }
        catch
        {
            // Best effort: keep app theme application resilient across Material versions.
        }
    }

    private static void TrySetThemeBase(object theme, Type themeType, bool isDark)
    {
        try
        {
            PropertyInfo? property = themeType.GetProperty("BaseTheme", BindingFlags.Public | BindingFlags.Instance);
            if (property?.CanWrite != true)
                return;

            object? value = CoerceValue(property.PropertyType, isDark ? "Dark" : "Light");
            if (value is not null)
                property.SetValue(theme, value);
        }
        catch
        {
            // Best effort: keep app theme application resilient across Material versions.
        }
    }

    private static void TryInvokeThemeMethod(object theme, Type themeType, string methodName, object value)
    {
        try
        {
            MethodInfo[] methods = themeType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            foreach (MethodInfo method in methods)
            {
                if (!string.Equals(method.Name, methodName, StringComparison.Ordinal))
                    continue;

                ParameterInfo[] parameters = method.GetParameters();
                if (!method.IsStatic && parameters.Length == 1)
                {
                    object? arg = CoerceValue(parameters[0].ParameterType, value);
                    if (arg is null)
                        continue;
                    method.Invoke(theme, [arg]);
                    return;
                }

                if (method.IsStatic && parameters.Length == 2 && parameters[0].ParameterType.IsInstanceOfType(theme))
                {
                    object? arg = CoerceValue(parameters[1].ParameterType, value);
                    if (arg is null)
                        continue;
                    method.Invoke(null, [theme, arg]);
                    return;
                }
            }
        }
        catch
        {
            // Best effort: keep app theme application resilient across Material versions.
        }
    }

    private static object? CoerceValue(Type targetType, object value)
    {
        if (targetType.IsInstanceOfType(value))
            return value;

        if (targetType == typeof(Color) && value is string colorText)
        {
            try
            {
                return Color.Parse(colorText);
            }
            catch
            {
                return null;
            }
        }

        if (targetType == typeof(string))
            return value.ToString();

        if (targetType.IsEnum)
        {
            string? name = value.ToString();
            if (string.IsNullOrWhiteSpace(name))
                return null;

            try
            {
                return Enum.Parse(targetType, name, ignoreCase: true);
            }
            catch
            {
                return null;
            }
        }

        ConstructorInfo? colorCtor = targetType.GetConstructor([typeof(Color)]);
        if (colorCtor is not null && value is Color color)
            return colorCtor.Invoke([color]);

        ConstructorInfo? stringCtor = targetType.GetConstructor([typeof(string)]);
        if (stringCtor is not null)
            return stringCtor.Invoke([value.ToString() ?? string.Empty]);

        return null;
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

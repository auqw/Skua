using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Reflection;

namespace Skua.Shared.Avalonia.Services;

public static class ThemeResourceApplicator
{
    public static void ApplyAccentBrushes(Application app, string? accentHex = null, string? accentForegroundHex = null, string fallbackAccentHex = "#FF607D8B", bool? isDarkTheme = null)
    {
        Color accent = ParseColorSafe(accentHex, GetResourceColor(app, "SkuaAccentColor", fallbackAccentHex));
        Color accentForeground = ParseColorSafe(accentForegroundHex, GetResourceColor(app, "SkuaAccentForegroundColor", "#FF000000"));

        app.Resources["SkuaAccentColor"] = accent;
        app.Resources["SkuaAccentForegroundColor"] = accentForeground;
        app.Resources["SkuaAccentBrush"] = new SolidColorBrush(accent);
        app.Resources["SkuaAccentForegroundBrush"] = new SolidColorBrush(accentForeground);
        app.Resources["SkuaAccentHoverBrush"] = new SolidColorBrush(Lighten(accent, 0.1));
        app.Resources["SkuaAccentPressedBrush"] = new SolidColorBrush(Darken(accent, 0.12));

        Color selectionLight = Color.FromArgb(0x33, accent.R, accent.G, accent.B);
        Color selectionDark = Color.FromArgb(0x66, accent.R, accent.G, accent.B);
        if (app.Resources.ThemeDictionaries.TryGetValue(ThemeVariant.Light, out IThemeVariantProvider? selLightProvider) && selLightProvider is IResourceDictionary selLightDict)
            selLightDict["SkuaSelectionBrush"] = new SolidColorBrush(selectionLight);
        if (app.Resources.ThemeDictionaries.TryGetValue(ThemeVariant.Dark, out IThemeVariantProvider? selDarkProvider) && selDarkProvider is IResourceDictionary selDarkDict)
            selDarkDict["SkuaSelectionBrush"] = new SolidColorBrush(selectionDark);

        ApplyMaterialPaletteResources(app, accent, accentForeground);
        TrySetMaterialAccent(app, accent);

        if (isDarkTheme is bool dark)
            TrySetMaterialBaseTheme(app, dark);
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

    private static Color ParseColorSafe(string? value, Color fallback)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            try
            {
                return Color.Parse(value);
            }
            catch
            {
                return fallback;
            }
        }

        return fallback;
    }

    private static void TrySetMaterialBaseTheme(Application app, bool isDark)
    {
        foreach (object style in app.Styles)
        {
            Type type = style.GetType();
            string? fullName = type.FullName;
            if (string.IsNullOrWhiteSpace(fullName) || !fullName.StartsWith("Material.Styles.Themes.", StringComparison.Ordinal))
                continue;

            try
            {
                PropertyInfo? property = type.GetProperty("BaseTheme", BindingFlags.Public | BindingFlags.Instance);
                if (property?.CanWrite == true)
                    property.SetValue(style, Enum.Parse(property.PropertyType, isDark ? "Dark" : "Light", ignoreCase: true));
            }
            catch
            {
                // Best effort: keep app theme application resilient across Material versions.
            }
        }
    }

    private static void TrySetMaterialAccent(Application app, Color accent)
    {
        foreach (object style in app.Styles)
        {
            Type type = style.GetType();
            string? fullName = type.FullName;
            if (string.IsNullOrWhiteSpace(fullName) || !fullName.StartsWith("Material.Styles.Themes.", StringComparison.Ordinal))
                continue;

            TrySetThemeColor(style, type, "PrimaryColor", accent);
            TrySetThemeColor(style, type, "SecondaryColor", accent);
            TrySetThemeColor(style, type, "PrimaryMid", accent);
            TrySetThemeColor(style, type, "SecondaryMid", accent);
            TryInvokeThemeMethod(style, type, "SetPrimaryColor", accent);
            TryInvokeThemeMethod(style, type, "SetSecondaryColor", accent);
        }
    }

    private static void ApplyMaterialPaletteResources(Application app, Color accent, Color accentForeground)
    {
        Color light = Lighten(accent, 0.1);
        Color dark = Darken(accent, 0.12);

        // Material.Avalonia uses MaterialPrimary*/MaterialSecondary* resources in templates.
        SetColorResource(app, "MaterialPrimaryLightColor", light);
        SetColorResource(app, "MaterialPrimaryMidColor", accent);
        SetColorResource(app, "MaterialPrimaryDarkColor", dark);
        SetColorResource(app, "MaterialSecondaryLightColor", light);
        SetColorResource(app, "MaterialSecondaryMidColor", accent);
        SetColorResource(app, "MaterialSecondaryDarkColor", dark);

        SetColorResource(app, "MaterialPrimaryLightForegroundColor", accentForeground);
        SetColorResource(app, "MaterialPrimaryMidForegroundColor", accentForeground);
        SetColorResource(app, "MaterialPrimaryForegroundColor", accentForeground);
        SetColorResource(app, "MaterialSecondaryLightForegroundColor", accentForeground);
        SetColorResource(app, "MaterialSecondaryMidForegroundColor", accentForeground);
        SetColorResource(app, "MaterialSecondaryDarkForegroundColor", accentForeground);

        SetColorResource(app, "MaterialPrimaryColor", accent);
        SetColorResource(app, "MaterialSecondaryColor", accent);
        SetColorResource(app, "MaterialSelectionColor", Color.FromArgb(0x55, accent.R, accent.G, accent.B));
        SetColorResource(app, "MaterialFlatButtonClickColor", Color.FromArgb(0x66, accent.R, accent.G, accent.B));
        SetColorResource(app, "MaterialFlatButtonRippleColor", Color.FromArgb(0x33, accent.R, accent.G, accent.B));
    }

    private static void SetColorResource(Application app, string colorKey, Color color)
    {
        app.Resources[colorKey] = color;

        if (colorKey.EndsWith("Color", StringComparison.Ordinal))
        {
            string brushKey = colorKey[..^"Color".Length] + "Brush";
            app.Resources[brushKey] = new SolidColorBrush(color);
        }
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
            // Best effort across Material versions.
        }
    }

    private static void TryInvokeThemeMethod(object theme, Type themeType, string methodName, Color color)
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
                    object? arg = CoerceValue(parameters[0].ParameterType, color);
                    if (arg is null)
                        continue;
                    method.Invoke(theme, [arg]);
                    return;
                }

                if (method.IsStatic && parameters.Length == 2 && parameters[0].ParameterType.IsInstanceOfType(theme))
                {
                    object? arg = CoerceValue(parameters[1].ParameterType, color);
                    if (arg is null)
                        continue;
                    method.Invoke(null, [theme, arg]);
                    return;
                }
            }
        }
        catch
        {
            // Best effort across Material versions.
        }
    }

    private static object? CoerceValue(Type targetType, Color color)
    {
        if (targetType == typeof(Color))
            return color;

        ConstructorInfo? colorCtor = targetType.GetConstructor([typeof(Color)]);
        if (colorCtor is not null)
            return colorCtor.Invoke([color]);

        if (targetType == typeof(string))
            return color.ToString();

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

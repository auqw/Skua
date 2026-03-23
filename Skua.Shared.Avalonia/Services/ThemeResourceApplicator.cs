using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using System;
using System.Reflection;

namespace Skua.Shared.Avalonia.Services;

public static class ThemeResourceApplicator
{
    public static void ApplyAccentBrushes(
        Application app,
        string? accentHex = null,
        string? accentForegroundHex = null,
        string? secondaryHex = null,
        string? secondaryForegroundHex = null,
        string fallbackAccentHex = "#FF607D8B",
        bool? isDarkTheme = null)
    {
        // Keep both Skua* and Material* resources in sync because custom shell controls
        // and Material templates read different keys at runtime (see Theme.axaml + Controls.axaml).
        Color primary = ParseColorSafe(accentHex, GetResourceColor(app, "SkuaAccentColor", fallbackAccentHex));
        Color primaryForeground = ParseColorSafe(accentForegroundHex, GetResourceColor(app, "SkuaAccentForegroundColor", "#FF000000"));
        
        // Note: We do not currently support any custom secondary colour via UI. This is a holdover from the legacy WPF Version which had some logic for secondary colour but no UI for it.
        // If no explicit secondary is provided, mirror the current primary instead of
        // inheriting any stale Material secondary value left in resources.
        Color secondary = ParseColorSafe(secondaryHex, primary);
        Color secondaryForeground = ParseColorSafe(secondaryForegroundHex, primaryForeground);

        app.Resources["SkuaAccentColor"] = primary;
        app.Resources["SkuaAccentForegroundColor"] = primaryForeground;
        app.Resources["SkuaAccentBrush"] = new SolidColorBrush(primary);
        app.Resources["SkuaAccentForegroundBrush"] = new SolidColorBrush(primaryForeground);
        app.Resources["SkuaAccentHoverBrush"] = new SolidColorBrush(Lighten(primary, 0.1));
        app.Resources["SkuaAccentPressedBrush"] = new SolidColorBrush(Darken(primary, 0.12));

        Color selectionLight = Color.FromArgb(0x33, primary.R, primary.G, primary.B);
        Color selectionDark = Color.FromArgb(0x66, primary.R, primary.G, primary.B);
        bool useDarkSelection = isDarkTheme ?? false;
        app.Resources["SkuaSelectionBrush"] = new SolidColorBrush(useDarkSelection ? selectionDark : selectionLight);
        if (app.Resources.ThemeDictionaries.TryGetValue(ThemeVariant.Light, out IThemeVariantProvider? selLightProvider) && selLightProvider is IResourceDictionary selLightDict)
            selLightDict["SkuaSelectionBrush"] = new SolidColorBrush(selectionLight);
        if (app.Resources.ThemeDictionaries.TryGetValue(ThemeVariant.Dark, out IThemeVariantProvider? selDarkProvider) && selDarkProvider is IResourceDictionary selDarkDict)
            selDarkDict["SkuaSelectionBrush"] = new SolidColorBrush(selectionDark);

        // Republish full Material palette on every change so controls that cache specific
        // MaterialPrimary*/MaterialSecondary* keys refresh consistently.
        ApplyMaterialPaletteResources(app, primary, primaryForeground, secondary, secondaryForeground);
        TrySetMaterialAccent(app, primary, secondary);

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

    private static void TrySetMaterialAccent(Application app, Color primary, Color secondary)
    {
        foreach (object style in app.Styles)
        {
            Type type = style.GetType();
            string? fullName = type.FullName;
            if (string.IsNullOrWhiteSpace(fullName) || !fullName.StartsWith("Material.Styles.Themes.", StringComparison.Ordinal))
                continue;

            TrySetThemeColor(style, type, "PrimaryColor", primary);
            TrySetThemeColor(style, type, "SecondaryColor", secondary);
            TrySetThemeColor(style, type, "PrimaryMid", primary);
            TrySetThemeColor(style, type, "SecondaryMid", secondary);
            TryInvokeThemeMethod(style, type, "SetPrimaryColor", primary);
            TryInvokeThemeMethod(style, type, "SetSecondaryColor", secondary);
        }
    }

    private static void ApplyMaterialPaletteResources(Application app, Color primary, Color primaryForeground, Color secondary, Color secondaryForeground)
    {
        Color primaryLight = Lighten(primary, 0.1);
        Color primaryDark = Darken(primary, 0.12);
        Color secondaryLight = Lighten(secondary, 0.1);
        Color secondaryDark = Darken(secondary, 0.12);

        // Material.Avalonia uses MaterialPrimary*/MaterialSecondary* resources in templates.
        SetColorResource(app, "MaterialPrimaryLightColor", primaryLight);
        SetColorResource(app, "MaterialPrimaryMidColor", primary);
        SetColorResource(app, "MaterialPrimaryDarkColor", primaryDark);
        SetColorResource(app, "MaterialSecondaryLightColor", secondaryLight);
        SetColorResource(app, "MaterialSecondaryMidColor", secondary);
        SetColorResource(app, "MaterialSecondaryDarkColor", secondaryDark);

        SetColorResource(app, "MaterialPrimaryLightForegroundColor", primaryForeground);
        SetColorResource(app, "MaterialPrimaryMidForegroundColor", primaryForeground);
        SetColorResource(app, "MaterialPrimaryForegroundColor", primaryForeground);
        SetColorResource(app, "MaterialSecondaryLightForegroundColor", secondaryForeground);
        SetColorResource(app, "MaterialSecondaryMidForegroundColor", secondaryForeground);
        SetColorResource(app, "MaterialSecondaryDarkForegroundColor", secondaryForeground);

        SetColorResource(app, "MaterialPrimaryColor", primary);
        SetColorResource(app, "MaterialSecondaryColor", secondary);
        SetColorResource(app, "MaterialSelectionColor", Color.FromArgb(0x55, primary.R, primary.G, primary.B));
        SetColorResource(app, "MaterialFlatButtonClickColor", Color.FromArgb(0x66, primary.R, primary.G, primary.B));
        SetColorResource(app, "MaterialFlatButtonRippleColor", Color.FromArgb(0x33, primary.R, primary.G, primary.B));
    }

    private static void SetColorResource(Application app, string colorKey, Color color)
    {
        // Some templates bind Color keys, others bind Brush keys. Writing both avoids
        // hidden drift when a control expects one representation.
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

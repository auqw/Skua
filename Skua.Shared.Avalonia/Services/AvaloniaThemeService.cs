using Avalonia.Media;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;

namespace Skua.Shared.Avalonia.Services;

public class AvaloniaThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;
    private static readonly Dictionary<string, Color> AccentMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Default"] = Color.Parse("#FF607D8B"),
        ["Pink"] = Color.Parse("#C9479A"),
        ["Ocean"] = Color.Parse("#2E6DD8"),
        ["Forest"] = Color.Parse("#2E9D57"),
        ["Crimson"] = Color.Parse("#C94747"),
        ["Blue"] = Color.Parse("#2E6DD8"),
        ["Green"] = Color.Parse("#2E9D57"),
        ["Orange"] = Color.Parse("#D8842E"),
        ["Red"] = Color.Parse("#C94747"),
        ["Gray"] = Color.Parse("#6E7685")
    };

    public AvaloniaThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        // Theme state is loaded from shared keys only.

        // Seed DefaultThemes if absent
        StringCollection? defaultThemesColl = _settingsService.Get<StringCollection>("DefaultThemes");
        if (defaultThemesColl is null || defaultThemesColl.Count == 0)
        {
            defaultThemesColl = BuildDefaultThemesCollection();
            _settingsService.Set("DefaultThemes", defaultThemesColl);
        }
        foreach (string? csv in defaultThemesColl)
        {
            ThemeItem? t = ThemeItem.FromString(csv);
            if (t is not null)
                Presets.Add(t.Name);
        }

        // Load UserThemes
        StringCollection? userThemesColl = _settingsService.Get<StringCollection>("UserThemes");
        if (userThemesColl is not null)
        {
            foreach (string? csv in userThemesColl)
            {
                ThemeItem? t = ThemeItem.FromString(csv);
                if (t is not null)
                    UserThemes.Add(t.Name);
            }
        }

        // Load current theme or fall back to first default
        string? currentThemeCsv = _settingsService.Get<string>("CurrentTheme");
        ThemeItem? current = ThemeItem.FromString(currentThemeCsv);

        if (current is null)
        {
            string? firstDefault = defaultThemesColl.Count > 0 ? defaultThemesColl[0] : null;
            current = ThemeItem.FromString(firstDefault) ?? new ThemeItem();
            _settingsService.Set("CurrentTheme", current.ConvertToString());
        }

        ApplyThemeItem(current);
    }

    public event ThemeChangedEventHandler? ThemeChanged;
    public event SchemeChangedEventHandler? SchemeChanged;

    public List<object> Presets { get; } = [];
    public List<object> UserThemes { get; } = [];
    public IEnumerable<object> ColorSelectionValues { get; } = ["Pink", "Blue", "Green", "Orange", "Red", "Gray"];
    private object _colorSelectionValue = "Pink";
    public object ColorSelectionValue
    {
        get => _colorSelectionValue;
        set
        {
            if (Equals(_colorSelectionValue, value))
                return;
            _colorSelectionValue = value;
            ChangeCustomColor(value);
        }
    }
    public IEnumerable<object> ContrastValues { get; } = ["Low", "Medium", "High"];
    private object _contrastValue = "Medium";
    public object ContrastValue
    {
        get => _contrastValue;
        set
        {
            if (Equals(_contrastValue, value))
                return;
            _contrastValue = value;
            ApplyColorAdjustmentIfEnabled();
            SaveCurrentThemeSnapshot();
        }
    }

    private float _desiredContrastRatio = 4.5f;
    public float DesiredContrastRatio
    {
        get => _desiredContrastRatio;
        set
        {
            if (Math.Abs(_desiredContrastRatio - value) < 0.001f)
                return;
            _desiredContrastRatio = value;
            ApplyColorAdjustmentIfEnabled();
            SaveCurrentThemeSnapshot();
        }
    }

    private bool _isColorAdjusted;
    public bool IsColorAdjusted
    {
        get => _isColorAdjusted;
        set
        {
            if (_isColorAdjusted == value)
                return;
            _isColorAdjusted = value;
            ApplyColorAdjustmentIfEnabled(forceRefreshWhenOff: true);
        }
    }

    private bool _isDarkTheme;
    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            if (_isDarkTheme == value)
                return;
            _isDarkTheme = value;
            SaveCurrentThemeSnapshot();
            ThemeChanged?.Invoke(SelectedColor);
        }
    }

    private Color _primaryColor;
    private Color _primaryForegroundColor;
    private Color _selectedColor;
    private bool _suppressSelectedColorApply;

    public object? SelectedColor
    {
        get => _selectedColor;
        set
        {
            Color color = ResolveAccentColor(value);
            if (_selectedColor == color)
                return;

            _selectedColor = color;
            if (_suppressSelectedColorApply)
                return;

            ApplyColorToActiveScheme(color);
        }
    }

    public ColorScheme ActiveScheme { get; set; } = ColorScheme.Primary;

    /// <summary>Returns the current accent foreground color as a hex string for use by ThemeResourceApplicator.</summary>
    public string ForegroundHex => $"#{_primaryForegroundColor.A:X2}{_primaryForegroundColor.R:X2}{_primaryForegroundColor.G:X2}{_primaryForegroundColor.B:X2}";

    public void ApplyBaseTheme(bool isDark)
    {
        IsDarkTheme = isDark;
    }

    public void ChangeCustomColor(object? obj)
    {
        Color color = ResolveAccentColor(obj);
        SetSelectedColorSilently(color);
        if (ActiveScheme == ColorScheme.PrimaryForeground && IsColorAdjusted)
            return;

        ApplyColorToActiveScheme(color);
    }

    public void ChangeScheme(ColorScheme scheme)
    {
        ActiveScheme = scheme;
        Color selected = scheme == ColorScheme.PrimaryForeground ? _primaryForegroundColor : _primaryColor;
        SetSelectedColorSilently(selected);
        SchemeChanged?.Invoke(scheme, selected);
    }

    public void SaveTheme(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        ThemeItem snapshot = new()
        {
            Name = name,
            IsDarkTheme = _isDarkTheme,
            PrimaryColor = _primaryColor,
            SecondaryColor = _primaryColor,
            PrimaryForegroundColor = _primaryForegroundColor,
            SecondaryForegroundColor = _primaryForegroundColor,
            UseColorAdjustment = _isColorAdjusted,
            DesiredContrastRatio = _desiredContrastRatio,
            ContrastValue = _contrastValue?.ToString() ?? "Medium"
        };
        string csv = snapshot.ConvertToString();

        // Update UserThemes StringCollection in settings
        StringCollection coll = _settingsService.Get<StringCollection>("UserThemes") ?? new StringCollection();
        StringCollection updated = new();
        foreach (string? entry in coll)
        {
            if (entry is null) continue;
            ThemeItem? existing = ThemeItem.FromString(entry);
            if (existing is not null && string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;
            updated.Add(entry);
        }
        updated.Add(csv);
        _settingsService.Set("UserThemes", updated);

        // Update in-memory list
        UserThemes.RemoveAll(t => string.Equals(t?.ToString(), name, StringComparison.OrdinalIgnoreCase));
        UserThemes.Add(name);

        // Write CurrentTheme with the name
        SaveCurrentThemeSnapshot(name);
    }

    public void SetCurrentTheme(object? theme)
    {
        if (theme is null)
            return;

        if (theme is string themeString)
        {
            // Try to resolve by name from UserThemes or DefaultThemes
            string? resolved = FindThemeCsvByName(themeString);
            if (resolved is not null && TryApplySerializedTheme(resolved))
                return;

            // Try as raw CSV
            if (TryApplySerializedTheme(themeString))
                return;
        }

        Color resolved2 = ResolveAccentColor(theme);
        _primaryColor = resolved2;
        _colorSelectionValue = FindClosestSelectionKey(resolved2);
        if (ActiveScheme == ColorScheme.Primary)
            SetSelectedColorSilently(resolved2);
        SaveCurrentThemeSnapshot();

        ThemeChanged?.Invoke(resolved2);
        SchemeChanged?.Invoke(ColorScheme.Primary, resolved2);
    }

    public void RemoveTheme(object? theme)
    {
        if (theme is null)
            return;

        string? name = theme.ToString();
        if (string.IsNullOrWhiteSpace(name))
            return;

        // Remove from UserThemes StringCollection in settings
        StringCollection coll = _settingsService.Get<StringCollection>("UserThemes") ?? new StringCollection();
        StringCollection updated = new();
        foreach (string? entry in coll)
        {
            if (entry is null) continue;
            ThemeItem? existing = ThemeItem.FromString(entry);
            if (existing is not null && string.Equals(existing.Name, name, StringComparison.OrdinalIgnoreCase))
                continue;
            updated.Add(entry);
        }
        _settingsService.Set("UserThemes", updated);

        UserThemes.RemoveAll(t => string.Equals(t?.ToString(), name, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyThemeItem(ThemeItem item)
    {
        _isDarkTheme = item.IsDarkTheme;
        _isColorAdjusted = item.UseColorAdjustment;
        _desiredContrastRatio = item.DesiredContrastRatio;
        _contrastValue = item.ContrastValue;
        _primaryColor = item.PrimaryColor;
        _primaryForegroundColor = item.UseColorAdjustment
            ? ComputeForeground(item.PrimaryColor, GetTargetContrastRatio())
            : item.PrimaryForegroundColor;
        _colorSelectionValue = FindClosestSelectionKey(_primaryColor);
        ActiveScheme = ColorScheme.Primary;
        SetSelectedColorSilently(_primaryColor);
    }

    private void LegacyDevNoOp()
    {
        // Intentionally empty.
        // Note: ISettingsService has no Remove — old keys are left in ExtensionData and ignored going forward.
    }

    private string? FindThemeCsvByName(string name)
    {
        StringCollection? userThemes = _settingsService.Get<StringCollection>("UserThemes");
        if (userThemes is not null)
        {
            foreach (string? csv in userThemes)
            {
                ThemeItem? t = ThemeItem.FromString(csv);
                if (t is not null && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                    return csv;
            }
        }

        StringCollection? defaultThemes = _settingsService.Get<StringCollection>("DefaultThemes");
        if (defaultThemes is not null)
        {
            foreach (string? csv in defaultThemes)
            {
                ThemeItem? t = ThemeItem.FromString(csv);
                if (t is not null && string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase))
                    return csv;
            }
        }

        return null;
    }

    private static StringCollection BuildDefaultThemesCollection()
    {
        return new StringCollection
        {
            "Skua,Dark,#FF607D8B,#FF607D8B,#FF000000,#FF000000,true,4.5,Medium,All",
            "RBot,Light,#FF9C934E,#FF9C934E,#FF000000,#FF000000",
            "Grimoire,Dark,#FFCC1F41,#FFCC1F41,#FFFFFFFF,#FFFFFFFF",
            "Purple,Dark,#FF9651D6,#FF9651D6,#FFFFFFFF,#FFFFFFFF,true,4.5,Medium,All",
            "Phonk,Dark,#FFFE27D7,#FF607D8B,#FF000000,#FF000000,true,4.5,Medium,All"
        };
    }

    private static Color ResolveAccentColor(object? obj)
    {
        if (obj is string s)
        {
            if (AccentMap.TryGetValue(s, out Color mapped))
                return mapped;
            try
            {
                return Color.Parse(s);
            }
            catch
            {
                return Color.Parse("#FF607D8B");
            }
        }

        if (obj is Color c)
            return c;

        return Color.Parse("#FF607D8B");
    }

    private static string FindClosestSelectionKey(Color target)
    {
        foreach (KeyValuePair<string, Color> kvp in AccentMap)
        {
            if (kvp.Value == target)
                return kvp.Key;
        }

        return "Pink";
    }

    private static Color ParseColor(string? value, Color fallback)
    {
        try
        {
            return Color.Parse(string.IsNullOrWhiteSpace(value) ? fallback.ToString() : value);
        }
        catch
        {
            return fallback;
        }
    }

    private void SetSelectedColorSilently(Color color)
    {
        _suppressSelectedColorApply = true;
        _selectedColor = color;
        _suppressSelectedColorApply = false;
    }

    private void ApplyColorToActiveScheme(Color color)
    {
        if (ActiveScheme == ColorScheme.PrimaryForeground)
        {
            _primaryForegroundColor = color;
            SaveCurrentThemeSnapshot();
        }
        else
        {
            _primaryColor = color;
            _colorSelectionValue = FindClosestSelectionKey(color);
            if (IsColorAdjusted)
            {
                _primaryForegroundColor = ComputeForeground(color, GetTargetContrastRatio());
                SetSelectedColorSilently(_primaryColor);
            }
            SaveCurrentThemeSnapshot();
        }

        if (IsColorAdjusted && ActiveScheme == ColorScheme.Primary)
        {
            ThemeChanged?.Invoke(_primaryColor);
            return;
        }

        SchemeChanged?.Invoke(ActiveScheme, color);
    }

    private void ApplyColorAdjustmentIfEnabled(bool forceRefreshWhenOff = false)
    {
        if (IsColorAdjusted)
        {
            _primaryForegroundColor = ComputeForeground(_primaryColor, GetTargetContrastRatio());
            SaveCurrentThemeSnapshot();
            ThemeChanged?.Invoke(_primaryColor);
        }
        else if (forceRefreshWhenOff)
        {
            SaveCurrentThemeSnapshot();
            ThemeChanged?.Invoke(_primaryColor);
        }
    }

    private bool TryApplySerializedTheme(string value)
    {
        ThemeItem? item = ThemeItem.FromString(value);
        if (item is null)
            return false;

        ApplyThemeItem(item);
        SaveCurrentThemeSnapshot(item.Name);

        ThemeChanged?.Invoke(_primaryColor);
        SchemeChanged?.Invoke(ColorScheme.Primary, _primaryColor);
        return true;
    }

    private void SaveCurrentThemeSnapshot(string? name = null)
    {
        string themeName = !string.IsNullOrWhiteSpace(name) ? name : "Skua";
        ThemeItem snapshot = new()
        {
            Name = themeName,
            IsDarkTheme = _isDarkTheme,
            PrimaryColor = _primaryColor,
            SecondaryColor = _primaryColor,
            PrimaryForegroundColor = _primaryForegroundColor,
            SecondaryForegroundColor = _primaryForegroundColor,
            UseColorAdjustment = _isColorAdjusted,
            DesiredContrastRatio = _desiredContrastRatio,
            ContrastValue = _contrastValue?.ToString() ?? "Medium"
        };
        _settingsService.Set("CurrentTheme", snapshot.ConvertToString());
    }

    private float GetTargetContrastRatio()
    {
        float baseRatio = Math.Clamp(DesiredContrastRatio, 1f, 21f);
        float factor = (_contrastValue?.ToString() ?? "Medium").ToLowerInvariant() switch
        {
            "low" => 0.8f,
            "high" => 1.25f,
            _ => 1.0f
        };
        return Math.Clamp(baseRatio * factor, 1f, 21f);
    }

    private static Color ComputeForeground(Color background, float targetContrast)
    {
        double lb = RelativeLuminance(background);
        double r = Math.Clamp(targetContrast, 1d, 21d);

        bool chooseLighter = lb < 0.5;
        double lf = chooseLighter
            ? (r * (lb + 0.05)) - 0.05
            : ((lb + 0.05) / r) - 0.05;

        lf = Math.Clamp(lf, 0d, 1d);
        byte c = LuminanceToSrgbByte(lf);
        return Color.FromArgb(255, c, c, c);
    }

    private static double RelativeLuminance(Color color)
    {
        static double ToLinear(byte c)
        {
            double s = c / 255.0;
            return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        double r = ToLinear(color.R);
        double g = ToLinear(color.G);
        double b = ToLinear(color.B);
        return 0.2126 * r + 0.7152 * g + 0.0722 * b;
    }

    private static byte LuminanceToSrgbByte(double luminance)
    {
        double s = luminance <= 0.0031308
            ? luminance * 12.92
            : 1.055 * Math.Pow(luminance, 1.0 / 2.4) - 0.055;
        return (byte)Math.Clamp(Math.Round(s * 255), 0, 255);
    }
}

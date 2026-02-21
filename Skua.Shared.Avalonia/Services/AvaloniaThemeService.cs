using Avalonia.Media;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Skua.Shared.Avalonia.Services;

public class AvaloniaThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;
    private static readonly Dictionary<string, Color> AccentMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Default"] = Color.Parse("#7D9AA9"),
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
        _isDarkTheme = _settingsService.Get("ManagerIsDarkTheme", true);
        _isColorAdjusted = _settingsService.Get("ManagerIsColorAdjusted", false);
        _desiredContrastRatio = _settingsService.Get("ManagerDesiredContrastRatio", 4.5f);
        _contrastValue = _settingsService.Get("ManagerContrastValue", "Medium");
        _primaryColor = ParseColor(_settingsService.Get("ManagerAccentColor", "#7D9AA9"), Color.Parse("#7D9AA9"));
        _primaryForegroundColor = ParseColor(_settingsService.Get("ManagerAccentForegroundColor", "#FFFFFFFF"), Color.Parse("#FFFFFFFF"));
        if (_isColorAdjusted)
            _primaryForegroundColor = ComputeForeground(_primaryColor, GetTargetContrastRatio());

        SelectedColor = _primaryColor;
        _colorSelectionValue = FindClosestSelectionKey(_primaryColor);
    }

    public event ThemeChangedEventHandler? ThemeChanged;
    public event SchemeChangedEventHandler? SchemeChanged;

    public List<object> Presets { get; } = ["Default", "Ocean", "Forest", "Crimson"];
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
            _settingsService.Set("ManagerContrastValue", value?.ToString() ?? "Medium");
            ApplyColorAdjustmentIfEnabled();
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
            _settingsService.Set("ManagerDesiredContrastRatio", value);
            ApplyColorAdjustmentIfEnabled();
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
            _settingsService.Set("ManagerIsColorAdjusted", value);
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
            _settingsService.Set("ManagerIsDarkTheme", value);
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

        if (!UserThemes.Any(t => string.Equals(t?.ToString(), name, StringComparison.OrdinalIgnoreCase)))
            UserThemes.Add(name);
    }

    public void SetCurrentTheme(object? theme)
    {
        if (theme is null)
            return;

        if (theme is string themeString && TryApplySerializedTheme(themeString))
            return;

        Color resolved = ResolveAccentColor(theme);
        _primaryColor = resolved;
        _settingsService.Set("ManagerAccentColor", resolved.ToString());
        _colorSelectionValue = FindClosestSelectionKey(resolved);
        if (ActiveScheme == ColorScheme.Primary)
            SetSelectedColorSilently(resolved);
        SaveCurrentThemeSnapshot();

        ThemeChanged?.Invoke(resolved);
        SchemeChanged?.Invoke(ColorScheme.Primary, resolved);
    }

    public void RemoveTheme(object? theme)
    {
        if (theme is null)
            return;

        UserThemes.RemoveAll(t => Equals(t, theme));
        if (Equals(SelectedColor, theme))
            SelectedColor = null;
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
                return Color.Parse("#7D9AA9");
            }
        }

        if (obj is Color c)
            return c;

        return Color.Parse("#7D9AA9");
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
            _settingsService.Set("ManagerAccentForegroundColor", color.ToString());
            SaveCurrentThemeSnapshot();
        }
        else
        {
            _primaryColor = color;
            _settingsService.Set("ManagerAccentColor", color.ToString());
            _colorSelectionValue = FindClosestSelectionKey(color);
            if (IsColorAdjusted)
            {
                _primaryForegroundColor = ComputeForeground(color, GetTargetContrastRatio());
                _settingsService.Set("ManagerAccentForegroundColor", _primaryForegroundColor.ToString());
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
            _settingsService.Set("ManagerAccentForegroundColor", _primaryForegroundColor.ToString());
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
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string[] parts = value.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 6)
            return false;

        Color primary = ParseColor(parts[2], _primaryColor);
        Color primaryForeground = ParseColor(parts[4], _primaryForegroundColor);
        bool isDark = parts[1].Equals("Dark", StringComparison.OrdinalIgnoreCase);

        bool useColorAdjustment = false;
        if (parts.Length > 6)
            bool.TryParse(parts[6], out useColorAdjustment);

        float desiredContrastRatio = 4.5f;
        if (parts.Length > 7)
            float.TryParse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture, out desiredContrastRatio);

        string contrastValue = parts.Length > 8 ? parts[8] : "Medium";

        _isDarkTheme = isDark;
        _isColorAdjusted = useColorAdjustment;
        _desiredContrastRatio = desiredContrastRatio <= 0 ? 4.5f : desiredContrastRatio;
        _contrastValue = string.IsNullOrWhiteSpace(contrastValue) ? "Medium" : contrastValue;
        _primaryColor = primary;
        _primaryForegroundColor = useColorAdjustment ? ComputeForeground(primary, GetTargetContrastRatio()) : primaryForeground;
        _colorSelectionValue = FindClosestSelectionKey(_primaryColor);
        ActiveScheme = ColorScheme.Primary;
        SetSelectedColorSilently(_primaryColor);

        _settingsService.Set("ManagerIsDarkTheme", _isDarkTheme);
        _settingsService.Set("ManagerIsColorAdjusted", _isColorAdjusted);
        _settingsService.Set("ManagerDesiredContrastRatio", _desiredContrastRatio);
        _settingsService.Set("ManagerContrastValue", _contrastValue?.ToString() ?? "Medium");
        _settingsService.Set("ManagerAccentColor", _primaryColor.ToString());
        _settingsService.Set("ManagerAccentForegroundColor", _primaryForegroundColor.ToString());
        SaveCurrentThemeSnapshot(parts[0]);

        ThemeChanged?.Invoke(_primaryColor);
        SchemeChanged?.Invoke(ColorScheme.Primary, _primaryColor);
        return true;
    }

    private void SaveCurrentThemeSnapshot(string? name = null)
    {
        string themeName = !string.IsNullOrWhiteSpace(name) ? name : "Skua";
        string baseTheme = IsDarkTheme ? "Dark" : "Light";
        string serialized = $"{themeName},{baseTheme},{_primaryColor},{_primaryColor},{_primaryForegroundColor},{_primaryForegroundColor}";

        if (IsColorAdjusted)
        {
            string ratio = DesiredContrastRatio.ToString(CultureInfo.InvariantCulture);
            serialized += $",true,{ratio},{ContrastValue},All";
        }

        _settingsService.Set("CurrentTheme", serialized);
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

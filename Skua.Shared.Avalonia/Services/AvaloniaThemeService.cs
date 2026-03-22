using Avalonia.Media;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Skua.Shared.Avalonia.Services;

public class AvaloniaThemeService : IThemeService, INotifyPropertyChanged
{
    private const string ColorSelectionNone = "None";
    private const string ColorSelectionPrimary = "Primary";
    private const string ColorSelectionSecondary = "Secondary";
    private const string ColorSelectionAll = "All";

    private const string ContrastNone = "None";
    private const string ContrastLow = "Low";
    private const string ContrastMedium = "Medium";
    private const string ContrastHigh = "High";

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

        // These keys are shared with WPF theme snapshots, so we keep one storage shape
        // across both UI stacks (see ThemeItem.FromString/ConvertToString).
        // Related files:
        // - Skua.Shared.Avalonia/Services/ThemeItem.cs
        // - Skua.Shared.Avalonia/Services/ThemeResourceApplicator.cs
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
    public event PropertyChangedEventHandler? PropertyChanged;

    public List<object> Presets { get; } = [];
    public List<object> UserThemes { get; } = [];
    public IEnumerable<object> ColorSelectionValues { get; } = [ColorSelectionNone, ColorSelectionPrimary, ColorSelectionSecondary, ColorSelectionAll];
    private object _colorSelectionValue = ColorSelectionAll;
    public object ColorSelectionValue
    {
        get => _colorSelectionValue;
        set
        {
            string normalized = NormalizeColorSelectionValue(value);
            if (Equals(_colorSelectionValue, normalized))
                return;
            _colorSelectionValue = normalized;
            OnPropertyChanged();
            if (IsColorAdjusted)
                ApplyColorAdjustmentIfEnabled();
            else
                SaveCurrentThemeSnapshot();
        }
    }
    public IEnumerable<object> ContrastValues { get; } = [ContrastNone, ContrastLow, ContrastMedium, ContrastHigh];
    private object _contrastValue = ContrastMedium;
    public object ContrastValue
    {
        get => _contrastValue;
        set
        {
            string normalized = NormalizeContrastValue(value);
            if (Equals(_contrastValue, normalized))
                return;
            _contrastValue = normalized;
            OnPropertyChanged();

            // Keep slider and preset aligned in UI.
            float presetRatio = normalized switch
            {
                ContrastNone => 1f,
                ContrastLow => 3f,
                ContrastHigh => 7f,
                _ => 4.5f
            };
            if (Math.Abs(_desiredContrastRatio - presetRatio) > 0.001f)
            {
                _desiredContrastRatio = presetRatio;
                OnPropertyChanged(nameof(DesiredContrastRatio));
            }

            if (IsColorAdjusted)
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
            OnPropertyChanged();
            if (IsColorAdjusted)
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
            OnPropertyChanged();
            if (_isColorAdjusted)
            {
                CaptureAdjustmentBaseline();
                ApplyColorAdjustmentIfEnabled();
            }
            else
            {
                RestoreAdjustmentBaseline();
                ThemeChanged?.Invoke(_primaryColor);
                SchemeChanged?.Invoke(ActiveScheme, GetColorForScheme(ActiveScheme));
            }
            SaveCurrentThemeSnapshot();
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
            OnPropertyChanged();
            if (IsColorAdjusted)
                ApplyColorAdjustmentIfEnabled();
            else
                ThemeChanged?.Invoke(SelectedColor);
            SaveCurrentThemeSnapshot();
        }
    }

    private Color _primaryColor;
    private Color _primaryForegroundColor;
    private Color _secondaryColor;
    private Color _secondaryForegroundColor;
    private bool _hasAdjustmentBaseline;
    private Color _baselinePrimaryColor;
    private Color _baselinePrimaryForegroundColor;
    private Color _baselineSecondaryColor;
    private Color _baselineSecondaryForegroundColor;
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
            OnPropertyChanged();
            if (_suppressSelectedColorApply)
                return;

            ApplyColorToActiveScheme(color);
        }
    }

    public ColorScheme ActiveScheme { get; set; } = ColorScheme.Primary;

    /// <summary>Returns the current accent foreground color as a hex string for use by ThemeResourceApplicator.</summary>
    public string PrimaryHex => $"#{_primaryColor.A:X2}{_primaryColor.R:X2}{_primaryColor.G:X2}{_primaryColor.B:X2}";
    public string ForegroundHex => $"#{_primaryForegroundColor.A:X2}{_primaryForegroundColor.R:X2}{_primaryForegroundColor.G:X2}{_primaryForegroundColor.B:X2}";
    public string SecondaryHex => $"#{_secondaryColor.A:X2}{_secondaryColor.R:X2}{_secondaryColor.G:X2}{_secondaryColor.B:X2}";
    public string SecondaryForegroundHex => $"#{_secondaryForegroundColor.A:X2}{_secondaryForegroundColor.R:X2}{_secondaryForegroundColor.G:X2}{_secondaryForegroundColor.B:X2}";

    public void ApplyBaseTheme(bool isDark)
    {
        IsDarkTheme = isDark;
    }

    public void ChangeCustomColor(object? obj)
    {
        Color color = ResolveAccentColor(obj);
        SetSelectedColorSilently(color);
        ApplyColorToActiveScheme(color);
    }

    public void ChangeScheme(ColorScheme scheme)
    {
        ActiveScheme = scheme;
        Color selected = scheme switch
        {
            ColorScheme.Primary => _primaryColor,
            ColorScheme.Secondary => _secondaryColor,
            ColorScheme.PrimaryForeground => _primaryForegroundColor,
            ColorScheme.SecondaryForeground => _secondaryForegroundColor,
            _ => _primaryColor
        };
        SetSelectedColorSilently(selected);
        SchemeChanged?.Invoke(scheme, selected);
    }

    public void SaveTheme(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        ThemeItem snapshot = BuildThemeSnapshot(name);
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
        // Direct color picks come in without a full serialized theme; keep primary/secondary
        // in lockstep here so runtime resources stay coherent until a named theme is loaded/saved.
        _primaryColor = resolved2;
        _secondaryColor = resolved2;
        _hasAdjustmentBaseline = false;
        if (IsColorAdjusted)
        {
            CaptureAdjustmentBaseline();
            ApplyColorAdjustmentIfEnabled();
        }
        else if (ActiveScheme == ColorScheme.Primary)
        {
            SetSelectedColorSilently(resolved2);
        }
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
        _contrastValue = NormalizeContrastValue(item.ContrastValue);
        _colorSelectionValue = NormalizeColorSelectionValue(item.ColorSelectionValue);
        _primaryColor = item.PrimaryColor;
        _secondaryColor = item.SecondaryColor;
        _primaryForegroundColor = item.PrimaryForegroundColor;
        _secondaryForegroundColor = item.SecondaryForegroundColor;
        _hasAdjustmentBaseline = false;
        if (_isColorAdjusted)
        {
            CaptureAdjustmentBaseline();
            ApplyColorAdjustmentIfEnabled();
        }
        ActiveScheme = ColorScheme.Primary;
        SetSelectedColorSilently(_primaryColor);
        OnPropertyChanged(nameof(IsDarkTheme));
        OnPropertyChanged(nameof(IsColorAdjusted));
        OnPropertyChanged(nameof(DesiredContrastRatio));
        OnPropertyChanged(nameof(ContrastValue));
        OnPropertyChanged(nameof(ColorSelectionValue));
        OnPropertyChanged(nameof(SelectedColor));
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

    private ThemeItem BuildThemeSnapshot(string name)
    {
        // Snapshot stores both adjusted and baseline-aware values so toggling color adjustment
        // can round-trip back to the original theme colors when disabled.
        return new ThemeItem
        {
            Name = name,
            IsDarkTheme = _isDarkTheme,
            PrimaryColor = _hasAdjustmentBaseline ? _baselinePrimaryColor : _primaryColor,
            SecondaryColor = _hasAdjustmentBaseline ? _baselineSecondaryColor : _secondaryColor,
            PrimaryForegroundColor = _hasAdjustmentBaseline ? _baselinePrimaryForegroundColor : _primaryForegroundColor,
            SecondaryForegroundColor = _hasAdjustmentBaseline ? _baselineSecondaryForegroundColor : _secondaryForegroundColor,
            UseColorAdjustment = _isColorAdjusted,
            DesiredContrastRatio = _desiredContrastRatio,
            ContrastValue = NormalizeContrastValue(_contrastValue),
            ColorSelectionValue = NormalizeColorSelectionValue(_colorSelectionValue)
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

    private void SetSelectedColorSilently(Color color)
    {
        _suppressSelectedColorApply = true;
        _selectedColor = color;
        _suppressSelectedColorApply = false;
        OnPropertyChanged(nameof(SelectedColor));
    }

    private void ApplyColorToActiveScheme(Color color)
    {
        if (ActiveScheme == ColorScheme.PrimaryForeground)
        {
            if (IsColorAdjusted)
            {
                CaptureAdjustmentBaseline();
                _baselinePrimaryForegroundColor = color;
                ApplyColorAdjustmentIfEnabled();
            }
            else
            {
                _primaryForegroundColor = color;
            }
        }
        else if (ActiveScheme == ColorScheme.SecondaryForeground)
        {
            if (IsColorAdjusted)
            {
                CaptureAdjustmentBaseline();
                _baselineSecondaryForegroundColor = color;
                ApplyColorAdjustmentIfEnabled();
            }
            else
            {
                _secondaryForegroundColor = color;
            }
        }
        else if (ActiveScheme == ColorScheme.Secondary)
        {
            if (IsColorAdjusted)
            {
                CaptureAdjustmentBaseline();
                _baselineSecondaryColor = color;
                ApplyColorAdjustmentIfEnabled();
            }
            else
            {
                _secondaryColor = color;
            }
        }
        else
        {
            if (IsColorAdjusted)
            {
                CaptureAdjustmentBaseline();
                _baselinePrimaryColor = color;
                ApplyColorAdjustmentIfEnabled();
            }
            else
            {
                _primaryColor = color;
            }
        }

        SaveCurrentThemeSnapshot();

        if (IsColorAdjusted && ActiveScheme == ColorScheme.Primary)
        {
            ThemeChanged?.Invoke(_primaryColor);
            return;
        }

        SchemeChanged?.Invoke(ActiveScheme, GetColorForScheme(ActiveScheme));
    }

    private void ApplyColorAdjustmentIfEnabled(bool forceRefreshWhenOff = false)
    {
        if (IsColorAdjusted)
        {
            CaptureAdjustmentBaseline();
            _primaryColor = ShouldAdjustPrimaryColor() ? GetAdjustedBackgroundColor(_baselinePrimaryColor) : _baselinePrimaryColor;
            _secondaryColor = ShouldAdjustSecondaryColor() ? GetAdjustedBackgroundColor(_baselineSecondaryColor) : _baselineSecondaryColor;
            if (ShouldAdjustPrimaryForeground())
                _primaryForegroundColor = GetAdjustedForegroundColor(_primaryColor);
            else
                _primaryForegroundColor = _baselinePrimaryForegroundColor;
            if (ShouldAdjustSecondaryForeground())
                _secondaryForegroundColor = GetAdjustedForegroundColor(_secondaryColor);
            else
                _secondaryForegroundColor = _baselineSecondaryForegroundColor;

            SetSelectedColorSilently(GetColorForScheme(ActiveScheme));
            ThemeChanged?.Invoke(_primaryColor);
            SchemeChanged?.Invoke(ActiveScheme, GetColorForScheme(ActiveScheme));
        }
        else if (forceRefreshWhenOff)
        {
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
        ThemeItem snapshot = BuildThemeSnapshot(themeName);
        _settingsService.Set("CurrentTheme", snapshot.ConvertToString());
    }

    private void CaptureAdjustmentBaseline()
    {
        if (_hasAdjustmentBaseline)
            return;

        _baselinePrimaryColor = _primaryColor;
        _baselinePrimaryForegroundColor = _primaryForegroundColor;
        _baselineSecondaryColor = _secondaryColor;
        _baselineSecondaryForegroundColor = _secondaryForegroundColor;
        _hasAdjustmentBaseline = true;
    }

    private void RestoreAdjustmentBaseline()
    {
        if (!_hasAdjustmentBaseline)
            return;

        _primaryColor = _baselinePrimaryColor;
        _primaryForegroundColor = _baselinePrimaryForegroundColor;
        _secondaryColor = _baselineSecondaryColor;
        _secondaryForegroundColor = _baselineSecondaryForegroundColor;
        _hasAdjustmentBaseline = false;
        SetSelectedColorSilently(GetColorForScheme(ActiveScheme));
    }

    private Color GetColorForScheme(ColorScheme scheme)
    {
        return scheme switch
        {
            ColorScheme.Primary => _primaryColor,
            ColorScheme.Secondary => _secondaryColor,
            ColorScheme.PrimaryForeground => _primaryForegroundColor,
            ColorScheme.SecondaryForeground => _secondaryForegroundColor,
            _ => _primaryColor
        };
    }

    private bool ShouldAdjustPrimaryForeground()
    {
        string selection = NormalizeColorSelectionValue(_colorSelectionValue);
        return selection is ColorSelectionPrimary or ColorSelectionAll;
    }

    private bool ShouldAdjustSecondaryForeground()
    {
        string selection = NormalizeColorSelectionValue(_colorSelectionValue);
        return selection is ColorSelectionSecondary or ColorSelectionAll;
    }

    private bool ShouldAdjustPrimaryColor()
    {
        string selection = NormalizeColorSelectionValue(_colorSelectionValue);
        return selection is ColorSelectionPrimary or ColorSelectionAll;
    }

    private bool ShouldAdjustSecondaryColor()
    {
        string selection = NormalizeColorSelectionValue(_colorSelectionValue);
        return selection is ColorSelectionSecondary or ColorSelectionAll;
    }

    private Color GetAdjustedForegroundColor(Color background)
    {
        Color baseForeground = GetBaseForegroundColor(background);
        double intensityScale = GetContrastIntensityScale(_contrastValue);
        if (intensityScale <= 0d)
            return baseForeground;

        double targetRatio = Math.Clamp(_desiredContrastRatio, 1f, 21f) * intensityScale;
        return ShiftForegroundTowardContrast(background, baseForeground, targetRatio);
    }

    private static Color GetBaseForegroundColor(Color background)
    {
        return RelativeLuminance(background) > 0.179d
            ? Color.FromArgb(255, 0, 0, 0)
            : Color.FromArgb(255, 255, 255, 255);
    }

    private static Color ShiftForegroundTowardContrast(Color background, Color baseForeground, double targetContrastRatio)
    {
        double bgLuminance = RelativeLuminance(background);
        bool useDarkForeground = baseForeground.R == 0 && baseForeground.G == 0 && baseForeground.B == 0;

        double targetLuminance = useDarkForeground
            ? ((bgLuminance + 0.05d) / targetContrastRatio) - 0.05d
            : (targetContrastRatio * (bgLuminance + 0.05d)) - 0.05d;

        targetLuminance = Math.Clamp(targetLuminance, 0d, 1d);
        byte channel = LuminanceToSrgbByte(targetLuminance);
        return Color.FromArgb(255, channel, channel, channel);
    }

    private Color GetAdjustedBackgroundColor(Color color)
    {
        double intensityScale = GetContrastIntensityScale(_contrastValue);
        if (intensityScale <= 0d)
            return color;

        double targetRatio = Math.Clamp(_desiredContrastRatio, 1f, 21f) * intensityScale;
        double amount = Math.Clamp((targetRatio - 1d) / 10d, 0d, 0.85d);

        return _isDarkTheme
            ? Mix(color, Color.FromArgb(255, 255, 255, 255), amount)
            : Mix(color, Color.FromArgb(255, 0, 0, 0), amount * 0.65d);
    }

    private static double GetContrastIntensityScale(object? contrastValue)
    {
        return NormalizeContrastValue(contrastValue) switch
        {
            ContrastNone => 0d,
            ContrastLow => 0.8d,
            ContrastHigh => 1.25d,
            _ => 1.0d
        };
    }

    private static string NormalizeColorSelectionValue(object? value)
    {
        string raw = value?.ToString() ?? ColorSelectionAll;
        if (raw.Equals(ColorSelectionNone, StringComparison.OrdinalIgnoreCase))
            return ColorSelectionNone;

        if (raw.Equals(ColorSelectionPrimary, StringComparison.OrdinalIgnoreCase))
            return ColorSelectionPrimary;

        if (raw.Equals(ColorSelectionSecondary, StringComparison.OrdinalIgnoreCase))
            return ColorSelectionSecondary;

        return ColorSelectionAll;
    }

    private static string NormalizeContrastValue(object? value)
    {
        string raw = value?.ToString() ?? ContrastMedium;
        if (raw.Equals(ContrastNone, StringComparison.OrdinalIgnoreCase))
            return ContrastNone;

        if (raw.Equals(ContrastLow, StringComparison.OrdinalIgnoreCase))
            return ContrastLow;

        if (raw.Equals(ContrastHigh, StringComparison.OrdinalIgnoreCase))
            return ContrastHigh;

        return ContrastMedium;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static byte LuminanceToSrgbByte(double luminance)
    {
        double srgb = luminance <= 0.0031308d
            ? luminance * 12.92d
            : 1.055d * Math.Pow(luminance, 1d / 2.4d) - 0.055d;

        return (byte)Math.Clamp((int)Math.Round(srgb * 255d), 0, 255);
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

    private static Color Mix(Color from, Color to, double amount)
    {
        amount = Math.Clamp(amount, 0d, 1d);
        byte M(byte a, byte b) => (byte)Math.Clamp((int)Math.Round(a + (b - a) * amount), 0, 255);
        return Color.FromArgb(255, M(from.R, to.R), M(from.G, to.G), M(from.B, to.B));
    }

}

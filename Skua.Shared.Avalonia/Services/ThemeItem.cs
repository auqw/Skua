using Avalonia.Media;
using System;
using System.Globalization;

namespace Skua.Shared.Avalonia.Services;

public class ThemeItem
{
    public string Name { get; set; } = "Skua";
    public bool IsDarkTheme { get; set; } = true;
    public Color PrimaryColor { get; set; } = Color.Parse("#FF607D8B");
    public Color SecondaryColor { get; set; } = Color.Parse("#FF607D8B");
    public Color PrimaryForegroundColor { get; set; } = Color.Parse("#FF000000");
    public Color SecondaryForegroundColor { get; set; } = Color.Parse("#FF000000");
    public bool UseColorAdjustment { get; set; } = false;
    public float DesiredContrastRatio { get; set; } = 4.5f;
    public string ContrastValue { get; set; } = "Medium";
    public string ColorSelectionValue { get; set; } = "All";

    /// <summary>
    /// Parse WPF-compatible CSV. Handles both #AARRGGBB and #RRGGBB color formats.
    /// Field order stays aligned with WPF so presets/current-theme values can be shared
    /// between WPF and Avalonia without a conversion or migration layer.
    /// Format: Name,Dark|Light,#primary,#secondary,#primaryFg,#secondaryFg[,useAdj,ratio,contrast,colorSel]
    /// </summary>
    public static ThemeItem? FromString(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return null;

        string[] parts = csv.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 6)
            return null;

        ThemeItem item = new()
        {
            Name = parts[0],
            IsDarkTheme = parts[1].Equals("Dark", StringComparison.OrdinalIgnoreCase),
            PrimaryColor = ParseColorOrDefault(parts[2], Color.Parse("#FF607D8B")),
            SecondaryColor = ParseColorOrDefault(parts[3], Color.Parse("#FF607D8B")),
            PrimaryForegroundColor = ParseColorOrDefault(parts[4], Color.Parse("#FF000000")),
            SecondaryForegroundColor = ParseColorOrDefault(parts[5], Color.Parse("#FF000000"))
        };

        if (parts.Length > 6 && bool.TryParse(parts[6], out bool useAdj))
            item.UseColorAdjustment = useAdj;

        if (parts.Length > 7 && float.TryParse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture, out float ratio) && ratio > 0)
            item.DesiredContrastRatio = ratio;

        if (parts.Length > 8 && !string.IsNullOrWhiteSpace(parts[8]))
            item.ContrastValue = parts[8];

        if (parts.Length > 9 && !string.IsNullOrWhiteSpace(parts[9]))
            item.ColorSelectionValue = parts[9];

        return item;
    }

    /// <summary>
    /// Serialize to WPF-compatible CSV using #AARRGGBB format.
    /// Output stays identical to WPF snapshot format so CurrentTheme/UserThemes are interchangeable.
    /// </summary>
    public string ConvertToString()
    {
        string baseTheme = IsDarkTheme ? "Dark" : "Light";
        string primary = FormatColor(PrimaryColor);
        string secondary = FormatColor(SecondaryColor);
        string primaryForeground = FormatColor(PrimaryForegroundColor);
        string secondaryForeground = FormatColor(SecondaryForegroundColor);
        string csv = $"{Name},{baseTheme},{primary},{secondary},{primaryForeground},{secondaryForeground}";

        if (UseColorAdjustment)
        {
            string ratio = DesiredContrastRatio.ToString(CultureInfo.InvariantCulture);
            string selection = string.IsNullOrWhiteSpace(ColorSelectionValue) ? "All" : ColorSelectionValue;
            csv += $",True,{ratio},{ContrastValue},{selection},";
        }

        return csv;
    }

    private static Color ParseColorOrDefault(string value, Color fallback)
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

    private static string FormatColor(Color color)
    {
        // Emit #AARRGGBB (WPF-compatible format).
        static string Hex(byte b) => b.ToString("X2").ToLowerInvariant();
        return $"#{Hex(color.A)}{Hex(color.R)}{Hex(color.G)}{Hex(color.B)}";
    }
}

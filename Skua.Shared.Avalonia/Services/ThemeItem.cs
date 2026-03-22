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

    /// <summary>
    /// Parse WPF-compatible CSV. Handles both #AARRGGBB and #RRGGBB color formats.
    /// Format: Name,Dark|Light,#primary,#secondary,#primaryFg,#secondaryFg[,useAdj,ratio,contrast,colorSel]
    /// </summary>
    public static ThemeItem? FromString(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
            return null;

        string[] parts = csv.Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length < 6)
            return null;

        try
        {
            ThemeItem item = new()
            {
                Name = parts[0],
                IsDarkTheme = parts[1].Equals("Dark", StringComparison.OrdinalIgnoreCase),
                PrimaryColor = ParseColor(parts[2]),
                SecondaryColor = ParseColor(parts[3]),
                PrimaryForegroundColor = ParseColor(parts[4]),
                SecondaryForegroundColor = ParseColor(parts[5])
            };

            if (parts.Length > 6 && bool.TryParse(parts[6], out bool useAdj))
                item.UseColorAdjustment = useAdj;

            if (parts.Length > 7 && float.TryParse(parts[7], NumberStyles.Any, CultureInfo.InvariantCulture, out float ratio))
                item.DesiredContrastRatio = ratio > 0 ? ratio : 4.5f;

            if (parts.Length > 8 && !string.IsNullOrWhiteSpace(parts[8]))
                item.ContrastValue = parts[8];

            return item;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Serialize to WPF-compatible CSV using #AARRGGBB format.
    /// Secondary values mirror primary (as per WPF convention).
    /// </summary>
    public string ConvertToString()
    {
        string baseTheme = IsDarkTheme ? "Dark" : "Light";
        string primary = FormatColor(PrimaryColor);
        string fg = FormatColor(PrimaryForegroundColor);
        string csv = $"{Name},{baseTheme},{primary},{primary},{fg},{fg}";

        if (UseColorAdjustment)
        {
            string ratio = DesiredContrastRatio.ToString(CultureInfo.InvariantCulture);
            csv += $",true,{ratio},{ContrastValue},All";
        }

        return csv;
    }

    private static Color ParseColor(string value)
    {
        // Avalonia Color.Parse handles both #AARRGGBB and #RRGGBB
        return Color.Parse(value);
    }

    private static string FormatColor(Color color)
    {
        // Emit #AARRGGBB — WPF-compatible format
        return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}

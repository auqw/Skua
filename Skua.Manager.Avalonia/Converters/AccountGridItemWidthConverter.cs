using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Skua.Manager.Avalonia.Converters;

public sealed class AccountGridItemWidthConverter : IMultiValueConverter
{
    private const double DefaultItemWidth = 200d;
    private const double MinimumItemWidth = 100d;

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return DefaultItemWidth;

        double availableWidth = ToDouble(values[0], 0d);
        int columns = Math.Max(2, (int)Math.Round(ToDouble(values[1], 3d)));
        if (availableWidth <= 0d)
            return DefaultItemWidth;

        // WPF parity intent: cards fill the row width evenly by column count.
        // Subtract a tiny epsilon so floating-point rounding does not force an early wrap.
        double computed = (availableWidth / columns) - 0.5d;

        return Math.Max(MinimumItemWidth, computed);
    }

    private static double ToDouble(object? value, double fallback)
    {
        return value switch
        {
            double d => d,
            float f => f,
            int i => i,
            long l => l,
            decimal m => (double)m,
            _ => fallback
        };
    }
}

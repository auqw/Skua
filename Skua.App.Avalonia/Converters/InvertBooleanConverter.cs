using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace Skua.App.Avalonia.Converters;

public class InvertBooleanConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }
}

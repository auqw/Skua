using System;
using System.Globalization;
using System.Windows.Data;

namespace Skua.WPF.Converters;

public class EqualToConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null && parameter is null)
            return true;

        if (value is null || parameter is null)
            return false;

        // Try numeric comparison first
        if (int.TryParse(value?.ToString() ?? string.Empty, out int intVal) && 
            int.TryParse(parameter?.ToString() ?? string.Empty, out int intParam))
        {
            return intVal == intParam;
        }

        return value.Equals(parameter) || value.ToString().Equals(parameter.ToString());
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

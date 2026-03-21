using Avalonia.Data.Converters;
using Skua.Core.Models.Servers;
using System;
using System.Globalization;

namespace Skua.Manager.Avalonia.Converters;

public sealed class ServerMembershipTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Server server && server.Upgrade)
            return " [M]";
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

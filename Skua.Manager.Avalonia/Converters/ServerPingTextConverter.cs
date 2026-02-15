using Avalonia.Data.Converters;
using Skua.Core.Models.Servers;
using System;
using System.Globalization;

namespace Skua.Manager.Avalonia.Converters;

public sealed class ServerPingTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not Server server)
            return string.Empty;

        if (!server.Online)
            return "Offline";

        return server.Ping switch
        {
            -1 => "...",
            9999 => "timeout",
            _ => $"{server.Ping}ms"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

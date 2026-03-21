using Avalonia;
using Avalonia.Controls;
using Skua.Core.Models.Servers;

namespace Skua.Shared.Avalonia.Controls.Settings;

public partial class ServerStatusInline : UserControl
{
    public static readonly StyledProperty<Server?> ServerProperty =
        AvaloniaProperty.Register<ServerStatusInline, Server?>(nameof(Server));

    public ServerStatusInline()
    {
        InitializeComponent();
    }

    public Server? Server
    {
        get => GetValue(ServerProperty);
        set => SetValue(ServerProperty, value);
    }
}

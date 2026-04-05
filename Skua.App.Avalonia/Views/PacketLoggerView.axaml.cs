using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Skua.App.Avalonia.Views;

public partial class PacketLoggerView : UserControl
{
    public PacketLoggerView()
    {
        InitializeComponent();
    }

    private void UnselectAllLogs_Click(object? sender, RoutedEventArgs e)
    {
        PacketsListBox.UnselectAll();
    }
}

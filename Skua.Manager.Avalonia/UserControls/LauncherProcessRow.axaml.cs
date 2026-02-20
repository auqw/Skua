using Avalonia;
using Avalonia.Controls;
using System.Windows.Input;

namespace Skua.Manager.Avalonia.UserControls;

public partial class LauncherProcessRow : UserControl
{
    public static readonly StyledProperty<ICommand?> StopCommandProperty =
        AvaloniaProperty.Register<LauncherProcessRow, ICommand?>(nameof(StopCommand));

    public LauncherProcessRow()
    {
        InitializeComponent();
    }

    public ICommand? StopCommand
    {
        get => GetValue(StopCommandProperty);
        set => SetValue(StopCommandProperty, value);
    }
}

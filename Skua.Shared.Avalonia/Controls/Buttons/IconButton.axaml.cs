using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Material.Icons;

namespace Skua.Shared.Avalonia.Controls.Buttons;

public partial class IconButton : UserControl
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<IconButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<IconButton, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<MaterialIconKind> IconKindProperty =
        AvaloniaProperty.Register<IconButton, MaterialIconKind>(nameof(IconKind));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<IconButton, double>(nameof(IconSize), 14);

    public static readonly StyledProperty<Thickness> ButtonPaddingProperty =
        AvaloniaProperty.Register<IconButton, Thickness>(nameof(ButtonPadding), new Thickness(0));

    public IconButton()
    {
        InitializeComponent();
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public MaterialIconKind IconKind
    {
        get => GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public Thickness ButtonPadding
    {
        get => GetValue(ButtonPaddingProperty);
        set => SetValue(ButtonPaddingProperty, value);
    }
}

using Avalonia;
using Avalonia.Controls;
using Material.Icons;

namespace Skua.Shared.Avalonia.Controls.Buttons;

public class IconTextButton : Button
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<IconTextButton, string?>(nameof(Text));

    public static readonly StyledProperty<MaterialIconKind> IconKindProperty =
        AvaloniaProperty.Register<IconTextButton, MaterialIconKind>(nameof(IconKind));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<IconTextButton, double>(nameof(IconSize), 14);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<IconTextButton, double>(nameof(Spacing), 4);

    public static readonly StyledProperty<Thickness> ButtonPaddingProperty =
        AvaloniaProperty.Register<IconTextButton, Thickness>(nameof(ButtonPadding), new Thickness(7, 2));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
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

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public Thickness ButtonPadding
    {
        get => GetValue(ButtonPaddingProperty);
        set => SetValue(ButtonPaddingProperty, value);
    }
}

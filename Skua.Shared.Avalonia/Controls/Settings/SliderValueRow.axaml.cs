using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Skua.Shared.Avalonia.Controls.Settings;

public partial class SliderValueRow : UserControl
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<SliderValueRow, double>(
            nameof(Value),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<SliderValueRow, double>(nameof(Minimum), 0);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<SliderValueRow, double>(nameof(Maximum), 1);

    public static readonly StyledProperty<double> TickFrequencyProperty =
        AvaloniaProperty.Register<SliderValueRow, double>(nameof(TickFrequency), 1);

    public static readonly StyledProperty<bool> IsSnapToTickEnabledProperty =
        AvaloniaProperty.Register<SliderValueRow, bool>(nameof(IsSnapToTickEnabled), false);

    public SliderValueRow()
    {
        InitializeComponent();
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double TickFrequency
    {
        get => GetValue(TickFrequencyProperty);
        set => SetValue(TickFrequencyProperty, value);
    }

    public bool IsSnapToTickEnabled
    {
        get => GetValue(IsSnapToTickEnabledProperty);
        set => SetValue(IsSnapToTickEnabledProperty, value);
    }
}

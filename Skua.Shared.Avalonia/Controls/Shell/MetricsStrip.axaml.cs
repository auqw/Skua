using Avalonia;
using Avalonia.Controls;

namespace Skua.Shared.Avalonia.Controls.Shell;

public partial class MetricsStrip : UserControl
{
    public static readonly StyledProperty<string?> FpsTextProperty =
        AvaloniaProperty.Register<MetricsStrip, string?>(nameof(FpsText), "FPS: --");

    public static readonly StyledProperty<string?> WorkingSetTextProperty =
        AvaloniaProperty.Register<MetricsStrip, string?>(nameof(WorkingSetText), "RAM: --");

    public static readonly StyledProperty<string?> PrivateTextProperty =
        AvaloniaProperty.Register<MetricsStrip, string?>(nameof(PrivateText), "Private: --");

    public static readonly StyledProperty<string?> ManagedTextProperty =
        AvaloniaProperty.Register<MetricsStrip, string?>(nameof(ManagedText), "Managed: --");

    public MetricsStrip()
    {
        InitializeComponent();
    }

    public string? FpsText
    {
        get => GetValue(FpsTextProperty);
        set => SetValue(FpsTextProperty, value);
    }

    public string? WorkingSetText
    {
        get => GetValue(WorkingSetTextProperty);
        set => SetValue(WorkingSetTextProperty, value);
    }

    public string? PrivateText
    {
        get => GetValue(PrivateTextProperty);
        set => SetValue(PrivateTextProperty, value);
    }

    public string? ManagedText
    {
        get => GetValue(ManagedTextProperty);
        set => SetValue(ManagedTextProperty, value);
    }
}

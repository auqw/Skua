using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Skua.Shared.Avalonia.Controls.Theming;

public partial class SchemePreviewCard : UserControl
{
    public static readonly StyledProperty<IBrush?> PreviewBorderBrushProperty =
        AvaloniaProperty.Register<SchemePreviewCard, IBrush?>(nameof(PreviewBorderBrush));

    public SchemePreviewCard()
    {
        InitializeComponent();
    }

    public IBrush? PreviewBorderBrush
    {
        get => GetValue(PreviewBorderBrushProperty);
        set => SetValue(PreviewBorderBrushProperty, value);
    }
}

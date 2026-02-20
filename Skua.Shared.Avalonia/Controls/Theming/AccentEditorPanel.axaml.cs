using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Skua.Shared.Avalonia.Controls.Theming;

public partial class AccentEditorPanel : UserControl
{
    public static readonly StyledProperty<IBrush?> SeparatorBrushProperty =
        AvaloniaProperty.Register<AccentEditorPanel, IBrush?>(nameof(SeparatorBrush));

    public AccentEditorPanel()
    {
        InitializeComponent();
    }

    public IBrush? SeparatorBrush
    {
        get => GetValue(SeparatorBrushProperty);
        set => SetValue(SeparatorBrushProperty, value);
    }
}

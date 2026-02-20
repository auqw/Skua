using Avalonia;
using Avalonia.Controls;

namespace Skua.Shared.Avalonia.Controls.Content;

public partial class MarkdownCard : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MarkdownCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MarkdownCard, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownCard, string?>(nameof(Markdown));

    public static readonly StyledProperty<object?> HeaderActionsProperty =
        AvaloniaProperty.Register<MarkdownCard, object?>(nameof(HeaderActions));

    public MarkdownCard()
    {
        InitializeComponent();
    }

    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }

    public object? HeaderActions
    {
        get => GetValue(HeaderActionsProperty);
        set => SetValue(HeaderActionsProperty, value);
    }
}

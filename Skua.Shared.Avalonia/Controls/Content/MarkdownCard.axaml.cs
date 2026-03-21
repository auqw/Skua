using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace Skua.Shared.Avalonia.Controls.Content;

public partial class MarkdownCard : UserControl
{
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<MarkdownCard, string?>(nameof(Title));

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MarkdownCard, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownCard, string?>(
            nameof(Markdown));

    public static readonly StyledProperty<object?> HeaderActionsProperty =
        AvaloniaProperty.Register<MarkdownCard, object?>(nameof(HeaderActions));

    static MarkdownCard()
    {
        MarkdownProperty.Changed.AddClassHandler<MarkdownCard>((card, _) => card.RenderBody());
    }

    public MarkdownCard()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) => RenderBody();
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

    private void RenderBody()
    {
        string markdown = Markdown ?? string.Empty;
        Control content = CreatePlainTextBody(markdown);
        BodyHost.Content = content;
    }

    private static Control CreatePlainTextBody(string markdown)
    {
        return new ScrollViewer
        {
            Margin = new Thickness(8),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = new SelectableTextBlock
            {
                Text = markdown,
                TextWrapping = TextWrapping.Wrap
            }
        };
    }
}

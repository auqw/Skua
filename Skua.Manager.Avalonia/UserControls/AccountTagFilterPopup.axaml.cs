using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using System.Collections;
using System.Windows.Input;

namespace Skua.Manager.Avalonia.UserControls;

public partial class AccountTagFilterPopup : UserControl
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<AccountTagFilterPopup, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<Control?> AnchorProperty =
        AvaloniaProperty.Register<AccountTagFilterPopup, Control?>(nameof(Anchor));

    public static readonly StyledProperty<string?> TagSearchTextProperty =
        AvaloniaProperty.Register<AccountTagFilterPopup, string?>(nameof(TagSearchText), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable?> FilteredTagsProperty =
        AvaloniaProperty.Register<AccountTagFilterPopup, IEnumerable?>(nameof(FilteredTags));

    public static readonly StyledProperty<ICommand?> ClearCommandProperty =
        AvaloniaProperty.Register<AccountTagFilterPopup, ICommand?>(nameof(ClearCommand));

    public AccountTagFilterPopup()
    {
        InitializeComponent();
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public Control? Anchor
    {
        get => GetValue(AnchorProperty);
        set => SetValue(AnchorProperty, value);
    }

    public string? TagSearchText
    {
        get => GetValue(TagSearchTextProperty);
        set => SetValue(TagSearchTextProperty, value);
    }

    public IEnumerable? FilteredTags
    {
        get => GetValue(FilteredTagsProperty);
        set => SetValue(FilteredTagsProperty, value);
    }

    public ICommand? ClearCommand
    {
        get => GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }
}

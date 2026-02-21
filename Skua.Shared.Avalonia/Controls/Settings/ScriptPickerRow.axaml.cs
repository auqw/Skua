using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using System.Windows.Input;

namespace Skua.Shared.Avalonia.Controls.Settings;

public partial class ScriptPickerRow : UserControl
{
    public static readonly StyledProperty<string?> ScriptPathProperty =
        AvaloniaProperty.Register<ScriptPickerRow, string?>(nameof(ScriptPath));

    public static readonly StyledProperty<ICommand?> SearchCommandProperty =
        AvaloniaProperty.Register<ScriptPickerRow, ICommand?>(nameof(SearchCommand));

    public static readonly StyledProperty<ICommand?> BrowseCommandProperty =
        AvaloniaProperty.Register<ScriptPickerRow, ICommand?>(nameof(BrowseCommand));

    public static readonly StyledProperty<bool?> AutoStartProperty =
        AvaloniaProperty.Register<ScriptPickerRow, bool?>(nameof(AutoStart), defaultBindingMode: BindingMode.TwoWay);

    public ScriptPickerRow()
    {
        InitializeComponent();
    }

    public string? ScriptPath
    {
        get => GetValue(ScriptPathProperty);
        set => SetValue(ScriptPathProperty, value);
    }

    public ICommand? SearchCommand
    {
        get => GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    public ICommand? BrowseCommand
    {
        get => GetValue(BrowseCommandProperty);
        set => SetValue(BrowseCommandProperty, value);
    }

    public bool? AutoStart
    {
        get => GetValue(AutoStartProperty);
        set => SetValue(AutoStartProperty, value);
    }
}

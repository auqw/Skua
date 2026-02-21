using Avalonia.Controls;
using Avalonia.Input;
using Skua.Core.Interfaces.ViewModels;

namespace Skua.Manager.Avalonia.UserControls;

public partial class AccountListRow : UserControl
{
    public AccountListRow()
    {
        InitializeComponent();
    }

    private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        if (sender is not Border border || border.DataContext is not IAccountItemViewModel account)
            return;

        if (IsFromInteractiveControl(e.Source, border))
            return;

        account.ToggleSelectionCommand.Execute(null);
        e.Handled = true;
    }

    private static bool IsFromInteractiveControl(object? source, Border boundary)
    {
        if (source is not global::Avalonia.StyledElement sourceElement)
            return false;

        global::Avalonia.StyledElement? current = sourceElement;
        while (current is not null && !ReferenceEquals(current, boundary))
        {
            if (current is Button || current is MenuItem || current is CheckBox)
                return true;

            current = current.Parent;
        }

        return false;
    }
}

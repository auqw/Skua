using Microsoft.Xaml.Behaviors;
using System.Windows.Controls;

namespace Skua.WPF;

/// <summary>
/// Adds "Select All" (multi-selection lists only) and "Unselect All" to a ListBox's own context menu.
/// Lists without their own menu get these from the window menu instead (<see cref="CustomWindow"/>).
/// Place it after any &lt;ListBox.ContextMenu&gt; in XAML, or that menu replaces the one this adds to.
/// </summary>
public class ListBoxCommonContextMenuBehavior : Behavior<ListBox>
{
    private readonly ListBoxSelectionMenuItems _items = new();

    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject.ContextMenu ??= new ContextMenu();
        _items.AddTo(AssociatedObject.ContextMenu);
        AssociatedObject.ContextMenuOpening += AssociatedObject_ContextMenuOpening;
    }

    protected override void OnDetaching()
    {
        AssociatedObject.ContextMenuOpening -= AssociatedObject_ContextMenuOpening;
        base.OnDetaching();
    }

    private void AssociatedObject_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        _items.Update(AssociatedObject, unselectForSingle: true);
    }
}

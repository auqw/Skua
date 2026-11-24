using System.Windows;
using System.Windows.Controls;

namespace Skua.WPF;

/// <summary>
/// "Select All" and "Unselect All" context menu items that act on one <see cref="ListBox"/>.
/// Call <see cref="Update"/> when the menu opens to point them at a list and show or hide them.
/// </summary>
internal sealed class ListBoxSelectionMenuItems
{
    private readonly Separator _separator = new();
    private readonly MenuItem _selectAll = new() { Header = "Select All" };
    private readonly MenuItem _unselectAll = new() { Header = "Unselect All" };
    private bool _hasItemsBefore;
    private ListBox? _target;

    public ListBoxSelectionMenuItems()
    {
        _selectAll.Click += (_, _) => _target?.SelectAll();
        _unselectAll.Click += (_, _) => _target?.UnselectAll();
    }

    public void AddTo(ContextMenu menu)
    {
        _hasItemsBefore = menu.Items.Count > 0;
        menu.Items.Add(_separator);
        menu.Items.Add(_selectAll);
        menu.Items.Add(_unselectAll);
    }

    /// <param name="target">The list the items act on; null hides them.</param>
    /// <param name="unselectForSingle">Also offer "Unselect All" on a single-selection list.</param>
    public void Update(ListBox? target, bool unselectForSingle)
    {
        _target = target;
        bool multi = target is not null && target.SelectionMode != SelectionMode.Single;
        bool showUnselect = target is not null && (multi || unselectForSingle);

        _selectAll.Visibility = multi ? Visibility.Visible : Visibility.Collapsed;
        _unselectAll.Visibility = showUnselect ? Visibility.Visible : Visibility.Collapsed;
        _separator.Visibility = _hasItemsBefore && showUnselect ? Visibility.Visible : Visibility.Collapsed;

        if (target is null)
            return;
        _selectAll.IsEnabled = target.Items.Count > target.SelectedItems.Count;
        _unselectAll.IsEnabled = target.SelectedItems.Count > 0;
    }
}

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Material.Icons;
using Material.Icons.Avalonia;
using System;

namespace Skua.Manager.Avalonia.UserControls;

public class SelectedAccountsContextMenu : ContextMenu
{
    public SelectedAccountsContextMenu()
    {
        MenuItem startSelected = CreateBoundMenuItem("Start Selected", "StartAccountsCommand", MaterialIconKind.PlayCircleOutline);
        MenuItem startWithScript = CreateClickMenuItem("Start Selected With Script", MaterialIconKind.ScriptTextPlayOutline, (_, _) =>
        {
            _ = FindOwner()?.StartSelectedWithScriptAsync();
        });
        MenuItem addTags = CreateClickMenuItem("Add Tags to Selected", MaterialIconKind.Tag, (_, _) =>
        {
            FindOwner()?.AddTagsToSelectedAction();
        });
        MenuItem addToGroup = CreateClickMenuItem("Add Selected to Group", MaterialIconKind.AccountMultiplePlus, (_, _) =>
        {
            FindOwner()?.AddSelectedToGroupAction();
        });
        MenuItem deselectAll = CreateBoundMenuItem("Deselect All", "DeselectAllCommand", MaterialIconKind.CheckboxBlankOutline);

        Items.Add(startSelected);
        Items.Add(startWithScript);
        Items.Add(new Separator());
        Items.Add(addTags);
        Items.Add(addToGroup);
        Items.Add(new Separator());
        Items.Add(deselectAll);
    }

    private AccountManagerUserControl? FindOwner()
    {
        if (PlacementTarget is not Control current)
            return null;

        while (current is not null)
        {
            if (current is AccountManagerUserControl owner)
                return owner;

            current = current.Parent as Control;
        }

        return null;
    }

    private static MenuItem CreateBoundMenuItem(string header, string commandPath, MaterialIconKind iconKind)
    {
        MenuItem item = new() { Header = header };
        item.Bind(MenuItem.CommandProperty, new Binding(commandPath));
        item.Icon = new MaterialIcon { Kind = iconKind, Width = 14, Height = 14 };
        return item;
    }

    private static MenuItem CreateClickMenuItem(string header, MaterialIconKind iconKind, EventHandler<RoutedEventArgs> clickHandler)
    {
        MenuItem item = new() { Header = header };
        item.Icon = new MaterialIcon { Kind = iconKind, Width = 14, Height = 14 };
        item.Click += clickHandler;
        return item;
    }
}

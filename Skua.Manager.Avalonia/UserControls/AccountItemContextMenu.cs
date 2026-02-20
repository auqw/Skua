using Avalonia.Controls;
using Avalonia.Data;
using Material.Icons;
using Material.Icons.Avalonia;

namespace Skua.Manager.Avalonia.UserControls;

public class AccountItemContextMenu : ContextMenu
{
    public AccountItemContextMenu()
    {
        Items.Add(CreateBoundMenuItem("Start", "StartCommand", MaterialIconKind.PlayCircleOutline));
        Items.Add(CreateBoundMenuItem("Start With Script", "StartWithScriptCommand", MaterialIconKind.ScriptTextPlayOutline));
        Items.Add(new Separator());
        Items.Add(CreateBoundMenuItem("Edit Tags", "AddTagsCommand", MaterialIconKind.Tag));
        Items.Add(CreateBoundMenuItem("Add to Group", "AddToGroupCommand", MaterialIconKind.AccountMultiplePlus));
        Items.Add(new Separator());
        Items.Add(CreateBoundMenuItem("Remove", "RemoveCommand", MaterialIconKind.DeleteOutline));
    }

    private static MenuItem CreateBoundMenuItem(string header, string commandPath, MaterialIconKind iconKind)
    {
        MenuItem item = new() { Header = header };
        item.Bind(MenuItem.CommandProperty, new Binding(commandPath));
        item.Icon = new MaterialIcon { Kind = iconKind, Width = 14, Height = 14 };
        return item;
    }
}

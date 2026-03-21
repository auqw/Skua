using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Messaging;

namespace Skua.Manager.Avalonia.UserControls;

public partial class GroupAccountRow : UserControl
{
    public GroupAccountRow()
    {
        InitializeComponent();
    }

    private void RemoveButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not IAccountItemViewModel account)
            return;

        if (Parent is not global::Avalonia.StyledElement directParent)
            return;

        IGroupItemViewModel? group = null;
        global::Avalonia.StyledElement? current = directParent;
        while (current is not null)
        {
            if (current is ItemsControl itemsControl && itemsControl.DataContext is IGroupItemViewModel candidate)
            {
                group = candidate;
                break;
            }

            current = current.Parent;
        }

        if (group is null)
            return;

        WeakReferenceMessenger.Default.Send(new RemoveAccountFromGroupMessage(group, account));
    }
}

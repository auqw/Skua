using Avalonia.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Messaging;
using Skua.Manager.Avalonia.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

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

        IGroupItemViewModel? group = ResolveGroup();
        if (group is null)
            return;

        WeakReferenceMessenger.Default.Send(new RemoveAccountFromGroupMessage(group, account));
    }

    private void ReplaceButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is not IAccountItemViewModel account)
            return;

        IGroupItemViewModel? group = ResolveGroup();
        AccountManagerViewModel? manager = ResolveManager();
        if (group is null || manager is null)
            return;

        List<IAccountItemViewModel> available = manager.Accounts
            .Where(a => !ReferenceEquals(a, account))
            .Where(a => !group.Accounts.Any(ga => string.Equals(ga.Username, a.Username, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (available.Count == 0)
        {
            Ioc.Default.GetRequiredService<IDialogService>()
                .ShowMessageBox("No available accounts to replace with.", "Replace Account");
            return;
        }

        SelectAccountDialogViewModel dialogVm = new(available)
        {
            SelectedAccount = available[0]
        };
        bool? result = Ioc.Default.GetRequiredService<IDialogService>()
            .ShowDialog(dialogVm, "Replace Account");
        if (result != true || dialogVm.SelectedAccount is null)
            return;

        WeakReferenceMessenger.Default.Send(new ReplaceAccountInGroupMessage(group, account, dialogVm.SelectedAccount));
    }

    private IGroupItemViewModel? ResolveGroup()
    {
        global::Avalonia.StyledElement? current = this;
        while (current is not null)
        {
            if (current is ItemsControl itemsControl && itemsControl.DataContext is IGroupItemViewModel group)
                return group;
            current = current.Parent;
        }

        return null;
    }

    private AccountManagerViewModel? ResolveManager()
    {
        global::Avalonia.StyledElement? current = this;
        while (current is not null)
        {
            if (current is AccountManagerUserControl managerControl && managerControl.DataContext is AccountManagerViewModel manager)
                return manager;
            current = current.Parent;
        }

        return null;
    }
}

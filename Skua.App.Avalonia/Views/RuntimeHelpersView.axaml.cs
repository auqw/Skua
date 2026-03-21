using Avalonia.Controls;
using Skua.App.Avalonia.ViewModels.Runtime;
using System.Collections.Generic;
using System.Linq;

namespace Skua.App.Avalonia.Views;

public partial class RuntimeHelpersView : UserControl
{
    public RuntimeHelpersView()
    {
        InitializeComponent();
    }

    private List<object> SelectedDrops()
    {
        if (DropList.SelectedItems is null)
            return new List<object>();

        return DropList.SelectedItems.Cast<object>().ToList();
    }

    private List<object> SelectedQuests()
    {
        if (QuestRegList.SelectedItems is null)
            return new List<object>();

        return QuestRegList.SelectedItems.Cast<object>().ToList();
    }

    private void RemoveSelectedDrops_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is RuntimeHelpersViewModel vm)
            vm.ToPickupDropsViewModel.RemoveDropsCommand.Execute(SelectedDrops());
    }

    private void RemoveSelectedQuests_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is RuntimeHelpersViewModel vm)
            vm.RegisteredQuestsViewModel.RemoveQuestsCommand.Execute(SelectedQuests());
    }

    private void BoostIdsInventory_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is RuntimeHelpersViewModel vm)
            vm.BoostsViewModel.SetBoostIDsCommand.Execute(false);
    }

    private void BoostIdsBank_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is RuntimeHelpersViewModel vm)
            vm.BoostsViewModel.SetBoostIDsCommand.Execute(true);
    }
}

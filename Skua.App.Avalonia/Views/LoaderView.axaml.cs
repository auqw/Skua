using Avalonia.Controls;
using Skua.App.Avalonia.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace Skua.App.Avalonia.Views;

public partial class LoaderView : UserControl
{
    public LoaderView()
    {
        InitializeComponent();
    }

    private List<object> SelectedQuestItems()
    {
        if (QuestList.SelectedItems is null)
            return new List<object>();

        return QuestList.SelectedItems.Cast<object>().ToList();
    }

    private void LoadSelected_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LoaderViewModel vm)
            vm.LoadQuestsCommand.Execute(SelectedQuestItems());
    }

    private void CopyNames_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LoaderViewModel vm)
            vm.CopyQuestsNamesCommand.Execute(SelectedQuestItems());
    }

    private void CopyIds_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LoaderViewModel vm)
            vm.CopyQuestsIDsCommand.Execute(SelectedQuestItems());
    }

    private void CopyNamesAndIds_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LoaderViewModel vm)
            vm.CopyQuestsNamesAndIDsCommand.Execute(SelectedQuestItems());
    }

    private void UpdateAll_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LoaderViewModel vm)
            vm.UpdateQuestsCommand.Execute(true);
    }

    private void UpdateMissing_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (DataContext is LoaderViewModel vm)
            vm.UpdateQuestsCommand.Execute(false);
    }
}

using Avalonia.Controls;
using Skua.App.Avalonia.ViewModels.Grabber;
using System.Collections.Generic;
using System.Linq;

namespace Skua.App.Avalonia.Views;

public partial class GrabberListView : UserControl
{
    public GrabberListView()
    {
        InitializeComponent();
    }

    private List<object> SelectedGrabbedItems()
    {
        if (GrabbedItemsList.SelectedItems is null)
            return new List<object>();

        return GrabbedItemsList.SelectedItems.Cast<object>().ToList();
    }

    private void GrabberTaskButton_Click(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: GrabberTaskViewModel taskVm })
            return;

        taskVm.GrabberTaskCommand.Execute(SelectedGrabbedItems());
    }
}

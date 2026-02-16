using Avalonia.Controls;
using Avalonia.VisualTree;
using Skua.Core.ViewModels;

namespace Skua.App.Avalonia.Views;

public partial class CBOClassSelectView : UserControl
{
    public CBOClassSelectView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is CBOClassSelectViewModel vm)
                vm.ReloadClassesCommand.Execute(null);
        };
    }
}

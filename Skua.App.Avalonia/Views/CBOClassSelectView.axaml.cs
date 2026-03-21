using Avalonia.Controls;
using Avalonia.VisualTree;
using Skua.App.Avalonia.ViewModels.CoreBotsOptions;

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

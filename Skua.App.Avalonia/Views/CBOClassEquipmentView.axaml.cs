using Avalonia.Controls;
using Skua.App.Avalonia.ViewModels.CoreBotsOptions;

namespace Skua.App.Avalonia.Views;

public partial class CBOClassEquipmentView : UserControl
{
    public CBOClassEquipmentView()
    {
        InitializeComponent();
        AttachedToVisualTree += (_, _) =>
        {
            if (DataContext is CBOClassEquipmentViewModel vm)
                vm.RefreshInventoryCommand.Execute(null);
        };
    }
}

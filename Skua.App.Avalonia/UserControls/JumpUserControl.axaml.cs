using Avalonia.Controls;
using Skua.App.Avalonia.ViewModels;

namespace Skua.App.Avalonia.UserControls;

public partial class JumpUserControl : UserControl
{
    public JumpUserControl()
    {
        InitializeComponent();
        Cells.DropDownOpened += Cells_DropDownOpened;
    }

    private void Cells_DropDownOpened(object? sender, System.EventArgs e)
    {
        if (DataContext is JumpViewModel vm)
            vm.UpdateCellsCommand.Execute(null);
    }
}

using Avalonia.Controls;
using Skua.App.Avalonia.ViewModels;

namespace Skua.App.Avalonia.UserControls;

public partial class AutoUserControl : UserControl
{
    public AutoUserControl()
    {
        InitializeComponent();
        ClassComboBox.DropDownOpened += ClassComboBox_DropDownOpened;
    }

    private void ClassComboBox_DropDownOpened(object? sender, System.EventArgs e)
    {
        if (DataContext is AutoViewModel vm)
            vm.ReloadClassesCommand.Execute(null);
    }
}

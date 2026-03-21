using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Text;

namespace Skua.App.Avalonia.ViewModels.CoreBotsOptions;

public class CBOLoadoutViewModel : ObservableObject, IManageCBOptions
{
    public CBOLoadoutViewModel(CBOClassSelectViewModel classSelectViewModel, CBOClassEquipmentViewModel classEquipmentViewModel)
    {
        ClassSelectViewModel = classSelectViewModel;
        ClassEquipmentViewModel = classEquipmentViewModel;
    }

    public CBOClassSelectViewModel ClassSelectViewModel { get; }
    public CBOClassEquipmentViewModel ClassEquipmentViewModel { get; }

    public StringBuilder Save(StringBuilder builder)
    {
        ClassSelectViewModel.Save(builder);
        ClassEquipmentViewModel.Save(builder);
        return builder;
    }

    public void SetValues(Dictionary<string, string> values)
    {
        ClassSelectViewModel.SetValues(values);
        ClassEquipmentViewModel.SetValues(values);
    }
}
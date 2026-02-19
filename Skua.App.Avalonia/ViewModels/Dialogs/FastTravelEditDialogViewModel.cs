using Skua.App.Avalonia.ViewModels.FastTravel;
using Skua.Shared.Avalonia.ViewModels.Dialogs;

namespace Skua.App.Avalonia.ViewModels.Dialogs;

public class FastTravelEditorDialogViewModel : DialogViewModelBase
{
    public FastTravelEditorDialogViewModel(FastTravelEditorViewModel fastTravelEditor)
        : base("Edit Fast Travel")
    {
        Editor = fastTravelEditor;
    }

    public FastTravelEditorViewModel Editor { get; }
}
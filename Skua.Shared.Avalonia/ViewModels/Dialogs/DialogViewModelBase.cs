using CommunityToolkit.Mvvm.ComponentModel;

namespace Skua.Shared.Avalonia.ViewModels.Dialogs;

public class DialogViewModelBase : ObservableRecipient
{
    public string Title { get; }

    public DialogViewModelBase(string title)
    {
        Title = title;
    }
}
using CommunityToolkit.Mvvm.ComponentModel;

namespace Skua.App.Avalonia.ViewModels.Dialogs;

public class DialogViewModelBase : ObservableRecipient
{
    public string Title { get; }

    public DialogViewModelBase(string title)
    {
        Title = title;
    }
}
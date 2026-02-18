using Skua.App.Avalonia.ViewModels.Dialogs;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Models;
using System;

namespace Skua.App.Avalonia.Services;

public class StubDialogService : IDialogService
{
    public bool? ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class
    {
        throw new NotImplementedException();
    }

    public bool? ShowDialog<TViewModel>(TViewModel viewModel, string Title) where TViewModel : class
    {
        throw new NotImplementedException();
    }

    public bool? ShowDialog<TViewModel>(TViewModel viewModel, Action<TViewModel> callback) where TViewModel : class
    {
        throw new NotImplementedException();
    }

    public void ShowDialog(IOptionContainer optionContainer, Action<IOptionContainerViewModel> callback)
    {
        throw new NotImplementedException();
    }

    public void ShowMessageBox(string message, string caption)
    {
        throw new NotImplementedException();
    }

    public bool? ShowMessageBox(string message, string caption, bool yesAndNo)
    {
        throw new NotImplementedException();
    }

    public DialogResult ShowMessageBox(string message, string caption, params string[] buttons)
    {
        throw new NotImplementedException();
    }

    public IInputDialogViewModel CreateInputDialog(string title, string dialogHint)
    {
        return new InputDialogViewModel(title, dialogHint);
    }
}
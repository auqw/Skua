using Skua.App.Avalonia.ViewModels.Dialogs;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Models;
using Skua.Shared.Avalonia.ViewModels.Dialogs;
using System;

namespace Skua.App.Avalonia.Services;

public class StubDialogService : IDialogService
{
    public bool? ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class
    {
        return null;
    }

    public bool? ShowDialog<TViewModel>(TViewModel viewModel, string Title) where TViewModel : class
    {
        return null;
    }

    public bool? ShowDialog<TViewModel>(TViewModel viewModel, Action<TViewModel> callback) where TViewModel : class
    {
        callback?.Invoke(viewModel);
        return null;
    }

    public void ShowDialog(IOptionContainer optionContainer, Action<IOptionContainerViewModel> callback)
    {
        // Avalonia dialog host is not wired yet; no-op stub keeps flows non-fatal.
    }

    public void ShowMessageBox(string message, string caption)
    {
        //WinForms.MessageBox.Show(message, caption, WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
    }

    public bool? ShowMessageBox(string message, string caption, bool yesAndNo)
    {
        /*if (!yesAndNo)
        {
            WinForms.MessageBox.Show(message, caption, WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
            return true;
        }

        WinForms.DialogResult result = WinForms.MessageBox.Show(
            message,
            caption,
            WinForms.MessageBoxButtons.YesNo,
            WinForms.MessageBoxIcon.Question);
        return result == WinForms.DialogResult.Yes;*/

        return null;
    }

    public DialogResult ShowMessageBox(string message, string caption, params string[] buttons)
    {
        /*if (buttons is null || buttons.Length == 0)
        {
            WinForms.MessageBox.Show(message, caption, WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
            return new DialogResult("OK", 0);
        }

        if (buttons.Length == 2 && string.Equals(buttons[0], "Yes", StringComparison.OrdinalIgnoreCase) && string.Equals(buttons[1], "No", StringComparison.OrdinalIgnoreCase))
        {
            WinForms.DialogResult result = WinForms.MessageBox.Show(
                message,
                caption,
                WinForms.MessageBoxButtons.YesNo,
                WinForms.MessageBoxIcon.Question);
            return result == WinForms.DialogResult.Yes ? new DialogResult(buttons[0], 0) : new DialogResult(buttons[1], 1);
        }

        WinForms.MessageBox.Show(message, caption, WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
        return new DialogResult(buttons[0], 0);*/
        
        return new DialogResult("", 0);
    }

    public IInputDialogViewModel CreateInputDialog(string title, string dialogHint)
    {
        return new InputDialogViewModel(title, dialogHint);
    }
}

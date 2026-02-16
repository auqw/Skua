using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.ViewModels;
using Skua.Core.ViewModels.Manager;
using System;
using System.Drawing;
using System.Linq;
using WinForms = System.Windows.Forms;

namespace Skua.App.Avalonia.Services;

public class DialogService : IDialogService
{
    public bool? ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class
        => ShowDialogInternal(viewModel, viewModel.GetType().Name);

    public bool? ShowDialog<TViewModel>(TViewModel viewModel, string title) where TViewModel : class
        => ShowDialogInternal(viewModel, title);

    public bool? ShowDialog<TViewModel>(TViewModel viewModel, Action<TViewModel> callback) where TViewModel : class
    {
        bool? result = ShowDialogInternal(viewModel, viewModel.GetType().Name);
        callback(viewModel);
        return result;
    }

    public void ShowMessageBox(string message, string caption)
    {
        WinForms.MessageBox.Show(message, caption, WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
    }

    public bool? ShowMessageBox(string message, string caption, bool yesAndNo)
    {
        WinForms.DialogResult result = WinForms.MessageBox.Show(
            message,
            caption,
            yesAndNo ? WinForms.MessageBoxButtons.YesNo : WinForms.MessageBoxButtons.OK,
            WinForms.MessageBoxIcon.Information);

        return result == WinForms.DialogResult.Yes || result == WinForms.DialogResult.OK;
    }

    public DialogResult ShowMessageBox(string message, string caption, params string[] buttons)
    {
        string[] actualButtons = buttons.Length == 0 ? ["OK"] : buttons;
        string clicked = ShowButtonsDialog(caption, message, actualButtons);
        int index = Array.IndexOf(actualButtons, clicked);
        return index < 0 ? DialogResult.Cancelled : new DialogResult(clicked, index);
    }

    private static bool? ShowDialogInternal<TViewModel>(TViewModel viewModel, string title) where TViewModel : class
    {
        return viewModel switch
        {
            InputDialogViewModel input => ShowInputDialog(input, title),
            SelectGroupDialogViewModel selectGroup => ShowSelectGroupDialog(selectGroup, title),
            MessageBoxDialogViewModel messageBox => ShowMessageBoxInternal(messageBox.Message, messageBox.Title, messageBox.YesAndNo),
            CustomDialogViewModel custom => ShowCustomDialog(custom, title),
            _ => ShowFallbackDialog(viewModel.GetType().Name, title)
        };
    }

    private static bool? ShowInputDialog(InputDialogViewModel vm, string title)
    {
        using WinForms.Form form = CreateForm(title, 420, 170);

        WinForms.Label label = new()
        {
            AutoSize = false,
            Text = vm.DialogHint,
            Bounds = new Rectangle(12, 12, 392, 34)
        };

        WinForms.TextBox input = new()
        {
            Text = vm.DialogTextInput,
            Bounds = new Rectangle(12, 52, 392, 24)
        };

        if (vm.NumberOnly)
        {
            input.TextChanged += (_, _) =>
            {
                string digitsOnly = new(input.Text.Where(char.IsDigit).ToArray());
                if (input.Text != digitsOnly)
                {
                    int caret = Math.Min(input.SelectionStart, digitsOnly.Length);
                    input.Text = digitsOnly;
                    input.SelectionStart = caret;
                }
            };
        }

        WinForms.Button ok = CreateButton("OK", 240, 94);
        WinForms.Button cancel = CreateButton("Cancel", 324, 94);

        bool? result = null;
        ok.Click += (_, _) =>
        {
            vm.DialogTextInput = input.Text;
            result = true;
            form.Close();
        };
        cancel.Click += (_, _) =>
        {
            result = false;
            form.Close();
        };

        form.Controls.Add(label);
        form.Controls.Add(input);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        form.ShowDialog();
        return result;
    }

    private static bool? ShowSelectGroupDialog(SelectGroupDialogViewModel vm, string title)
    {
        using WinForms.Form form = CreateForm(title, 420, 150);

        WinForms.Label label = new()
        {
            AutoSize = true,
            Text = "Select group",
            Location = new Point(12, 14)
        };

        WinForms.ComboBox combo = new()
        {
            DropDownStyle = WinForms.ComboBoxStyle.DropDownList,
            Bounds = new Rectangle(12, 36, 392, 24),
            DataSource = vm.Groups.ToList(),
            DisplayMember = nameof(GroupItemViewModel.Name)
        };

        if (vm.SelectedGroup is not null)
            combo.SelectedItem = vm.SelectedGroup;

        WinForms.Button ok = CreateButton("OK", 240, 76);
        WinForms.Button cancel = CreateButton("Cancel", 324, 76);

        bool? result = null;
        ok.Click += (_, _) =>
        {
            vm.SelectedGroup = combo.SelectedItem as GroupItemViewModel;
            result = vm.SelectedGroup is not null;
            form.Close();
        };
        cancel.Click += (_, _) =>
        {
            result = false;
            form.Close();
        };

        form.Controls.Add(label);
        form.Controls.Add(combo);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        form.ShowDialog();
        return result;
    }

    private static bool? ShowCustomDialog(CustomDialogViewModel vm, string title)
    {
        string clicked = ShowButtonsDialog(title, vm.Message, vm.Buttons.ToArray());
        int index = vm.Buttons.IndexOf(clicked);
        vm.Result = index < 0 ? DialogResult.Cancelled : new DialogResult(clicked, index);
        return index >= 0;
    }

    private static bool? ShowFallbackDialog(string typeName, string title)
    {
        WinForms.MessageBox.Show(
            $"Dialog view not ported yet: {typeName}",
            title,
            WinForms.MessageBoxButtons.OK,
            WinForms.MessageBoxIcon.Information);
        return true;
    }

    private static bool? ShowMessageBoxInternal(string message, string caption, bool yesAndNo)
    {
        WinForms.DialogResult result = WinForms.MessageBox.Show(
            message,
            caption,
            yesAndNo ? WinForms.MessageBoxButtons.YesNo : WinForms.MessageBoxButtons.OK,
            WinForms.MessageBoxIcon.Information);

        return result == WinForms.DialogResult.Yes || result == WinForms.DialogResult.OK;
    }

    private static string ShowButtonsDialog(string title, string message, string[] buttons)
    {
        using WinForms.Form form = CreateForm(title, 460, 170);

        WinForms.Label label = new()
        {
            AutoSize = false,
            Text = message,
            Bounds = new Rectangle(12, 12, 432, 70)
        };

        WinForms.FlowLayoutPanel panel = new()
        {
            FlowDirection = WinForms.FlowDirection.RightToLeft,
            WrapContents = false,
            AutoSize = false,
            Bounds = new Rectangle(12, 94, 432, 34)
        };

        string clicked = string.Empty;
        foreach (string text in buttons.Reverse())
        {
            WinForms.Button button = new()
            {
                Text = text,
                Width = 84,
                Height = 28,
                Margin = new WinForms.Padding(8, 0, 0, 0)
            };
            button.Click += (_, _) =>
            {
                clicked = text;
                form.Close();
            };
            panel.Controls.Add(button);
        }

        form.Controls.Add(label);
        form.Controls.Add(panel);
        form.ShowDialog();
        return clicked;
    }

    private static WinForms.Form CreateForm(string title, int width, int height) =>
        new()
        {
            Text = title,
            Width = width,
            Height = height,
            FormBorderStyle = WinForms.FormBorderStyle.FixedDialog,
            StartPosition = WinForms.FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            TopMost = true
        };

    private static WinForms.Button CreateButton(string text, int x, int y) =>
        new()
        {
            Text = text,
            Width = 80,
            Height = 28,
            Location = new Point(x, y)
        };
}

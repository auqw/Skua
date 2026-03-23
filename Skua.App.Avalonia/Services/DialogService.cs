using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Models;
using Skua.Shared.Avalonia.ViewModels.Dialogs;
using Skua.Shared.Avalonia.ViewModels.Options;
using System;

namespace Skua.App.Avalonia.Services;

#if IS_WINDOWS
using Skua.App.Avalonia.ViewModels.AdvancedSkills;
using Skua.App.Avalonia.ViewModels.Dialogs;
using System.Drawing;
using System.Linq;
using WinForms = System.Windows.Forms;
public class DialogService : IDialogService
{
    public bool? ShowDialog<TViewModel>(TViewModel viewModel) where TViewModel : class
        => ShowDialogInternal(viewModel, viewModel.GetType().Name);

    public bool? ShowDialog<TViewModel>(TViewModel viewModel, string title) where TViewModel : class
        => ShowDialogInternal(viewModel, title);

    public bool? ShowDialog<TViewModel>(TViewModel viewModel, Action<TViewModel> callback) where TViewModel : class
    {
        bool? result = ShowDialogInternal(viewModel, viewModel.GetType().Name);
        callback?.Invoke(viewModel);
        return result;
    }

    public void ShowDialog(IOptionContainer optionContainer, Action<IOptionContainerViewModel> callback)
    {
        OptionContainerViewModel vm = new(optionContainer);
        callback?.Invoke(vm);
    }

    public IInputDialogViewModel CreateInputDialog(string title, string dialogHint)
    {
        return new InputDialogViewModel(title, dialogHint);
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
            AssignHotKeyDialogViewModel hotKey => ShowAssignHotKeyDialog(hotKey, title),
            SkillRuleEditorDialogViewModel skillRules => ShowSkillRuleEditorDialog(skillRules, title),
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

    private static bool? ShowCustomDialog(CustomDialogViewModel vm, string title)
    {
        string clicked = ShowButtonsDialog(title, vm.Message, vm.Buttons.ToArray());
        int index = vm.Buttons.IndexOf(clicked);
        vm.Result = index < 0 ? DialogResult.Cancelled : new DialogResult(clicked, index);
        return index >= 0;
    }

    private static bool? ShowSkillRuleEditorDialog(SkillRuleEditorDialogViewModel vm, string title)
    {
        // Edit a clone and copy back only on confirm so cancel behaves correctly.
        SkillRulesViewModel scratch = new(vm.UseRules);

        using WinForms.Form form = CreateForm(string.IsNullOrWhiteSpace(title) ? "Edit Rules" : title, 640, 720);
        form.MinimumSize = new Size(640, 680);

        WinForms.PropertyGrid propertyGrid = new()
        {
            Dock = WinForms.DockStyle.Fill,
            SelectedObject = scratch,
            HelpVisible = true,
            ToolbarVisible = false,
            PropertySort = WinForms.PropertySort.CategorizedAlphabetical
        };

        WinForms.Label help = new()
        {
            Dock = WinForms.DockStyle.Top,
            Height = 42,
            Text = "Edit rule values then click Confirm. Multi-aura entries can be managed in the main Skills panel.",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new WinForms.Padding(8, 0, 8, 0)
        };

        WinForms.FlowLayoutPanel panel = new()
        {
            Dock = WinForms.DockStyle.Bottom,
            Height = 44,
            FlowDirection = WinForms.FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new WinForms.Padding(8, 6, 8, 6)
        };

        WinForms.Button confirm = new() { Text = "Confirm", Width = 92, Height = 28 };
        WinForms.Button cancel = new() { Text = "Cancel", Width = 92, Height = 28 };

        bool? result = null;
        confirm.Click += (_, _) =>
        {
            CopySkillRules(scratch, vm.UseRules);
            result = true;
            form.Close();
        };
        cancel.Click += (_, _) =>
        {
            result = false;
            form.Close();
        };

        panel.Controls.Add(confirm);
        panel.Controls.Add(cancel);
        form.Controls.Add(propertyGrid);
        form.Controls.Add(help);
        form.Controls.Add(panel);
        form.AcceptButton = confirm;
        form.CancelButton = cancel;

        form.ShowDialog();
        return result;
    }

    private static bool? ShowAssignHotKeyDialog(AssignHotKeyDialogViewModel vm, string title)
    {
        using WinForms.Form form = CreateForm(string.IsNullOrWhiteSpace(title) ? "Assign HotKey" : title, 420, 190);

        WinForms.CheckBox ctrl = new()
        {
            Text = "Ctrl",
            Checked = vm.CtrlCheck,
            Location = new Point(12, 14),
            AutoSize = true
        };
        WinForms.CheckBox shift = new()
        {
            Text = "Shift",
            Checked = vm.ShiftCheck,
            Location = new Point(92, 14),
            AutoSize = true
        };
        WinForms.CheckBox alt = new()
        {
            Text = "Alt",
            Checked = vm.AltCheck,
            Location = new Point(176, 14),
            AutoSize = true
        };

        WinForms.Label keyLabel = new()
        {
            Text = "Key",
            AutoSize = true,
            Location = new Point(12, 48)
        };
        WinForms.TextBox keyInput = new()
        {
            Text = vm.KeyInput,
            Bounds = new Rectangle(12, 66, 392, 24)
        };
        keyInput.TextChanged += (_, _) =>
        {
            if (keyInput.Text.Length > 1)
                keyInput.Text = keyInput.Text[..1].ToUpperInvariant();
            keyInput.SelectionStart = keyInput.Text.Length;
        };

        WinForms.Button ok = CreateButton("OK", 240, 106);
        WinForms.Button cancel = CreateButton("Cancel", 324, 106);

        bool? result = null;
        ok.Click += (_, _) =>
        {
            vm.CtrlCheck = ctrl.Checked;
            vm.ShiftCheck = shift.Checked;
            vm.AltCheck = alt.Checked;
            vm.KeyInput = (keyInput.Text ?? string.Empty).Trim().ToUpperInvariant();
            result = true;
            form.Close();
        };
        cancel.Click += (_, _) =>
        {
            result = false;
            form.Close();
        };

        form.Controls.Add(ctrl);
        form.Controls.Add(shift);
        form.Controls.Add(alt);
        form.Controls.Add(keyLabel);
        form.Controls.Add(keyInput);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;
        form.ShowDialog();
        return result;
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

    private static void CopySkillRules(SkillRulesViewModel source, SkillRulesViewModel target)
    {
        target.UseRuleBool = source.UseRuleBool;
        target.MultiAuraBool = source.MultiAuraBool;
        target.HealthGreaterThanBool = source.HealthGreaterThanBool;
        target.HealthUseValue = source.HealthUseValue;
        target.HealthIsPercentage = source.HealthIsPercentage;
        target.ManaGreaterThanBool = source.ManaGreaterThanBool;
        target.ManaUseValue = source.ManaUseValue;
        target.ManaIsPercentage = source.ManaIsPercentage;
        target.WaitUseValue = source.WaitUseValue;
        target.SkipUseBool = source.SkipUseBool;
        target.AuraGreaterThanBool = source.AuraGreaterThanBool;
        target.AuraUseValue = source.AuraUseValue;
        target.AuraTargetIndex = source.AuraTargetIndex;
        target.AuraName = source.AuraName;
        target.PartyMemberHealthGreaterThanBool = source.PartyMemberHealthGreaterThanBool;
        target.PartyMemberHealthUseValue = source.PartyMemberHealthUseValue;
        target.PartyMemberHealthIsPercentage = source.PartyMemberHealthIsPercentage;
        target.MultiAuraOperatorIndex = source.MultiAuraOperatorIndex;

        target.MultiAuraChecks.Clear();
        foreach (AuraCheckViewModel check in source.MultiAuraChecks)
            target.MultiAuraChecks.Add(new AuraCheckViewModel(check));
    }
}
#else
public class DialogService : IDialogService
{
    // TODO implement these properly
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
        
    }

    public bool? ShowMessageBox(string message, string caption, bool yesAndNo)
    {
        return null;
    }

    public DialogResult ShowMessageBox(string message, string caption, params string[] buttons)
    {
        return new DialogResult("", 0);
    }

    public IInputDialogViewModel CreateInputDialog(string title, string dialogHint)
    {
        return new InputDialogViewModel(title, dialogHint);
    }
}
#endif

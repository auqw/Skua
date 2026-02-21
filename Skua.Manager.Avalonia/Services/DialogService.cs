using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using Skua.Core.Models;
using Skua.Manager.Avalonia.ViewModels;
using Skua.Shared.Avalonia.ViewModels.Options;
using Skua.Shared.Avalonia.ViewModels.Dialogs;
using System;
using System.Linq;
using CustomDialogViewModel = Skua.Shared.Avalonia.ViewModels.Dialogs.CustomDialogViewModel;

namespace Skua.Manager.Avalonia.Services;

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

    public void ShowMessageBox(string message, string caption)
    {
        _ = ShowButtonsDialog(caption, message, ["OK"]);
    }

    public bool? ShowMessageBox(string message, string caption, bool yesAndNo)
    {
        string[] buttons = yesAndNo ? ["No", "Yes"] : ["OK"];
        string clicked = ShowButtonsDialog(caption, message, buttons);
        return clicked == "Yes" || clicked == "OK";
    }

    public DialogResult ShowMessageBox(string message, string caption, params string[] buttons)
    {
        string[] actualButtons = buttons.Length == 0 ? ["OK"] : buttons;
        string clicked = ShowButtonsDialog(caption, message, actualButtons);
        int index = Array.IndexOf(actualButtons, clicked);
        return index < 0 ? DialogResult.Cancelled : new DialogResult(clicked, index);
    }

    public IInputDialogViewModel CreateInputDialog(string title, string dialogHint)
    {
        return new InputDialogViewModel(title, dialogHint);
    }

    private bool? ShowDialogInternal<TViewModel>(TViewModel viewModel, string title) where TViewModel : class
    {
        return viewModel switch
        {
            InputDialogViewModel input => ShowInputDialog(input, title),
            SelectGroupDialogViewModel selectGroup => ShowSelectGroupDialog(selectGroup, title),
            MessageBoxDialogViewModel messageBox => ShowMessageBox(messageBox.Message, messageBox.Title, messageBox.YesAndNo),
            CustomDialogViewModel custom => ShowCustomDialog(custom, title),
            _ => ShowFallbackDialog(viewModel, title)
        };
    }

    private static bool? ShowInputDialog(InputDialogViewModel vm, string title)
    {
        Window dialog = CreateBaseDialog(title);
        TextBox input = new() { Text = vm.DialogTextInput, Watermark = vm.TextBoxHint };
        if (vm.NumberOnly)
            input.TextChanged += (_, _) => input.Text = new string((input.Text ?? string.Empty).Where(char.IsDigit).ToArray());

        bool? result = null;
        Button ok = new() { Content = "OK", MinWidth = 84 };
        Button cancel = new() { Content = "Cancel", MinWidth = 84 };
        ok.Click += (_, _) =>
        {
            vm.DialogTextInput = input.Text ?? string.Empty;
            result = true;
            dialog.Close();
        };
        cancel.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(14),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = vm.DialogHint },
                input,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancel, ok }
                }
            }
        };
        Show(dialog);
        return result;
    }

    private static bool? ShowSelectGroupDialog(SelectGroupDialogViewModel vm, string title)
    {
        Window dialog = CreateBaseDialog(title);
        ComboBox groups = new()
        {
            ItemsSource = vm.Groups,
            SelectedItem = vm.SelectedGroup,
            ItemTemplate = new FuncDataTemplate<GroupItemViewModel>((g, _) =>
                new TextBlock { Text = g?.Name ?? string.Empty }, true)
        };

        bool? result = null;
        Button ok = new() { Content = "OK", MinWidth = 84 };
        Button cancel = new() { Content = "Cancel", MinWidth = 84 };
        ok.Click += (_, _) =>
        {
            vm.SelectedGroup = groups.SelectedItem as GroupItemViewModel;
            result = vm.SelectedGroup != null;
            dialog.Close();
        };
        cancel.Click += (_, _) =>
        {
            result = false;
            dialog.Close();
        };

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(14),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = "Select group" },
                groups,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancel, ok }
                }
            }
        };
        Show(dialog);
        return result;
    }

    private static bool? ShowCustomDialog(CustomDialogViewModel vm, string title)
    {
        string clicked = ShowButtonsDialog(title, vm.Message, vm.Buttons.ToArray());
        int index = vm.Buttons.IndexOf(clicked);
        vm.Result = index < 0 ? DialogResult.Cancelled : new DialogResult(clicked, index);
        return index >= 0;
    }

    private static bool? ShowFallbackDialog<TViewModel>(TViewModel vm, string title) where TViewModel : class
    {
        Window dialog = CreateBaseDialog(title);
        bool? result = null;
        Button ok = new() { Content = "OK", MinWidth = 84 };
        ok.Click += (_, _) =>
        {
            result = true;
            dialog.Close();
        };
        dialog.Content = new StackPanel
        {
            Margin = new Thickness(14),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = $"Dialog view not ported yet: {vm.GetType().Name}" },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { ok }
                }
            }
        };
        Show(dialog);
        return result;
    }

    private static string ShowButtonsDialog(string title, string message, string[] buttons)
    {
        Window dialog = CreateBaseDialog(title);
        string clicked = string.Empty;
        StackPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        foreach (string buttonText in buttons)
        {
            Button button = new() { Content = buttonText, MinWidth = 84 };
            button.Click += (_, _) =>
            {
                clicked = buttonText;
                dialog.Close();
            };
            row.Children.Add(button);
        }

        dialog.Content = new StackPanel
        {
            Margin = new Thickness(14),
            Spacing = 10,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                row
            }
        };

        Show(dialog);
        return clicked;
    }

    private static Window CreateBaseDialog(string title) =>
        new()
        {
            Title = title,
            Width = 420,
            Height = 180,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

    private static void Show(Window dialog)
    {
        Window? owner = GetOwner();
        if (owner != null)
        {
            dialog.ShowDialog(owner).GetAwaiter().GetResult();
            return;
        }

        Window tempOwner = new()
        {
            Width = 1,
            Height = 1,
            Opacity = 0,
            ShowInTaskbar = false
        };
        tempOwner.Show();
        dialog.ShowDialog(tempOwner).GetAwaiter().GetResult();
        tempOwner.Close();
    }

    private static Window? GetOwner()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return null;
        return desktop.MainWindow;
    }
}

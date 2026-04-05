using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Linq;
using Skua.Shared.Avalonia.ViewModels.Options;

namespace Skua.Shared.Avalonia.Controls.Options;

public partial class OptionItemControl : UserControl
{
    private const double CompactOptionHeight = 22;
    private const double CompactOptionFontSize = 11;

    public OptionItemControl()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => RebuildContent();
    }

    private void RebuildContent()
    {
        if (DataContext is not DisplayOptionItemViewModelBase vm)
        {
            OptionHost.Content = null;
            return;
        }

        OptionHost.Content = CreateDefaultContent(vm);
    }

    public static Control CreateDefaultContent(DisplayOptionItemViewModelBase vm)
    {
        return vm.Tag switch
        {
            "1" => BuildBooleanItem(vm),
            "2" => BuildTextItem(vm),
            "3" => BuildNumberItem(vm),
            "4" => BuildActionItem(vm),
            _ => BuildFallbackItem(vm)
        };
    }

    public static ToggleButton CreateBoundToggleButton(DisplayOptionItemViewModelBase vm, object? content = null)
    {
        ToggleButton toggleButton = new()
        {
            Content = content ?? vm.Content
        };
        BindValue(toggleButton, vm);

        if (vm is CommandOptionItemViewModel commandVm)
        {
            toggleButton.IsCheckedChanged += (_, _) => commandVm.Command.Execute(toggleButton.IsChecked == true);
        }

        return toggleButton;
    }

    public static TextBox CreateBoundTextBox(DisplayOptionItemViewModelBase vm, bool numericOnly = false)
    {
        TextBox textBox = new();
        BindValue(textBox, vm);

        if (numericOnly)
        {
            textBox.TextInput += (_, e) =>
            {
                if (!char.IsDigit(e.Text?[0] ?? '\0'))
                    e.Handled = true;
            };
            textBox.TextChanged += (_, _) =>
            {
                string original = textBox.Text ?? string.Empty;
                string sanitized = SanitizeDigits(original);
                if (!string.Equals(original, sanitized, StringComparison.Ordinal))
                {
                    textBox.Text = sanitized;
                    textBox.CaretIndex = sanitized.Length;
                }
            };
        }

        return textBox;
    }

    public static Button CreateBoundButton(DisplayOptionItemViewModelBase vm, object? content = null, Func<string?>? commandArgumentFactory = null)
    {
        Button button = new()
        {
            Content = content ?? vm.Content
        };
        ToolTip.SetTip(button, vm.Content);
        button.Classes.Add("skua-button");

        if (vm is CommandOptionItemViewModel commandVm)
        {
            if (commandArgumentFactory is null)
            {
                button.Command = commandVm.Command;
            }
            else
            {
                button.Click += (_, _) => commandVm.Command.Execute(commandArgumentFactory());
            }
        }

        return button;
    }

    private static Control BuildBooleanItem(DisplayOptionItemViewModelBase vm)
    {
        ToggleButton toggleButton = CreateBoundToggleButton(vm);
        toggleButton.Classes.Add("option-chip");
        ToolTip.SetTip(toggleButton, vm.Content);
        toggleButton.Margin = new Thickness(6, 1);
        return toggleButton;
    }

    private static Control BuildTextItem(DisplayOptionItemViewModelBase vm)
    {
        TextBox textBox = CreateBoundTextBox(vm);
        textBox.Watermark = vm.Content;
        textBox.Classes.Add("option-pill-input");
        textBox.Theme = null;
        textBox.Height = CompactOptionHeight;
        textBox.MinHeight = CompactOptionHeight;
        textBox.MaxHeight = CompactOptionHeight;
        textBox.FontSize = CompactOptionFontSize;
        textBox.Padding = new Thickness(10, 0, 0, 0);

        Button button = CreateBoundButton(vm, "Set", () => textBox.Text ?? string.Empty);
        button.Classes.Add("option-pill-action");
        button.Classes.Add("option-pill-action-auto");
        button.Classes.Remove("skua-button");
        button.Theme = null;
        button.Height = CompactOptionHeight;
        button.MinHeight = CompactOptionHeight;
        button.MaxHeight = CompactOptionHeight;
        button.FontSize = CompactOptionFontSize;
        button.FontWeight = FontWeight.Normal;
        button.Padding = new Thickness(6, 0);
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        TextBlock.SetLineHeight(button, CompactOptionHeight);
        button.MinWidth = 64;

        return BuildOptionPill(textBox, button, middle: null, fillAction: false);
    }

    private static Control BuildNumberItem(DisplayOptionItemViewModelBase vm)
    {
        TextBox textBox = CreateBoundTextBox(vm, numericOnly: true);
        textBox.Classes.Add("option-pill-input");
        textBox.Theme = null;
        textBox.Height = CompactOptionHeight;
        textBox.MinHeight = CompactOptionHeight;
        textBox.MaxHeight = CompactOptionHeight;
        textBox.FontSize = CompactOptionFontSize;
        textBox.Padding = new Thickness(10, 0, 0, 0);
        textBox.Width = 70;
        textBox.HorizontalAlignment = HorizontalAlignment.Left;

        Button button = CreateBoundButton(vm, vm.Content, () => textBox.Text ?? string.Empty);
        button.Classes.Add("option-pill-action");
        button.Classes.Remove("skua-button");
        button.Theme = null;
        button.Height = CompactOptionHeight;
        button.MinHeight = CompactOptionHeight;
        button.MaxHeight = CompactOptionHeight;
        button.FontSize = CompactOptionFontSize;
        button.FontWeight = FontWeight.Normal;
        button.Padding = new Thickness(6, 0);
        button.VerticalContentAlignment = VerticalAlignment.Center;
        button.HorizontalContentAlignment = HorizontalAlignment.Center;
        TextBlock.SetLineHeight(button, CompactOptionHeight);
        button.MinWidth = 90;

        if (!string.IsNullOrWhiteSpace(vm.SuffixText))
        {
            TextBlock suffix = new()
            {
                Text = vm.SuffixText,
                Classes = { "option-pill-suffix" }
            };
            return BuildOptionPill(textBox, button, suffix, fillAction: true);
        }

        return BuildOptionPill(textBox, button, middle: null, fillAction: true);
    }

    private static Control BuildActionItem(DisplayOptionItemViewModelBase vm)
    {
        Button button = CreateBoundButton(vm);
        button.Classes.Remove("skua-button");
        button.Classes.Add("option-action-button");
        button.Theme = null;
        button.Margin = new Thickness(6, 2);
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        return button;
    }

    private static Control BuildFallbackItem(DisplayOptionItemViewModelBase vm)
    {
        if (vm.DisplayType == typeof(bool))
            return BuildBooleanItem(vm);

        if (vm.DisplayType == typeof(string))
            return BuildTextItem(vm);

        if (IsNumericDisplayType(vm.DisplayType))
            return BuildNumberItem(vm);

        return BuildActionItem(vm);
    }

    private static void BindValue(TextBox textBox, DisplayOptionItemViewModelBase vm)
    {
        textBox[!TextBox.TextProperty] = new Binding(nameof(DisplayOptionItemViewModelBase.Value))
        {
            Source = vm,
            Mode = BindingMode.TwoWay
        };
        textBox.GotFocus += (_, _) => textBox.SelectAll();
    }

    private static void BindValue(ToggleButton toggleButton, DisplayOptionItemViewModelBase vm)
    {
        toggleButton[!ToggleButton.IsCheckedProperty] = new Binding(nameof(DisplayOptionItemViewModelBase.Value))
        {
            Source = vm,
            Mode = BindingMode.TwoWay
        };
    }

    private static bool IsNumericDisplayType(Type displayType)
    {
        return displayType == typeof(byte)
            || displayType == typeof(sbyte)
            || displayType == typeof(short)
            || displayType == typeof(ushort)
            || displayType == typeof(int)
            || displayType == typeof(uint)
            || displayType == typeof(long)
            || displayType == typeof(ulong);
    }

    private static string SanitizeDigits(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static Control BuildOptionPill(Control input, Button action, Control? middle = null, bool fillAction = false)
    {
        Border border = new()
        {
            Classes = { "option-pill-container" },
            Margin = new Thickness(6, 1)
        };

        string columns;
        if (middle is null)
            columns = fillAction ? "Auto,*" : "*,Auto";
        else
            columns = fillAction ? "Auto,Auto,*" : "*,Auto,Auto";
        Grid grid = new()
        {
            ColumnDefinitions = new ColumnDefinitions(columns)
        };

        grid.Children.Add(input);
        Grid.SetColumn(input, 0);

        if (middle is not null)
        {
            grid.Children.Add(middle);
            Grid.SetColumn(middle, 1);
            grid.Children.Add(action);
            Grid.SetColumn(action, 2);
        }
        else
        {
            grid.Children.Add(action);
            Grid.SetColumn(action, 1);
        }

        border.Child = grid;
        return border;
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Skua.Shared.Avalonia.ViewModels.Options;

namespace Skua.Shared.Avalonia.Controls.Options;

public partial class OptionItemControl : UserControl
{
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

    public static CheckBox CreateBoundCheckBox(DisplayOptionItemViewModelBase vm, object? content = null)
    {
        CheckBox checkBox = new()
        {
            Content = content ?? vm.Content,
            IsThreeState = false
        };
        BindValue(checkBox, vm);

        if (vm is CommandOptionItemViewModel commandVm)
            checkBox.IsCheckedChanged += (_, _) => commandVm.Command.Execute(checkBox.IsChecked == true);

        return checkBox;
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
        }

        return textBox;
    }

    public static Button CreateBoundButton(DisplayOptionItemViewModelBase vm, object? content = null, Func<string?>? commandArgumentFactory = null)
    {
        Button button = new()
        {
            Content = content ?? vm.Content
        };
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
        CheckBox checkBox = CreateBoundCheckBox(vm);
        checkBox.Margin = new Thickness(6, 4);
        return checkBox;
    }

    private static Control BuildTextItem(DisplayOptionItemViewModelBase vm)
    {
        TextBox textBox = CreateBoundTextBox(vm);
        textBox.Watermark = vm.Content;
        textBox.MinWidth = 220;

        Button button = CreateBoundButton(vm, "Set", () => textBox.Text ?? string.Empty);
        button.MinWidth = 64;
        button.Margin = new Thickness(8, 0, 0, 0);

        Grid grid = new()
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            Margin = new Thickness(6, 4)
        };
        grid.Children.Add(textBox);
        grid.Children.Add(button);
        Grid.SetColumn(button, 1);
        return grid;
    }

    private static Control BuildNumberItem(DisplayOptionItemViewModelBase vm)
    {
        TextBox textBox = CreateBoundTextBox(vm, numericOnly: true);
        textBox.Width = 90;
        textBox.HorizontalAlignment = HorizontalAlignment.Left;

        TextBlock suffix = new()
        {
            Text = vm.SuffixText ?? string.Empty,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            IsVisible = !string.IsNullOrWhiteSpace(vm.SuffixText)
        };

        Button button = CreateBoundButton(vm, vm.Content, () => textBox.Text ?? string.Empty);
        button.MinWidth = 96;

        StackPanel row = new()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(6, 4),
            Children = { textBox, suffix, button }
        };
        return row;
    }

    private static Control BuildActionItem(DisplayOptionItemViewModelBase vm)
    {
        Button button = CreateBoundButton(vm);
        button.Margin = new Thickness(6, 4);
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
    }

    private static void BindValue(CheckBox checkBox, DisplayOptionItemViewModelBase vm)
    {
        checkBox[!CheckBox.IsCheckedProperty] = new Binding(nameof(DisplayOptionItemViewModelBase.Value))
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
}

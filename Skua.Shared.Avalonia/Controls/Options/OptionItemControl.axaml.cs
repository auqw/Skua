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

        OptionHost.Content = vm.Tag switch
        {
            "1" => BuildBooleanItem(vm),
            "2" => BuildTextItem(vm),
            "3" => BuildNumberItem(vm),
            "4" => BuildActionItem(vm),
            _ => BuildActionItem(vm)
        };
    }

    private static Control BuildBooleanItem(DisplayOptionItemViewModelBase vm)
    {
        CheckBox checkBox = new()
        {
            Content = vm.Content,
            Margin = new Thickness(6, 4),
            IsThreeState = false
        };
        checkBox[!CheckBox.IsCheckedProperty] = new Binding(nameof(DisplayOptionItemViewModelBase.Value))
        {
            Source = vm,
            Mode = BindingMode.TwoWay
        };

        if (vm is CommandOptionItemViewModel commandVm)
            checkBox.IsCheckedChanged += (_, _) => commandVm.Command.Execute(checkBox.IsChecked == true);

        return checkBox;
    }

    private static Control BuildTextItem(DisplayOptionItemViewModelBase vm)
    {
        TextBox textBox = new()
        {
            Watermark = vm.Content,
            MinWidth = 220
        };
        textBox[!TextBox.TextProperty] = new Binding(nameof(DisplayOptionItemViewModelBase.Value))
        {
            Source = vm,
            Mode = BindingMode.TwoWay
        };

        Button button = new()
        {
            Content = "Set",
            MinWidth = 64,
            Margin = new Thickness(8, 0, 0, 0)
        };
        button.Classes.Add("skua-button");
        if (vm is CommandOptionItemViewModel commandVm)
            button.Click += (_, _) => commandVm.Command.Execute(textBox.Text ?? string.Empty);

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
        TextBox textBox = new()
        {
            Width = 90,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        textBox[!TextBox.TextProperty] = new Binding(nameof(DisplayOptionItemViewModelBase.Value))
        {
            Source = vm,
            Mode = BindingMode.TwoWay
        };
        textBox.TextInput += (_, e) =>
        {
            if (!char.IsDigit(e.Text?[0] ?? '\0'))
                e.Handled = true;
        };

        TextBlock suffix = new()
        {
            Text = vm.SuffixText ?? string.Empty,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            IsVisible = !string.IsNullOrWhiteSpace(vm.SuffixText)
        };

        Button button = new()
        {
            Content = vm.Content,
            MinWidth = 96
        };
        button.Classes.Add("skua-button");
        if (vm is CommandOptionItemViewModel commandVm)
            button.Click += (_, _) => commandVm.Command.Execute(textBox.Text ?? string.Empty);

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
        Button button = new()
        {
            Content = vm.Content,
            Margin = new Thickness(6, 4),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        button.Classes.Add("skua-button");

        if (vm is CommandOptionItemViewModel commandVm)
            button.Command = commandVm.Command;

        return button;
    }
}

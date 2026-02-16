using Avalonia.Controls;
using Avalonia.Data;
using Skua.Core.ViewModels;

namespace Skua.App.Avalonia.UserControls;

public partial class CBOOptionItemUserControl : UserControl
{
    public CBOOptionItemUserControl()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Rebuild();
    }

    private void Rebuild()
    {
        if (DataContext is not DisplayOptionItemViewModelBase vm)
        {
            OptionHost.Content = null;
            return;
        }

        if (vm is CBOBoolChoiceOptionItemViewModel boolChoice)
        {
            OptionHost.Content = BuildBoolChoice(boolChoice);
            return;
        }

        if (vm is CBOChoiceOptionItemViewModel choice)
        {
            OptionHost.Content = BuildChoice(choice);
            return;
        }

        if (vm.DisplayType == typeof(bool))
        {
            OptionHost.Content = BuildBool(vm);
            return;
        }

        if (vm.DisplayType == typeof(int) || vm.DisplayType == typeof(long))
        {
            OptionHost.Content = BuildNumber(vm);
            return;
        }

        OptionHost.Content = BuildText(vm);
    }

    private static Control BuildBool(DisplayOptionItemViewModelBase vm)
    {
        Grid grid = new() { ColumnDefinitions = new("*,Auto"), Margin = new(4, 3) };
        TextBlock title = new() { Text = vm.Content, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center };
        CheckBox value = new() { Content = "Enabled", HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right };
        value[!CheckBox.IsCheckedProperty] = new Binding(nameof(DisplayOptionItemViewModelBase.Value))
        {
            Source = vm,
            Mode = BindingMode.TwoWay
        };
        grid.Children.Add(title);
        grid.Children.Add(value);
        Grid.SetColumn(value, 1);
        return grid;
    }

    private static Control BuildNumber(DisplayOptionItemViewModelBase vm)
    {
        Grid grid = new() { ColumnDefinitions = new("*,120"), Margin = new(4, 3) };
        TextBlock title = new() { Text = vm.Content, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center };
        TextBox value = new() { HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch };
        value[!TextBox.TextProperty] = new Binding(nameof(DisplayOptionItemViewModelBase.Value))
        {
            Source = vm,
            Mode = BindingMode.TwoWay
        };
        value.TextInput += (_, e) =>
        {
            if (!char.IsDigit(e.Text?[0] ?? '\0'))
                e.Handled = true;
        };
        grid.Children.Add(title);
        grid.Children.Add(value);
        Grid.SetColumn(value, 1);
        return grid;
    }

    private static Control BuildText(DisplayOptionItemViewModelBase vm)
    {
        Grid grid = new() { ColumnDefinitions = new("*,180"), Margin = new(4, 3) };
        TextBlock title = new() { Text = vm.Content, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center };
        TextBox value = new() { HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Stretch };
        value[!TextBox.TextProperty] = new Binding(nameof(DisplayOptionItemViewModelBase.Value))
        {
            Source = vm,
            Mode = BindingMode.TwoWay
        };
        grid.Children.Add(title);
        grid.Children.Add(value);
        Grid.SetColumn(value, 1);
        return grid;
    }

    private static Control BuildChoice(CBOChoiceOptionItemViewModel vm)
    {
        Grid grid = new() { ColumnDefinitions = new("*,180"), Margin = new(4, 3) };
        TextBlock title = new() { Text = vm.Content, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center };
        ComboBox combo = new() { ItemsSource = vm.Options };
        combo[!ComboBox.SelectedIndexProperty] = new Binding(nameof(DisplayOptionItemViewModelBase.Value))
        {
            Source = vm,
            Mode = BindingMode.TwoWay
        };
        grid.Children.Add(title);
        grid.Children.Add(combo);
        Grid.SetColumn(combo, 1);
        return grid;
    }

    private static Control BuildBoolChoice(CBOBoolChoiceOptionItemViewModel vm)
    {
        StackPanel root = new() { Spacing = 4, Margin = new(4, 3) };
        root.Children.Add(new TextBlock { Text = vm.Content });

        StackPanel options = new() { Spacing = 2, Margin = new(16, 0, 0, 0) };
        RadioButton first = new() { Content = vm.FirstChoice };
        RadioButton second = new() { Content = vm.SecondChoice };
        string group = $"cbo_{vm.Tag}";
        first.GroupName = group;
        second.GroupName = group;

        bool current = vm.Value is bool b && b;
        first.IsChecked = current;
        second.IsChecked = !current;

        first.IsCheckedChanged += (_, _) => vm.Value = true;
        second.IsCheckedChanged += (_, _) => vm.Value = false;

        options.Children.Add(first);
        options.Children.Add(second);
        root.Children.Add(options);
        return root;
    }
}

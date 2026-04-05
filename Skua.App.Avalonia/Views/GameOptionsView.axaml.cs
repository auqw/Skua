using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using System.Linq;

namespace Skua.App.Avalonia.Views;

public partial class GameOptionsView : UserControl
{
    public GameOptionsView()
    {
        InitializeComponent();
        Loaded += GameOptionsView_Loaded;
    }

    private void GameOptionsView_Loaded(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<TextBox>("ColumnsBox") is { } columnsBox)
        {
            columnsBox.TextInput -= ColumnsBox_TextInput;
            columnsBox.TextChanged -= ColumnsBox_TextChanged;
            columnsBox.TextInput += ColumnsBox_TextInput;
            columnsBox.TextChanged += ColumnsBox_TextChanged;
        }
    }

    private static void ColumnsBox_TextInput(object? sender, TextInputEventArgs e)
    {
        char firstChar = string.IsNullOrEmpty(e.Text) ? '\0' : e.Text[0];
        if (!char.IsDigit(firstChar))
            e.Handled = true;
    }

    private static void ColumnsBox_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        string original = textBox.Text ?? string.Empty;
        char[] digits = original.Where(char.IsDigit).ToArray();
        string sanitized = new(digits);
        if (sanitized != original)
        {
            textBox.Text = sanitized;
            textBox.CaretIndex = sanitized.Length;
        }
    }
}

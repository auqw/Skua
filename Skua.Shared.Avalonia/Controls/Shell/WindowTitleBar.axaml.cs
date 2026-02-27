using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Skua.Shared.Avalonia.Controls.Shell;

public partial class WindowTitleBar : UserControl
{
    public static readonly StyledProperty<string> TitleTextProperty =
        AvaloniaProperty.Register<WindowTitleBar, string>(nameof(TitleText), "Skua");

    public string TitleText
    {
        get => GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public WindowTitleBar()
    {
        InitializeComponent();
        PointerPressed += OnTitleBarPointerPressed;
        MinimizeButton.Click += OnMinimizeClicked;
        MaximizeButton.Click += OnMaximizeClicked;
        CloseButton.Click += OnCloseClicked;
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || TopLevel.GetTopLevel(this) is not Window window)
            return;

        window.BeginMoveDrag(e);
    }

    private void OnMinimizeClicked(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
            window.WindowState = WindowState.Minimized;
    }

    private void OnMaximizeClicked(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window window)
            return;

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is Window window)
            window.Close();
    }
}

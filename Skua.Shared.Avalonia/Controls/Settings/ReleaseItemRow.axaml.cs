using Avalonia;
using Avalonia.Controls;
using System.Windows.Input;

namespace Skua.Shared.Avalonia.Controls.Settings;

public partial class ReleaseItemRow : UserControl
{
    public static readonly StyledProperty<string?> ReleaseNameProperty =
        AvaloniaProperty.Register<ReleaseItemRow, string?>(nameof(ReleaseName));

    public static readonly StyledProperty<string?> VersionProperty =
        AvaloniaProperty.Register<ReleaseItemRow, string?>(nameof(Version));

    public static readonly StyledProperty<ICommand?> DownloadCommandProperty =
        AvaloniaProperty.Register<ReleaseItemRow, ICommand?>(nameof(DownloadCommand));

    public ReleaseItemRow()
    {
        InitializeComponent();
    }

    public string? ReleaseName
    {
        get => GetValue(ReleaseNameProperty);
        set => SetValue(ReleaseNameProperty, value);
    }

    public string? Version
    {
        get => GetValue(VersionProperty);
        set => SetValue(VersionProperty, value);
    }

    public ICommand? DownloadCommand
    {
        get => GetValue(DownloadCommandProperty);
        set => SetValue(DownloadCommandProperty, value);
    }
}

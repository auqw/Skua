using CommunityToolkit.Mvvm.ComponentModel;
using Skua.Shared.Avalonia.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Skua.App.Avalonia.ViewModels.AppLogs;

public partial class LogsViewModel : BotControlViewModelBase
{
    public LogsViewModel(IEnumerable<LogTabViewModel> logTabs)
        : base("Logs")
    {
        _logTabs = new(logTabs);
        _selectedTab = _logTabs[0];
    }

    [ObservableProperty]
    private ObservableCollection<LogTabViewModel> _logTabs;

    [ObservableProperty]
    private LogTabViewModel _selectedTab;
}
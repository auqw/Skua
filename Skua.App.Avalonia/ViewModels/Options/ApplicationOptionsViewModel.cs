using Skua.Shared.Avalonia.ViewModels;
using Skua.Shared.Avalonia.ViewModels.Options;
using System.Collections.Generic;

namespace Skua.App.Avalonia.ViewModels.Options;

public class ApplicationOptionsViewModel : BotControlViewModelBase
{
    public ApplicationOptionsViewModel(List<DisplayOptionItemViewModelBase> appOptions)
        : base("Application Options", 420, 0)
    {
        ApplicationOptions = appOptions;
    }

    public List<DisplayOptionItemViewModelBase> ApplicationOptions { get; }
}
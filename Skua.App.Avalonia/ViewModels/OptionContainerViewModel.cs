using CommunityToolkit.Mvvm.ComponentModel;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.ViewModels;
using System.Collections.Generic;

namespace Skua.App.Avalonia.ViewModels;

public partial class OptionContainerViewModel : ObservableObject, IOptionContainerViewModel
{
    public OptionContainerViewModel(IOptionContainer container)
    {
        Container = container;
        Options = new();
        foreach (IOption option in container.Options)
            Options.Add(new OptionContainerItemViewModel(container, option));

        if (container.MultipleOptions.Count > 0)
        {
            foreach (List<IOption> optionList in container.MultipleOptions.Values)
            {
                foreach (IOption option in optionList)
                    Options.Add(new OptionContainerItemViewModel(container, option));
            }
        }
    }

    public string Title { get; } = "Options";
    public IOptionContainer Container { get; set; }

    public List<IOptionContainerItemViewModel> Options { get; }

    [ObservableProperty]
    private IOptionContainerItemViewModel? _selectedOption;
}
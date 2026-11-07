using CommunityToolkit.Mvvm.ComponentModel;
using Skua.Core.Interfaces;

namespace Skua.Core.ViewModels;

public partial class OptionContainerViewModel : ObservableObject
{
    public OptionContainerViewModel(IOptionContainer container)
    {
        Container = container;
        Options = new();
        foreach (IOption option in container.Options)
            Options.Add(new(container, option));

        if (container.MultipleOptions.Count > 0)
        {
            foreach (List<IOption> optionList in container.MultipleOptions.Values)
            {
                foreach (IOption option in optionList)
                    Options.Add(new(container, option));
            }
        }

        if (container.ResetOptions.Count > 0)
            ResetNotice = "These saved choices no longer exist and were reset:\n"
                + string.Join("\n", container.ResetOptions)
                + "\nCheck them, then close this window to save.";
    }

    public string Title { get; } = "Options";
    public string? ResetNotice { get; }
    public IOptionContainer Container { get; set; }

    public List<OptionContainerItemViewModel> Options { get; }

    [ObservableProperty]
    private OptionContainerItemViewModel? _selectedOption;
}
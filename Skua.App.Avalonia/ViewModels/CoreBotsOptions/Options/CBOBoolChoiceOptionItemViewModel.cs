using Skua.App.Avalonia.ViewModels.Options;
using Skua.Shared.Avalonia.ViewModels.Options;

namespace Skua.App.Avalonia.ViewModels.CoreBotsOptions.Options;

public class CBOBoolChoiceOptionItemViewModel : DisplayOptionItemViewModel<bool>
{
    public CBOBoolChoiceOptionItemViewModel(string optionTitle, string description, string tag, string firstChoice, string secondChoice)
        : base(optionTitle, description, tag)
    {
        FirstChoice = firstChoice;
        SecondChoice = secondChoice;
        Value = false;
    }

    public CBOBoolChoiceOptionItemViewModel(string optionTitle, string description, string tag, string firstChoice, string secondChoice, bool value)
        : base(optionTitle, description, tag)
    {
        FirstChoice = firstChoice;
        SecondChoice = secondChoice;
        Value = value;
    }

    public string FirstChoice { get; }
    public string SecondChoice { get; }
}
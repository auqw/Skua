using Skua.App.Avalonia.ViewModels.AdvancedSkills;

namespace Skua.App.Avalonia.ViewModels.Dialogs;

public class SkillRuleEditorDialogViewModel : DialogViewModelBase
{
    public SkillRuleEditorDialogViewModel(SkillRulesViewModel useRules)
        : base("Edit Rules")
    {
        UseRules = useRules;
    }

    public SkillRulesViewModel UseRules { get; }
}
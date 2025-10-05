using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Skua.Core.ViewModels;
public partial class SkillRulesViewModel : ObservableRecipient
{
    public SkillRulesViewModel() { }

    public SkillRulesViewModel(SkillRulesViewModel rules)
    {
        _useRuleBool = rules.UseRuleBool;
        _waitUseValue = rules.WaitUseValue;
        _healthGreaterThanBool = rules.HealthGreaterThanBool;
        _healthUseValue = rules.HealthUseValue;
        _manaGreaterThanBool = rules.ManaGreaterThanBool;
        _manaUseValue = rules.ManaUseValue;
        _skipUseBool = rules.SkipUseBool;
        _auraName = rules.AuraName;
        _auraComparison = rules.AuraComparison;
        _auraValue = rules.AuraValue;
        _targetAuraName = rules.TargetAuraName;
        _targetAuraComparison = rules.TargetAuraComparison;
        _targetAuraValue = rules.TargetAuraValue;
    }

    [ObservableProperty]
    private bool _useRuleBool;

    [ObservableProperty]
    private bool _healthGreaterThanBool = true;
    private int _healthUseValue;
    public int HealthUseValue
    {
        get { return _healthUseValue; }
        set
        {
            if (value is < 0 or > 100)
                return;
            SetProperty(ref _healthUseValue, value);
        }
    }

    [ObservableProperty]
    private bool _manaGreaterThanBool = true;
    private int _manaUseValue;
    public int ManaUseValue
    {
        get { return _manaUseValue; }
        set
        {
            if (value is < 0 or > 100)
                return;
            SetProperty(ref _manaUseValue, value);
        }
    }
    [ObservableProperty]
    private int _waitUseValue;
    [ObservableProperty]
    private bool _skipUseBool;

    [ObservableProperty]
    private string? _auraName;
    [ObservableProperty]
    private string _auraComparison = ">";
    [ObservableProperty]
    private int _auraValue;

    [ObservableProperty]
    private string? _targetAuraName;
    [ObservableProperty]
    private string _targetAuraComparison = ">";
    [ObservableProperty]
    private int _targetAuraValue;

    [RelayCommand]
    private void ResetUseRules()
    {
        UseRuleBool = false;
        HealthGreaterThanBool = true;
        HealthUseValue = 0;
        ManaGreaterThanBool = true;
        ManaUseValue = 0;
        WaitUseValue = 0;
        SkipUseBool = false;
        AuraName = null;
        AuraComparison = ">";
        AuraValue = 0;
        TargetAuraName = null;
        TargetAuraComparison = ">";
        TargetAuraValue = 0;
    }
}

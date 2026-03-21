using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Skua.Core.Skills;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Skua.App.Avalonia.ViewModels.AdvancedSkills;

public partial class SkillRulesViewModel : ObservableRecipient
{
    public SkillRulesViewModel()
    { }

    public SkillRulesViewModel(SkillRulesViewModel rules)
    {
        UseRuleBool = rules.UseRuleBool;
        WaitUseValue = rules.WaitUseValue;
        HealthGreaterThanBool = rules.HealthGreaterThanBool;
        HealthUseValue = rules.HealthUseValue;
        HealthIsPercentage = rules.HealthIsPercentage;
        ManaGreaterThanBool = rules.ManaGreaterThanBool;
        ManaUseValue = rules.ManaUseValue;
        ManaIsPercentage = rules.ManaIsPercentage;
        AuraGreaterThanBool = rules.AuraGreaterThanBool;
        AuraUseValue = rules.AuraUseValue;
        AuraTargetIndex = rules.AuraTargetIndex;
        AuraName = rules.AuraName;
        SkipUseBool = rules.SkipUseBool;
        PartyMemberHealthGreaterThanBool = rules.PartyMemberHealthGreaterThanBool;
        PartyMemberHealthUseValue = rules.PartyMemberHealthUseValue;
        PartyMemberHealthIsPercentage = rules.PartyMemberHealthIsPercentage;
        MultiAuraBool = rules.MultiAuraBool;
        MultiAuraOperatorIndex = rules.MultiAuraOperatorIndex;
        foreach (AuraCheckViewModel check in rules.MultiAuraChecks)
        {
            MultiAuraChecks.Add(new AuraCheckViewModel(check));
        }
    }

    [ObservableProperty]
    private bool _useRuleBool;

    [ObservableProperty]
    private bool _multiAuraBool;

    [ObservableProperty]
    private bool _healthGreaterThanBool = true;

    private int _healthUseValue;

    public int HealthUseValue
    {
        get => _healthUseValue;
        set
        {
            if (value < 0)
                return;
            SetProperty(ref _healthUseValue, value);
        }
    }

    [ObservableProperty]
    private bool _healthIsPercentage = true;

    [ObservableProperty]
    private bool _manaGreaterThanBool = true;

    private int _manaUseValue;

    public int ManaUseValue
    {
        get => _manaUseValue;
        set
        {
            if (value < 0)
                return;
            SetProperty(ref _manaUseValue, value);
        }
    }

    [ObservableProperty]
    private bool _manaIsPercentage = true;

    [ObservableProperty]
    private int _waitUseValue;

    [ObservableProperty]
    private bool _skipUseBool;

    [ObservableProperty]
    private bool _auraGreaterThanBool = true;

    private float _auraUseValue;

    public float AuraUseValue
    {
        get => _auraUseValue; set => SetProperty(ref _auraUseValue, value);
    }

    [ObservableProperty]
    private int _auraTargetIndex = 0;

    [ObservableProperty]
    private string _auraName = string.Empty;

    [ObservableProperty]
    private bool _partyMemberHealthGreaterThanBool = true;

    private int _partyMemberHealthUseValue;

    public int PartyMemberHealthUseValue
    {
        get => _partyMemberHealthUseValue;
        set
        {
            if (value < 0)
                return;
            SetProperty(ref _partyMemberHealthUseValue, value);
        }
    }

    [ObservableProperty]
    private bool _partyMemberHealthIsPercentage = true;

    [ObservableProperty]
    private ObservableCollection<AuraCheckViewModel> _multiAuraChecks = new();

    [ObservableProperty]
    private int _multiAuraOperatorIndex = 0;

    [RelayCommand]
    private void AddAuraCheck()
    {
        MultiAuraChecks.Add(new AuraCheckViewModel());
    }

    [RelayCommand]
    private void RemoveAuraCheck(AuraCheckViewModel check)
    {
        MultiAuraChecks.Remove(check);
        if (MultiAuraChecks.Count == 0)
            MultiAuraBool = false;
    }

    public UseRule[] ToUseRules()
    {
        List<UseRule> rules = new();

        if (MultiAuraChecks.Count > 0 && MultiAuraBool)
        {
            List<AuraCheck> auraChecks = new();
            foreach (AuraCheckViewModel check in MultiAuraChecks)
            {
                auraChecks.Add(check.ToAuraCheck());
            }
            MultiAuraOperator op = MultiAuraOperatorIndex switch
            {
                1 => MultiAuraOperator.Or,
                _ => MultiAuraOperator.And
            };
            rules.Add(new UseRule(SkillRule.MultiAura, SkipUseBool, auraChecks, op));
        }
        else if (!string.IsNullOrEmpty(AuraName) || AuraUseValue != 0)
        {
            string target = AuraTargetIndex == 1 ? "target" : "self";
            rules.Add(new UseRule(SkillRule.Aura, SkipUseBool, AuraName, target, AuraGreaterThanBool, AuraUseValue));
        }

        if (HealthUseValue != 0)
            rules.Add(new UseRule(SkillRule.Health, HealthGreaterThanBool, HealthUseValue, SkipUseBool, HealthIsPercentage));

        if (ManaUseValue != 0)
            rules.Add(new UseRule(SkillRule.Mana, ManaGreaterThanBool, ManaUseValue, SkipUseBool, ManaIsPercentage));

        if (PartyMemberHealthUseValue != 0)
            rules.Add(new UseRule(SkillRule.PartyHealth, PartyMemberHealthGreaterThanBool, PartyMemberHealthUseValue, SkipUseBool, PartyMemberHealthIsPercentage));

        if (WaitUseValue != 0)
            rules.Add(new UseRule(SkillRule.Wait, true, WaitUseValue, SkipUseBool));

        return rules.Count > 0 ? rules.ToArray() : new[] { new UseRule(SkillRule.None) };
    }

    [RelayCommand]
    private void ResetUseRules()
    {
        UseRuleBool = false;
        HealthGreaterThanBool = true;
        HealthUseValue = 0;
        HealthIsPercentage = true;
        ManaGreaterThanBool = true;
        ManaUseValue = 0;
        ManaIsPercentage = true;
        WaitUseValue = 0;
        AuraGreaterThanBool = true;
        AuraUseValue = 0;
        AuraTargetIndex = 0;
        AuraName = string.Empty;
        SkipUseBool = false;
        PartyMemberHealthGreaterThanBool = true;
        PartyMemberHealthUseValue = 0;
        PartyMemberHealthIsPercentage = true;
        MultiAuraBool = false;
        MultiAuraChecks.Clear();
        MultiAuraOperatorIndex = 0;
    }
}
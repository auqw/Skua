using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Skua.Core.Utils;

namespace Skua.Core.ViewModels;
public class SkillItemViewModel : ObservableObject
{
    public SkillItemViewModel(int skill, bool useRule, int waitValue, bool healthGreaterThanBool, int healthValue, bool manaGreaterThanBool, int manaValue, bool skipBool, string? auraName = null, string auraComparison = ">", int auraValue = 0, string? targetAuraName = null, string targetAuraComparison = ">", int targetAuraValue = 0)
    {
        Skill = skill;
        _useRules = new SkillRulesViewModel()
        {
            UseRuleBool = useRule,
            WaitUseValue = waitValue,
            HealthGreaterThanBool = healthGreaterThanBool,
            HealthUseValue = healthValue,
            ManaGreaterThanBool = manaGreaterThanBool,
            ManaUseValue = manaValue,
            SkipUseBool = skipBool,
            AuraName = auraName,
            AuraComparison = auraComparison,
            AuraValue = auraValue,
            TargetAuraName = targetAuraName,
            TargetAuraComparison = targetAuraComparison,
            TargetAuraValue = targetAuraValue
        };
        _displayString = ToString();
    }
    public SkillItemViewModel(int skill, SkillRulesViewModel useRules)
    {
        Skill = skill;
        _useRules = new SkillRulesViewModel()
        {
            UseRuleBool = useRules.UseRuleBool,
            WaitUseValue = useRules.WaitUseValue,
            HealthGreaterThanBool = useRules.HealthGreaterThanBool,
            HealthUseValue = useRules.HealthUseValue,
            ManaGreaterThanBool = useRules.ManaGreaterThanBool,
            ManaUseValue = useRules.ManaUseValue,
            SkipUseBool = useRules.SkipUseBool,
            AuraName = useRules.AuraName,
            AuraComparison = useRules.AuraComparison,
            AuraValue = useRules.AuraValue,
            TargetAuraName = useRules.TargetAuraName,
            TargetAuraComparison = useRules.TargetAuraComparison,
            TargetAuraValue = useRules.TargetAuraValue
        };
        _displayString = ToString();
    }

    public SkillItemViewModel(string skill)
    {
        string[] skillRules = skill[1..].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        Skill = int.Parse(skill.AsSpan(0, 1));
        bool useRule = false, healthGreater = false, manaGreater = false, skip = false;
        int waitVal = 0, healthVal = 0, manaVal = 0;
        string? auraName = null, targetAuraName = null;
        string auraComparison = ">", targetAuraComparison = ">";
        int auraVal = 0, targetAuraVal = 0;
        
        for (int i = 0; i < skillRules.Length; i++)
        {
            if (skillRules[i].Contains('W'))
            {
                useRule = true;
                waitVal = int.Parse(skillRules[i].RemoveLetters());
            }
            else if (skillRules[i].Contains('H'))
            {
                useRule = true;
                if (skillRules[i].Contains('>'))
                    healthGreater = true;
                healthVal = int.Parse(skillRules[i].RemoveLetters());
            }
            else if (skillRules[i].Contains('M'))
            {
                useRule = true;
                if (skillRules[i].Contains('>'))
                    manaGreater = true;
                manaVal = int.Parse(skillRules[i].RemoveLetters());
            }
            else if (skillRules[i].StartsWith("TA:", StringComparison.OrdinalIgnoreCase))
            {
                useRule = true;
                var auraRule = ParseAuraRule(skillRules[i][3..]);
                if (auraRule.HasValue)
                {
                    targetAuraName = auraRule.Value.Name;
                    targetAuraComparison = auraRule.Value.Comparison;
                    targetAuraVal = auraRule.Value.Value;
                }
            }
            else if (skillRules[i].StartsWith("A:", StringComparison.OrdinalIgnoreCase))
            {
                useRule = true;
                var auraRule = ParseAuraRule(skillRules[i][2..]);
                if (auraRule.HasValue)
                {
                    auraName = auraRule.Value.Name;
                    auraComparison = auraRule.Value.Comparison;
                    auraVal = auraRule.Value.Value;
                }
            }

            if (skillRules[i].Contains('S'))
                useRule = skip = true;
        }
        _useRules = new SkillRulesViewModel()
        {
            UseRuleBool = useRule,
            WaitUseValue = waitVal,
            HealthGreaterThanBool = healthGreater,
            HealthUseValue = healthVal,
            ManaGreaterThanBool = manaGreater,
            ManaUseValue = manaVal,
            SkipUseBool = skip,
            AuraName = auraName,
            AuraComparison = auraComparison,
            AuraValue = auraVal,
            TargetAuraName = targetAuraName,
            TargetAuraComparison = targetAuraComparison,
            TargetAuraValue = targetAuraVal
        };
        _displayString = ToString();
    }
    
    private (string Name, string Comparison, int Value)? ParseAuraRule(string rule)
    {
        string comparison = ">";
        int splitIndex = -1;
        
        if (rule.Contains('>'))
        {
            comparison = ">";
            splitIndex = rule.IndexOf('>');
        }
        else if (rule.Contains('<'))
        {
            comparison = "<";
            splitIndex = rule.IndexOf('<');
        }
        else if (rule.Contains('='))
        {
            comparison = "=";
            splitIndex = rule.IndexOf('=');
        }
        
        if (splitIndex > 0)
        {
            string name = rule.Substring(0, splitIndex);
            string valueStr = rule.Substring(splitIndex + 1);
            if (int.TryParse(valueStr, out int value))
            {
                return (name, comparison, value);
            }
        }
        
        return null;
    }

    private SkillRulesViewModel _useRules;
    public SkillRulesViewModel UseRules
    {
        get { return _useRules; }
        set
        {
            _useRules = value;
            DisplayString = ToString();
        }
    }
    public int Skill { get; }

    private string _displayString;
    public string DisplayString
    {
        get { return _displayString; }
        set { SetProperty(ref _displayString, value); }
    }

    public override string ToString()
    {
        StringBuilder bob = new();
        bob.Append(Skill);

        if(!UseRules.UseRuleBool)
            return bob.ToString();
        
        if(UseRules.WaitUseValue != 0)
            bob.Append($" - [Wait for {UseRules.WaitUseValue}]");
        
        if(UseRules.HealthUseValue != 0)
        {
            bob.Append(" - [Health");
            _ = UseRules.HealthGreaterThanBool ? bob.Append(" > ") : bob.Append(" < ");
            bob.Append(UseRules.HealthUseValue);
            bob.Append("%]");
        }
        
        if (UseRules.ManaUseValue != 0)
        {
            bob.Append(" - [Mana");
            _ = UseRules.ManaGreaterThanBool ? bob.Append(" > ") : bob.Append(" < ");
            bob.Append(UseRules.ManaUseValue);
            bob.Append("%]");
        }
        
        if (!string.IsNullOrEmpty(UseRules.AuraName))
        {
            bob.Append($" - [Aura '{UseRules.AuraName}' {UseRules.AuraComparison} {UseRules.AuraValue}]");
        }
        
        if (!string.IsNullOrEmpty(UseRules.TargetAuraName))
        {
            bob.Append($" - [Target Aura '{UseRules.TargetAuraName}' {UseRules.TargetAuraComparison} {UseRules.TargetAuraValue}]");
        }
        
        if(UseRules.SkipUseBool)
            bob.Append(" - [Skip if not available]");
        
        return bob.ToString();
    }

    public string Convert()
    {
        StringBuilder bob = new();
        bob.Append(Skill);
        if (!UseRules.UseRuleBool)
            return bob.ToString();
        if (UseRules.WaitUseValue != 0)
            bob.Append($" WW{UseRules.WaitUseValue}");
        if (UseRules.HealthUseValue != 0)
            bob.Append($" H{(UseRules.HealthGreaterThanBool ? ">" : "<")}{UseRules.HealthUseValue}");
        if (UseRules.ManaUseValue != 0)
            bob.Append($" M{(UseRules.ManaGreaterThanBool ? ">" : "<")}{UseRules.ManaUseValue}");
        if (!string.IsNullOrEmpty(UseRules.AuraName))
            bob.Append($" A:{UseRules.AuraName}{UseRules.AuraComparison}{UseRules.AuraValue}");
        if (!string.IsNullOrEmpty(UseRules.TargetAuraName))
            bob.Append($" TA:{UseRules.TargetAuraName}{UseRules.TargetAuraComparison}{UseRules.TargetAuraValue}");
        if (UseRules.SkipUseBool)
            bob.Append('S');
        return bob.ToString();
    }
}

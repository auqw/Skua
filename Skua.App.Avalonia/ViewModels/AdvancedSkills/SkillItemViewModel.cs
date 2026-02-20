using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Skua.App.Avalonia.ViewModels.AdvancedSkills;

public class SkillItemViewModel : ObservableObject
{
    public SkillItemViewModel(int skill, bool useRule, int waitValue, bool healthGreaterThanBool, int healthValue, bool manaGreaterThanBool, bool auraGreaterThanBool, int manaValue, bool skipBool)
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
            AuraGreaterThanBool = auraGreaterThanBool
        };
        _displayString = ToString();
    }

    public SkillItemViewModel(int skill, SkillRulesViewModel useRules)
    {
        Skill = skill;
        _useRules = new SkillRulesViewModel(useRules);
        _displayString = ToString();
    }

    public SkillItemViewModel(string skill)
    {
        Skill = int.Parse(skill.AsSpan(0, 1));
        string rest = skill[1..].Trim();
        bool useRule = false, healthGreater = false, manaGreater = false, auraGreater = false, skip = false;
        bool healthIsPercentage = true, manaIsPercentage = true, partyHealthIsPercentage = true, partyHealthGreater = false;
        int waitVal = 0, healthVal = 0, manaVal = 0, auraTargetIndex = 0, partyHealthVal = 0;
        float auraVal = 0;
        string auraName = string.Empty;
        List<AuraCheckViewModel> multiAuraChecks = new();
        int multiAuraOp = 0;

        if (string.IsNullOrEmpty(rest))
        {
            _useRules = new SkillRulesViewModel();
            _displayString = ToString();
            return;
        }

        int pos = 0;
        while (pos < rest.Length)
        {
            if (pos + 1 < rest.Length && rest[pos] == 'W' && rest[pos + 1] == 'W')
            {
                useRule = true;
                pos += 2;
                int numStart = pos;
                while (pos < rest.Length && char.IsDigit(rest[pos]))
                    pos++;
                if (pos > numStart)
                    waitVal = int.Parse(rest[numStart..pos]);
                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;
            }
            else if (pos + 1 < rest.Length && rest[pos] == 'P' && rest[pos + 1] == 'H')
            {
                useRule = true;
                pos += 2;
                if (pos < rest.Length && rest[pos] == '>')
                {
                    partyHealthGreater = true;
                    pos++;
                }
                else if (pos < rest.Length && rest[pos] == '<')
                {
                    pos++;
                }
                int numStart = pos;
                while (pos < rest.Length && char.IsDigit(rest[pos]))
                    pos++;
                if (pos > numStart)
                    partyHealthVal = int.Parse(rest[numStart..pos]);
                if (pos < rest.Length && rest[pos] == '#')
                {
                    partyHealthIsPercentage = false;
                    pos++;
                }
                else if (pos < rest.Length && rest[pos] == '%')
                {
                    pos++;
                }
                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;
            }
            else if (rest[pos] == 'H')
            {
                useRule = true;
                pos++;
                if (pos < rest.Length && rest[pos] == '>')
                {
                    healthGreater = true;
                    pos++;
                }
                else if (pos < rest.Length && rest[pos] == '<')
                {
                    pos++;
                }
                int numStart = pos;
                while (pos < rest.Length && char.IsDigit(rest[pos]))
                    pos++;
                if (pos > numStart)
                    healthVal = int.Parse(rest[numStart..pos]);
                if (pos < rest.Length && rest[pos] == '#')
                {
                    healthIsPercentage = false;
                    pos++;
                }
                else if (pos < rest.Length && rest[pos] == '%')
                {
                    pos++;
                }
                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;
            }
            else if (pos + 1 < rest.Length && rest[pos] == 'M' && rest[pos + 1] == 'A')
            {
                useRule = true;
                pos += 2;
                if (pos < rest.Length && rest[pos] == '>')
                {
                    auraGreater = true;
                    pos++;
                }
                else if (pos < rest.Length && rest[pos] == '<')
                {
                    pos++;
                }

                string mauraName = "";
                float mauraVal = 0;
                bool mauraGreater = auraGreater;
                int mauraTargetIndex = 0;

                if (pos < rest.Length && rest[pos] == '"')
                {
                    pos++;
                    int nameStart = pos;
                    while (pos < rest.Length && rest[pos] != '"')
                    {
                        if (rest[pos] == '\\' && pos + 1 < rest.Length && rest[pos + 1] == '"')
                            pos += 2;
                        else
                            pos++;
                    }
                    string rawName = rest[nameStart..pos];
                    mauraName = rawName.Replace("\\\"", "\"").Trim();
                    if (pos < rest.Length && rest[pos] == '"')
                        pos++;
                }

                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;

                int valueStart = pos;
                while (pos < rest.Length && (char.IsDigit(rest[pos]) || (pos < rest.Length && rest[pos] == '.')))
                    pos++;
                if (valueStart < pos && float.TryParse(rest[valueStart..pos], out float mauraValParsed))
                    mauraVal = mauraValParsed;

                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;

                if (pos < rest.Length && char.IsLetter(rest[pos]))
                {
                    int targetEnd = pos;
                    while (targetEnd < rest.Length && char.IsLetter(rest[targetEnd]))
                        targetEnd++;
                    if (rest[pos..targetEnd].Contains("TARGET", StringComparison.OrdinalIgnoreCase))
                        mauraTargetIndex = 1;
                    pos = targetEnd;
                }

                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;

                if (pos < rest.Length && (rest[pos] == '&' || rest[pos] == ':'))
                {
                    if (multiAuraChecks.Count == 0)
                    {
                        multiAuraOp = rest[pos] switch
                        {
                            ':' => 1,
                            _ => 0
                        };
                    }
                    pos++;
                }

                if (!string.IsNullOrEmpty(mauraName))
                {
                    multiAuraChecks.Add(new()
                    {
                        AuraName = mauraName,
                        StackCount = mauraVal,
                        IsGreater = mauraGreater,
                        AuraTargetIndex = mauraTargetIndex
                    });
                }

                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;
            }
            else if (rest[pos] == 'M')
            {
                useRule = true;
                pos++;
                if (pos < rest.Length && rest[pos] == '>')
                {
                    manaGreater = true;
                    pos++;
                }
                else if (pos < rest.Length && rest[pos] == '<')
                {
                    pos++;
                }
                int numStart = pos;
                while (pos < rest.Length && char.IsDigit(rest[pos]))
                    pos++;
                if (pos > numStart)
                    manaVal = int.Parse(rest[numStart..pos]);
                if (pos < rest.Length && rest[pos] == '#')
                {
                    manaIsPercentage = false;
                    pos++;
                }
                else if (pos < rest.Length && rest[pos] == '%')
                {
                    pos++;
                }
                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;
            }
            else if (rest[pos] == 'A')
            {
                useRule = true;
                pos++;
                if (pos < rest.Length && rest[pos] == '>')
                {
                    auraGreater = true;
                    pos++;
                }
                else if (pos < rest.Length && rest[pos] == '<')
                {
                    pos++;
                }

                if (pos < rest.Length && rest[pos] == '"')
                {
                    pos++;
                    int nameStart = pos;
                    while (pos < rest.Length && rest[pos] != '"')
                    {
                        if (rest[pos] == '\\' && pos + 1 < rest.Length && rest[pos + 1] == '"')
                            pos += 2;
                        else
                            pos++;
                    }
                    string rawName = rest[nameStart..pos];
                    auraName = rawName.Replace("\\\"", "\"").Trim();
                    if (pos < rest.Length && rest[pos] == '"')
                        pos++;

                    while (pos < rest.Length && rest[pos] == ' ')
                        pos++;

                    int valueStart = pos;
                    while (pos < rest.Length && (char.IsDigit(rest[pos]) || (pos < rest.Length && rest[pos] == '.')))
                        pos++;
                    if (valueStart < pos && float.TryParse(rest[valueStart..pos], out float auraValParsed))
                        auraVal = auraValParsed;
                }
                else
                {
                    int nameStart = pos;
                    int lastDigitStart = -1;
                    int lastDigitEnd = -1;

                    while (pos < rest.Length && rest[pos] != ' ')
                    {
                        if (char.IsDigit(rest[pos]))
                        {
                            if (lastDigitStart == -1)
                                lastDigitStart = pos;
                            lastDigitEnd = pos + 1;
                        }
                        else if (lastDigitStart != -1 && lastDigitEnd == pos)
                        {
                            lastDigitStart = -1;
                        }
                        pos++;
                    }

                    while (pos < rest.Length && rest[pos] == ' ')
                        pos++;

                    int valueStart = pos;
                    while (pos < rest.Length && (char.IsDigit(rest[pos]) || (pos < rest.Length && rest[pos] == '.')))
                        pos++;

                    if (lastDigitStart >= 0 && lastDigitEnd > lastDigitStart)
                    {
                        auraName = rest[nameStart..lastDigitStart].Trim();
                        if (float.TryParse(rest[lastDigitStart..lastDigitEnd], out float auraValParsed1))
                            auraVal = auraValParsed1;
                    }
                    else if (valueStart < pos && float.TryParse(rest[valueStart..pos], out float auraValParsed2))
                    {
                        auraName = rest[nameStart..valueStart].Trim();
                        auraVal = auraValParsed2;
                    }
                }

                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;

                if (pos < rest.Length && char.IsLetter(rest[pos]))
                {
                    int targetEnd = pos;
                    while (targetEnd < rest.Length && char.IsLetter(rest[targetEnd]))
                        targetEnd++;
                    if (rest[pos..targetEnd].Contains("TARGET", StringComparison.OrdinalIgnoreCase))
                        auraTargetIndex = 1;
                    pos = targetEnd;
                }

                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;
            }
            else if (rest[pos] == 'S')
            {
                useRule = skip = true;
                pos++;
                while (pos < rest.Length && rest[pos] == ' ')
                    pos++;
            }
            else
            {
                pos++;
            }
        }
        _useRules = new SkillRulesViewModel()
        {
            UseRuleBool = useRule,
            WaitUseValue = waitVal,
            HealthGreaterThanBool = healthGreater,
            HealthUseValue = healthVal,
            HealthIsPercentage = healthIsPercentage,
            ManaGreaterThanBool = manaGreater,
            ManaUseValue = manaVal,
            ManaIsPercentage = manaIsPercentage,
            PartyMemberHealthGreaterThanBool = partyHealthGreater,
            PartyMemberHealthUseValue = partyHealthVal,
            PartyMemberHealthIsPercentage = partyHealthIsPercentage,
            AuraGreaterThanBool = auraGreater,
            AuraUseValue = multiAuraChecks.Count > 0 ? 0 : auraVal,
            AuraTargetIndex = auraTargetIndex,
            AuraName = multiAuraChecks.Count > 0 ? string.Empty : auraName,
            SkipUseBool = skip,
            MultiAuraBool = multiAuraChecks.Count > 0,
            MultiAuraOperatorIndex = multiAuraOp
        };
        foreach (AuraCheckViewModel check in multiAuraChecks)
        {
            _useRules.MultiAuraChecks.Add(check);
        }
        _displayString = ToString();
    }

    private SkillRulesViewModel _useRules;

    public SkillRulesViewModel UseRules
    {
        get => _useRules;
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
        get => _displayString;
        set => SetProperty(ref _displayString, value);
    }

    public override string ToString()
    {
        StringBuilder bob = new();
        bob.Append(Skill);

        if (!UseRules.UseRuleBool)
            return bob.ToString();

        if (UseRules.WaitUseValue != 0)
            bob.Append($" - [Wait for {UseRules.WaitUseValue}]");

        if (UseRules.HealthUseValue != 0)
        {
            bob.Append(" - [Health");
            _ = UseRules.HealthGreaterThanBool ? bob.Append(" > ") : bob.Append(" < ");
            bob.Append(UseRules.HealthUseValue);
            bob.Append(UseRules.HealthIsPercentage ? "%]" : "#]");
        }

        if (UseRules.ManaUseValue != 0)
        {
            bob.Append(" - [Mana");
            _ = UseRules.ManaGreaterThanBool ? bob.Append(" > ") : bob.Append(" < ");
            bob.Append(UseRules.ManaUseValue);
            bob.Append(UseRules.ManaIsPercentage ? "%]" : "#]");
        }

        if (UseRules.PartyMemberHealthUseValue != 0)
        {
            bob.Append(" - [Party HP");
            _ = UseRules.PartyMemberHealthGreaterThanBool ? bob.Append(" > ") : bob.Append(" < ");
            bob.Append(UseRules.PartyMemberHealthUseValue);
            bob.Append(UseRules.PartyMemberHealthIsPercentage ? "%]" : "#]");
        }

        if (UseRules.MultiAuraBool && UseRules.MultiAuraChecks.Count > 0)
        {
            string opStr = UseRules.MultiAuraOperatorIndex switch
            {
                1 => "OR",
                _ => "AND"
            };
            bob.Append($" - [Multi-Aura ({opStr})");
            foreach (AuraCheckViewModel check in UseRules.MultiAuraChecks)
            {
                string target = check.AuraTargetIndex == 1 ? "Tgt" : "Self";
                bob.Append($" '{check.AuraName}'{(check.IsGreater ? ">" : "<")}{check.StackCount}");
            }
            bob.Append(']');
        }
        else if (UseRules.AuraUseValue != 0 || !string.IsNullOrEmpty(UseRules.AuraName))
        {
            string target = UseRules.AuraTargetIndex == 1 ? "Target" : "Self";
            bob.Append($" - [Aura ({target})");
            if (!string.IsNullOrEmpty(UseRules.AuraName))
                bob.Append($" '{UseRules.AuraName}'");
            _ = UseRules.AuraGreaterThanBool ? bob.Append(" > ") : bob.Append(" < ");
            bob.Append(UseRules.AuraUseValue);
            bob.Append(']');
        }

        if (UseRules.SkipUseBool)
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
            bob.Append($" H{(UseRules.HealthGreaterThanBool ? ">" : "<")}{UseRules.HealthUseValue}{(UseRules.HealthIsPercentage ? "%" : "#")}");
        if (UseRules.ManaUseValue != 0)
            bob.Append($" M{(UseRules.ManaGreaterThanBool ? ">" : "<")}{UseRules.ManaUseValue}{(UseRules.ManaIsPercentage ? "%" : "#")}");
        if (UseRules.PartyMemberHealthUseValue != 0)
            bob.Append($" PH{(UseRules.PartyMemberHealthGreaterThanBool ? ">" : "<")}{UseRules.PartyMemberHealthUseValue}{(UseRules.PartyMemberHealthIsPercentage ? "%" : "#")}");
        if (UseRules.MultiAuraBool && UseRules.MultiAuraChecks.Count > 0)
        {
            string opChar = UseRules.MultiAuraOperatorIndex switch
            {
                1 => ":",
                _ => "&"
            };
            for (int i = 0; i < UseRules.MultiAuraChecks.Count; i++)
            {
                AuraCheckViewModel check = UseRules.MultiAuraChecks[i];
                string suffix = i < UseRules.MultiAuraChecks.Count - 1 ? opChar : "";
                string target = check.AuraTargetIndex == 1 ? " TARGET" : string.Empty;
                bob.Append($" MA{(check.IsGreater ? ">" : "<")}\"{check.AuraName}\" {check.StackCount}{target}{suffix}");
            }
        }
        else if (!string.IsNullOrEmpty(UseRules.AuraName))
        {
            string target = UseRules.AuraTargetIndex == 1 ? " TARGET" : string.Empty;
            bob.Append($" A{(UseRules.AuraGreaterThanBool ? ">" : "<")}\"{UseRules.AuraName}\" {UseRules.AuraUseValue}{target}");
        }
        if (UseRules.SkipUseBool)
            bob.Append('S');
        return bob.ToString();
    }
}
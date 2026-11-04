
using Skua.Core.Models.Skills;
using System.Text.Json;

namespace Skua.Core.Skills;

public static class AdvancedSkillsParser
{
    public static AdvancedSkillsConfigJson ParseTextToJson(string textContent)
    {
        AdvancedSkillsConfigJson config = new();
        string[] lines = textContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        foreach (string line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            (string className, string classUseMode, string skillsStr, int skillTimeout, string skillUseMode)? entry = ParseLine(line);
            if (entry == null)
                continue;

            (string? className, string? classUseMode, string? skillsStr, int skillTimeout, string? skillUseMode) = entry.Value;

            if (!config.ContainsKey(className))
                config[className] = new Dictionary<string, SkillModeJson>();

            SkillModeJson modeEntry = new()
            {
                SkillUseMode = skillUseMode,
                SkillTimeout = skillTimeout,
                Skills = ParseSkills(skillsStr)
            };

            config[className][classUseMode] = modeEntry;
        }

        return config;
    }

    private static (string className, string classUseMode, string skillsStr, int skillTimeout, string skillUseMode)? ParseLine(string line)
    {
        string[] parts = line.Split(new[] { '=' }, 4);
        if (parts.Length < 3)
            return null;

        string classUseMode = parts[0].Trim();
        string className = parts[1].Trim();
        string skillsStr = parts[2].Trim();

        int skillTimeout = 100;
        string skillUseMode = "UseIfAvailable";

        if (parts.Length == 4)
        {
            string lastPart = parts[3].Trim();

            if (lastPart.Equals("Use if Available", StringComparison.OrdinalIgnoreCase))
            {
                skillUseMode = "UseIfAvailable";
            }
            else if (int.TryParse(string.Concat(lastPart.Where(char.IsDigit)), out int timeout))
            {
                skillTimeout = timeout;
                skillUseMode = "WaitForCooldown";
            }
        }

        return (className, classUseMode, skillsStr, skillTimeout, skillUseMode);
    }

    private static List<AdvancedSkillJson> ParseSkills(string skillsStr)
    {
        List<AdvancedSkillJson> skills = new();
        string[] skillEntries = skillsStr.Split('|');

        foreach (string skillEntry in skillEntries)
        {
            string trimmed = skillEntry.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            if (!int.TryParse(trimmed.AsSpan(0, 1), out int skillId))
                continue;

            AdvancedSkillJson skill = new() { SkillId = skillId };

            if (trimmed.Length > 1)
            {
                string rulesPart = trimmed[1..].Trim();
                bool hasSkipFlag = rulesPart.EndsWith('S') || rulesPart.EndsWith('s');
                List<SkillRuleJson>? rules = ParseRules(rulesPart, out string multiAuraOperator);
                skill.Rules = rules?.Count > 0 ? rules : null;
                skill.SkipOnMatch = hasSkipFlag;
                if (SkillRuleText.HasOr(rulesPart))
                    skill.RuleOperator = "OR";

                bool hasMultiAura = rules?.Any(r => r.Type == "MultiAura") ?? false;
                skill.MultiAura = hasMultiAura;
                if (hasMultiAura)
                {
                    skill.MultiAuraOperator = multiAuraOperator;
                }
            }

            skills.Add(skill);
        }

        return skills;
    }

    private static List<SkillRuleJson> ParseRules(string rulesPart, out string multiAuraOperator)
    {
        List<SkillRuleJson> rules = new();
        multiAuraOperator = "AND";
        bool skipOnMatch = rulesPart.EndsWith('S') || rulesPart.EndsWith('s');

        int pos = 0;
        List<SkillRuleJson> singleAuraRules = new();
        List<SkillRuleJson> multiAuraRules = new();
        while (pos < rulesPart.Length)
        {
            if (char.ToUpper(rulesPart[pos]) == 'W' && pos + 1 < rulesPart.Length && char.ToUpper(rulesPart[pos + 1]) == 'W')
            {
                pos += 2;
                int numStart = pos;
                while (pos < rulesPart.Length && char.IsDigit(rulesPart[pos]))
                    pos++;

                if (pos > numStart)
                {
                    int timeout = int.Parse(rulesPart[numStart..pos]);
                    SkillRuleJson waitRule = new()
                    {
                        Type = "Wait"
                    };
                    if (timeout > 0)
                        waitRule.Timeout = timeout;
                    rules.Add(waitRule);
                }

                while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                    pos++;
            }
            else if (pos + 1 < rulesPart.Length && char.ToUpper(rulesPart[pos]) == 'P' && char.ToUpper(rulesPart[pos + 1]) == 'H')
            {
                pos += 2;
                string comparison = "less";
                if (pos < rulesPart.Length && rulesPart[pos] == '>')
                {
                    comparison = "greater";
                    pos++;
                }
                else if (pos < rulesPart.Length && rulesPart[pos] == '<')
                {
                    pos++;
                }

                int numStart = pos;
                while (pos < rulesPart.Length && char.IsDigit(rulesPart[pos]))
                    pos++;

                if (pos > numStart)
                {
                    int value = int.Parse(rulesPart[numStart..pos]);
                    bool isPercentage = true;
                    if (pos < rulesPart.Length && rulesPart[pos] == '#')
                    {
                        isPercentage = false;
                        pos++;
                    }
                    else if (pos < rulesPart.Length && rulesPart[pos] == '%')
                    {
                        pos++;
                    }
                    rules.Add(new SkillRuleJson
                    {
                        Type = "PartyHealth",
                        Value = value,
                        Comparison = comparison,
                        IsPercentage = isPercentage
                    });
                }

                while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                    pos++;
            }
            else if (char.ToUpper(rulesPart[pos]) == 'H')
            {
                pos++;
                string comparison = "less";
                if (pos < rulesPart.Length && rulesPart[pos] == '>')
                {
                    comparison = "greater";
                    pos++;
                }
                else if (pos < rulesPart.Length && rulesPart[pos] == '<')
                {
                    pos++;
                }

                int numStart = pos;
                while (pos < rulesPart.Length && char.IsDigit(rulesPart[pos]))
                    pos++;

                if (pos > numStart)
                {
                    int value = int.Parse(rulesPart[numStart..pos]);
                    bool isPercentage = true;
                    if (pos < rulesPart.Length && rulesPart[pos] == '#')
                    {
                        isPercentage = false;
                        pos++;
                    }
                    else if (pos < rulesPart.Length && rulesPart[pos] == '%')
                    {
                        pos++;
                    }
                    rules.Add(new SkillRuleJson
                    {
                        Type = "Health",
                        Value = value,
                        Comparison = comparison,
                        IsPercentage = isPercentage
                    });
                }

                while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                    pos++;
            }
            else if (pos + 1 < rulesPart.Length && char.ToUpper(rulesPart[pos]) == 'M' && char.ToUpper(rulesPart[pos + 1]) == 'A')
            {
                pos += 2;
                string comparison = "less";
                if (pos < rulesPart.Length && rulesPart[pos] == '>')
                {
                    comparison = "greater";
                    pos++;
                }
                else if (pos < rulesPart.Length && rulesPart[pos] == '<')
                {
                    pos++;
                }

                string auraName = "";
                float auraValue = 0;

                if (pos < rulesPart.Length && rulesPart[pos] == '"')
                {
                    pos++;
                    int nameStart = pos;
                    while (pos < rulesPart.Length && rulesPart[pos] != '"')
                    {
                        if (rulesPart[pos] == '\\' && pos + 1 < rulesPart.Length && rulesPart[pos + 1] == '"')
                            pos += 2;
                        else
                            pos++;
                    }
                    string rawName = rulesPart[nameStart..pos];
                    auraName = rawName.Replace("\\\"", "\"").Trim();
                    if (pos < rulesPart.Length && rulesPart[pos] == '"')
                        pos++;
                }

                while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                    pos++;

                int valueStart = pos;
                while (pos < rulesPart.Length && (char.IsDigit(rulesPart[pos]) || rulesPart[pos] == '.'))
                    pos++;
                if (valueStart < pos && float.TryParse(rulesPart[valueStart..pos], out float parsedValue))
                    auraValue = parsedValue;

                while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                    pos++;

                string auraTarget = "self";
                if (pos < rulesPart.Length && char.IsLetter(rulesPart[pos]))
                {
                    int targetStart = pos;
                    while (pos < rulesPart.Length && char.IsLetter(rulesPart[pos]))
                        pos++;

                    string targetStr = rulesPart[targetStart..pos];
                    if (targetStr.Contains("TARGET", StringComparison.OrdinalIgnoreCase))
                        auraTarget = "target";
                }

                string operatorIndex = "AND";
                if (pos < rulesPart.Length && (rulesPart[pos] == '&' || rulesPart[pos] == ':'))
                {
                    operatorIndex = rulesPart[pos] switch
                    {
                        ':' => "OR",
                        _ => "AND"
                    };
                    multiAuraOperator = operatorIndex;
                    pos++;
                }

                if (!string.IsNullOrEmpty(auraName))
                {
                    SkillRuleJson rule = new()
                    {
                        Type = "MultiAura",
                        AuraName = auraName,
                        AuraTarget = auraTarget,
                        Value = auraValue,
                        Comparison = comparison
                    };
                    multiAuraRules.Add(rule);
                }

                while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                    pos++;
            }
            else if (char.ToUpper(rulesPart[pos]) == 'M')
            {
                pos++;
                string comparison = "less";
                if (pos < rulesPart.Length && rulesPart[pos] == '>')
                {
                    comparison = "greater";
                    pos++;
                }
                else if (pos < rulesPart.Length && rulesPart[pos] == '<')
                {
                    pos++;
                }

                int numStart = pos;
                while (pos < rulesPart.Length && char.IsDigit(rulesPart[pos]))
                    pos++;

                if (pos > numStart)
                {
                    int value = int.Parse(rulesPart[numStart..pos]);
                    bool isPercentage = true;
                    if (pos < rulesPart.Length && rulesPart[pos] == '#')
                    {
                        isPercentage = false;
                        pos++;
                    }
                    else if (pos < rulesPart.Length && rulesPart[pos] == '%')
                    {
                        pos++;
                    }
                    rules.Add(new SkillRuleJson
                    {
                        Type = "Mana",
                        Value = value,
                        Comparison = comparison,
                        IsPercentage = isPercentage
                    });
                }

                while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                    pos++;
            }
            else if (char.ToUpper(rulesPart[pos]) == 'A')
            {
                pos++;
                string comparison = "less";
                if (pos < rulesPart.Length && rulesPart[pos] == '>')
                {
                    comparison = "greater";
                    pos++;
                }
                else if (pos < rulesPart.Length && rulesPart[pos] == '<')
                {
                    pos++;
                }

                string auraName = "";
                float auraValue = 0;

                if (pos < rulesPart.Length && rulesPart[pos] == '"')
                {
                    pos++;
                    int nameStart = pos;
                    while (pos < rulesPart.Length && rulesPart[pos] != '"')
                    {
                        if (rulesPart[pos] == '\\' && pos + 1 < rulesPart.Length && rulesPart[pos + 1] == '"')
                            pos += 2;
                        else
                            pos++;
                    }
                    string rawName = rulesPart[nameStart..pos];
                    auraName = rawName.Replace("\\\"", "\"").Trim();
                    if (pos < rulesPart.Length && rulesPart[pos] == '"')
                        pos++;

                    while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                        pos++;

                    int valueStart = pos;
                    while (pos < rulesPart.Length && (char.IsDigit(rulesPart[pos]) || rulesPart[pos] == '.'))
                        pos++;
                    if (valueStart < pos && float.TryParse(rulesPart[valueStart..pos], out float parsedValue))
                        auraValue = parsedValue;
                }
                else
                {
                    int nameStart = pos;
                    int lastDigitStart = -1;
                    int lastDigitEnd = -1;

                    while (pos < rulesPart.Length && rulesPart[pos] != ' ')
                    {
                        if (char.IsDigit(rulesPart[pos]))
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

                    while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                        pos++;

                    int valueStart = pos;
                    while (pos < rulesPart.Length && (char.IsDigit(rulesPart[pos]) || rulesPart[pos] == '.'))
                        pos++;

                    if (lastDigitStart >= 0 && lastDigitEnd > lastDigitStart)
                    {
                        auraName = rulesPart[nameStart..lastDigitStart].Trim();
                        if (float.TryParse(rulesPart[lastDigitStart..lastDigitEnd], out float val1))
                            auraValue = val1;
                    }
                    else if (valueStart < pos && float.TryParse(rulesPart[valueStart..pos], out float val2))
                    {
                        auraName = rulesPart[nameStart..valueStart].Trim();
                        auraValue = val2;
                    }
                }

                while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                    pos++;

                string auraTarget = "self";
                if (pos < rulesPart.Length && char.IsLetter(rulesPart[pos]))
                {
                    int targetStart = pos;
                    while (pos < rulesPart.Length && char.IsLetter(rulesPart[pos]))
                        pos++;

                    string targetStr = rulesPart[targetStart..pos];
                    if (targetStr.Contains("TARGET", StringComparison.OrdinalIgnoreCase))
                        auraTarget = "target";
                }

                if (!string.IsNullOrEmpty(auraName))
                {
                    SkillRuleJson auraRule = new()
                    {
                        Type = "Aura",
                        Value = auraValue,
                        Comparison = comparison
                    };
                    if (!string.IsNullOrEmpty(auraName))
                        auraRule.AuraName = auraName;
                    if (!string.IsNullOrEmpty(auraTarget))
                        auraRule.AuraTarget = auraTarget;
                    singleAuraRules.Add(auraRule);
                }

                while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                    pos++;
            }
            else if (char.ToUpper(rulesPart[pos]) == 'S')
            {
                pos++;

                while (pos < rulesPart.Length && rulesPart[pos] == ' ')
                    pos++;
            }
            else
            {
                pos++;
            }
        }

        rules.AddRange(singleAuraRules);
        rules.AddRange(multiAuraRules);

        if (rules.Count == 0)
        {
            rules.Add(new SkillRuleJson { Type = "None" });
        }

        return rules;
    }

    public static string ConvertJsonToJson(AdvancedSkillsConfigJson config)
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(config, options);
    }

    public static List<AdvancedSkillJson> ParseSkillString(string skillsStr)
    {
        return ParseSkills(skillsStr);
    }
}
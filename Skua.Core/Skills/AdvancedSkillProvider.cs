using Skua.Core.Interfaces;
using Skua.Core.Models.Monsters;

namespace Skua.Core.Skills;

public class AdvancedSkillProvider : ISkillProvider
{
    private readonly IScriptPlayer _player;
    private readonly IScriptSelfAuras _self;
    private readonly IScriptTargetAuras _target;
    private readonly IScriptCombat _combat;
    private readonly IFlashUtil _flash;
    private readonly AdvancedSkillCommand _currentCommand;

    public AdvancedSkillProvider(IScriptPlayer player, IScriptSelfAuras self, IScriptTargetAuras target, IScriptCombat combat, IFlashUtil flash)
    {
        _player = player;
        _self = self;
        _target = target;
        _combat = combat;
        _flash = flash;
        _currentCommand = new AdvancedSkillCommand(flash);
    }

    private readonly UseRule[] _none = new[] { new UseRule(SkillRule.None) };

    public bool ResetOnTarget { get; set; } = false;
    private string? _lastTargetKey;
    public int SkillCount => _currentCommand.SkillCount;

    public (int, int) GetNextSkill()
    {
        return _currentCommand.GetNextSkill();
    }

    public void Load(string skills)
    {
        int index = 0;
        foreach (string command in skills.ToLower().Split('|'))
        {
            string trimmed = command.Trim();
            if (trimmed.Length > 0 && int.TryParse(trimmed.AsSpan(0, 1), out int skill))
            {
                _currentCommand.Skills.Add(index, skill);
                _currentCommand.UseRules.Add(trimmed.Length <= 1 ? _none : ParseUseRule(trimmed[1..]));
                ++index;
            }
        }
    }

    private UseRule[] ParseUseRule(string useRule)
    {
        if (string.IsNullOrWhiteSpace(useRule))
            return new[] { new UseRule(SkillRule.None) };

        List<UseRule> rules = new();
        List<AuraCheck> multiAuraChecks = new();
        int multiAuraOp = 0;
        bool shouldSkip = useRule.Length > 0 && useRule[^1] == 's';

        int pos = 0;
        while (pos < useRule.Length)
        {
            if (char.IsWhiteSpace(useRule[pos]))
            {
                pos++;
                continue;
            }

            if (pos + 1 < useRule.Length && useRule[pos] == 'm' && useRule[pos + 1] == 'a')
            {
                pos += 2;
                bool isGreater = false;
                if (pos < useRule.Length && useRule[pos] == '>')
                {
                    isGreater = true;
                    pos++;
                }
                else if (pos < useRule.Length && useRule[pos] == '<')
                {
                    pos++;
                }

                string auraName = "";
                float auraValue = 0;

                if (pos < useRule.Length && useRule[pos] == '"')
                {
                    pos++;
                    int nameStart = pos;
                    while (pos < useRule.Length && useRule[pos] != '"')
                    {
                        if (useRule[pos] == '\\' && pos + 1 < useRule.Length && useRule[pos + 1] == '"')
                            pos += 2;
                        else
                            pos++;
                    }
                    string rawName = useRule[nameStart..pos];
                    auraName = rawName.Replace("\\\"", "\"").Trim();
                    if (pos < useRule.Length && useRule[pos] == '"')
                        pos++;
                }

                while (pos < useRule.Length && useRule[pos] == ' ')
                    pos++;

                int numStart = pos;
                while (pos < useRule.Length && (char.IsDigit(useRule[pos]) || useRule[pos] == '.'))
                    pos++;
                if (pos > numStart && float.TryParse(useRule[numStart..pos], out float parsed))
                    auraValue = parsed;

                while (pos < useRule.Length && useRule[pos] == ' ')
                    pos++;

                string auraTarget = "self";
                if (pos < useRule.Length && char.IsLetter(useRule[pos]))
                {
                    int targetStart = pos;
                    while (pos < useRule.Length && char.IsLetter(useRule[pos]))
                        pos++;
                    if (useRule[targetStart..pos].Contains("TARGET", StringComparison.OrdinalIgnoreCase))
                        auraTarget = "target";
                }

                while (pos < useRule.Length && useRule[pos] == ' ')
                    pos++;

                if (pos < useRule.Length && (useRule[pos] == '&' || useRule[pos] == ':'))
                {
                    multiAuraOp = useRule[pos] switch
                    {
                        ':' => 1,
                        _ => 0
                    };
                    pos++;
                }

                if (!string.IsNullOrEmpty(auraName))
                {
                    multiAuraChecks.Add(new AuraCheck
                    {
                        AuraName = auraName,
                        StackCount = auraValue,
                        Greater = isGreater,
                        AuraTarget = auraTarget
                    });
                }
                continue;
            }

            if (pos + 1 < useRule.Length && (useRule[pos] == 'p' || useRule[pos] == 'P') && (useRule[pos + 1] == 'h' || useRule[pos + 1] == 'H'))
            {
                pos += 2;
                bool isGreater = pos < useRule.Length && useRule[pos] == '>';
                if (isGreater || (pos < useRule.Length && useRule[pos] == '<'))
                    pos++;
                int numStart = pos;
                while (pos < useRule.Length && char.IsDigit(useRule[pos]))
                    pos++;
                if (pos > numStart)
                {
                    int value = int.Parse(useRule[numStart..pos]);
                    bool isPercentage = true;
                    if (pos < useRule.Length && useRule[pos] == '#')
                    {
                        isPercentage = false;
                        pos++;
                    }
                    else if (pos < useRule.Length && useRule[pos] == '%')
                    {
                        pos++;
                    }
                    rules.Add(new UseRule(SkillRule.PartyHealth, isGreater, value, shouldSkip, isPercentage));
                }
                continue;
            }

            if (useRule[pos] is 'h' or 'H')
            {
                pos++;
                bool isGreater = pos < useRule.Length && useRule[pos] == '>';
                if (isGreater || (pos < useRule.Length && useRule[pos] == '<'))
                    pos++;
                int numStart = pos;
                while (pos < useRule.Length && char.IsDigit(useRule[pos]))
                    pos++;
                if (pos > numStart)
                {
                    int value = int.Parse(useRule[numStart..pos]);
                    bool isPercentage = true;
                    if (pos < useRule.Length && useRule[pos] == '#')
                    {
                        isPercentage = false;
                        pos++;
                    }
                    else if (pos < useRule.Length && useRule[pos] == '%')
                    {
                        pos++;
                    }
                    rules.Add(new UseRule(SkillRule.Health, isGreater, value, shouldSkip, isPercentage));
                }
                continue;
            }

            if (useRule[pos] is 'm' or 'M')
            {
                pos++;
                bool isGreater = pos < useRule.Length && useRule[pos] == '>';
                if (isGreater || (pos < useRule.Length && useRule[pos] == '<'))
                    pos++;
                int numStart = pos;
                while (pos < useRule.Length && char.IsDigit(useRule[pos]))
                    pos++;
                if (pos > numStart)
                {
                    int value = int.Parse(useRule[numStart..pos]);
                    bool isPercentage = true;
                    if (pos < useRule.Length && useRule[pos] == '#')
                    {
                        isPercentage = false;
                        pos++;
                    }
                    else if (pos < useRule.Length && useRule[pos] == '%')
                    {
                        pos++;
                    }
                    rules.Add(new UseRule(SkillRule.Mana, isGreater, value, shouldSkip, isPercentage));
                }
                continue;
            }

            if (useRule[pos] is 'a' or 'A')
            {
                pos++;
                bool isGreater = false;
                if (pos < useRule.Length && useRule[pos] == '>')
                {
                    isGreater = true;
                    pos++;
                }
                else if (pos < useRule.Length && useRule[pos] == '<')
                {
                    pos++;
                }

                string auraName = "";
                float auraValue = 0;

                if (pos < useRule.Length && useRule[pos] == '"')
                {
                    pos++;
                    int nameStart = pos;
                    while (pos < useRule.Length && useRule[pos] != '"')
                    {
                        if (useRule[pos] == '\\' && pos + 1 < useRule.Length && useRule[pos + 1] == '"')
                            pos += 2;
                        else
                            pos++;
                    }
                    string rawName = useRule[nameStart..pos];
                    auraName = rawName.Replace("\\\"", "\"").Trim();
                    if (pos < useRule.Length && useRule[pos] == '"')
                        pos++;
                }
                else
                {
                    int nameStart = pos;
                    while (pos < useRule.Length && useRule[pos] != ' ' && !char.IsDigit(useRule[pos]) && useRule[pos] != '.')
                        pos++;
                    auraName = useRule[nameStart..pos];
                }

                while (pos < useRule.Length && useRule[pos] == ' ')
                    pos++;

                int numStart = pos;
                while (pos < useRule.Length && (char.IsDigit(useRule[pos]) || useRule[pos] == '.'))
                    pos++;
                if (pos > numStart && float.TryParse(useRule[numStart..pos], out float parsed))
                    auraValue = parsed;

                while (pos < useRule.Length && useRule[pos] == ' ')
                    pos++;

                string auraTarget = "self";
                if (pos < useRule.Length && char.IsLetter(useRule[pos]))
                {
                    int targetStart = pos;
                    while (pos < useRule.Length && char.IsLetter(useRule[pos]))
                        pos++;
                    if (useRule[targetStart..pos].Contains("TARGET", StringComparison.OrdinalIgnoreCase))
                        auraTarget = "target";
                }

                if (!string.IsNullOrEmpty(auraName))
                {
                    rules.Add(new UseRule(SkillRule.Aura, shouldSkip, auraName, auraTarget, isGreater, auraValue));
                }
                continue;
            }

            if (useRule[pos] is 'w' or 'W')
            {
                pos++;
                if (pos < useRule.Length && useRule[pos] == 'w')
                    pos++;
                int numStart = pos;
                while (pos < useRule.Length && char.IsDigit(useRule[pos]))
                    pos++;
                if (pos > numStart)
                    rules.Add(new UseRule(SkillRule.Wait, true, int.Parse(useRule[numStart..pos]), shouldSkip));
                continue;
            }

            if (useRule[pos] is 's' or 'S')
            {
                pos++;
                continue;
            }

            pos++;
        }

        if (multiAuraChecks.Count > 0)
        {
            MultiAuraOperator op = multiAuraOp switch
            {
                1 => MultiAuraOperator.Or,
                _ => MultiAuraOperator.And
            };
            rules.Add(new UseRule(SkillRule.MultiAura, shouldSkip, multiAuraChecks, op));
        }

        return rules.Count == 0 ? new[] { new UseRule(SkillRule.None) } : rules.ToArray();
    }

    public void Save(string file)
    {
    }

    public bool OnTargetReset()
    {
        if (!ResetOnTarget)
            return false;

        string? currentTargetKey = _player.HasTarget ? GetCurrentTargetKey() : null;

        if (currentTargetKey == null)
        {
            if (_lastTargetKey != null)
            {
                _lastTargetKey = null;
                _currentCommand.Reset();
                return true;
            }

            return false;
        }

        if (_lastTargetKey == null)
        {
            _lastTargetKey = currentTargetKey;
            return false;
        }

        if (!string.Equals(_lastTargetKey, currentTargetKey, StringComparison.Ordinal))
        {
            _lastTargetKey = currentTargetKey;
            _currentCommand.Reset();
            return true;
        }

        return false;
    }

    private string? GetCurrentTargetKey()
    {
        Monster? target = _player.Target;

        if (target == null)
            return null;

        return $"{target.MapID}:{target.ID}:{target.Cell}:{target.Name}";
    }

    public bool? ShouldUseSkill(int skillIndex, bool canUse)
    {
        return _currentCommand.ShouldUse(_player, _self, _target, skillIndex, canUse);
    }

    public void Stop()
    {
        _combat.CancelAutoAttack();
        _combat.CancelTarget();
        _lastTargetKey = null;
        _currentCommand.Reset();
    }

    public void OnPlayerDeath()
    {
        _lastTargetKey = null;
        _currentCommand.Reset();
    }
}
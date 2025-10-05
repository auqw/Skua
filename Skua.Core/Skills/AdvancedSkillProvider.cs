using Skua.Core.Interfaces;
using Skua.Core.Interfaces.Auras;
using Skua.Core.Utils;
using System.Diagnostics;

namespace Skua.Core.Skills;

public class AdvancedSkillProvider : ISkillProvider
{
    private readonly IScriptPlayer _player;
    private readonly IScriptCombat _combat;
    private readonly IScriptSelfAuras? _self;
    private readonly IScriptTargetAuras? _target;
    private readonly UseRule[] _none = new[] { new UseRule(SkillRule.None) };

    public AdvancedSkillProvider(IScriptPlayer player, IScriptCombat combat, IScriptSelfAuras? self = null, IScriptTargetAuras? target = null)
    {
        _player = player;
        _combat = combat;
        _self = self;
        _target = target;
    }

    public AdvancedSkillCommand Root { get; set; } = new AdvancedSkillCommand();

    public bool ResetOnTarget { get; set; } = false;

    public (int, int) GetNextSkill()
    {
        return Root.GetNextSkill();
    }

    public void Load(string skills)
    {
        int index = 0;
        foreach (string command in skills.ToLower().Split('|').Select(s => s.Trim()).ToList())
        {
            if(int.TryParse(command.AsSpan(0, 1), out int skill))
            {
                Root.Skills.Add(index, skill);
                Root.UseRules.Add(command.Length <= 1 ? _none : ParseUseRule(command[1..]));
                ++index;
            }
        }
    }

    private UseRule[] ParseUseRule(string useRule)
    {
        ReadOnlySpan<string> stringRules = useRule.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        UseRule[] rules = new UseRule[stringRules.Length];
        
        bool shouldSkip = useRule.Last() == 's';
        for(int i = 0; i < stringRules.Length; i++)
        {
            if(stringRules[i].Contains('h'))
            {
                rules[i] = new UseRule(SkillRule.Health, stringRules[i].Contains('>'), int.Parse(stringRules[i].RemoveLetters()), shouldSkip);
                continue;
            }
            
            if (stringRules[i].Contains('m'))
            {
                rules[i] = new UseRule(SkillRule.Mana, stringRules[i].Contains('>'), int.Parse(stringRules[i].RemoveLetters()), shouldSkip);
                continue;
            }
            
            if (stringRules[i].Contains('w'))
            {
                rules[i] = new UseRule(SkillRule.Wait, true, int.Parse(stringRules[i].RemoveLetters()), shouldSkip);
                continue;
            }
            
            // Parse aura rules: a:auraname>5 or ta:auraname<3 or a:auraname=2
            if (stringRules[i].StartsWith("ta:", StringComparison.OrdinalIgnoreCase))
            {
                var auraRule = ParseAuraRule(stringRules[i][3..], SkillRule.TargetAura, shouldSkip);
                if (auraRule.HasValue)
                {
                    rules[i] = auraRule.Value;
                    continue;
                }
            }
            else if (stringRules[i].StartsWith("a:", StringComparison.OrdinalIgnoreCase))
            {
                var auraRule = ParseAuraRule(stringRules[i][2..], SkillRule.Aura, shouldSkip);
                if (auraRule.HasValue)
                {
                    rules[i] = auraRule.Value;
                    continue;
                }
            }
        }
        
        return rules;
    }
    
    private UseRule? ParseAuraRule(string rule, SkillRule skillRule, bool shouldSkip)
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
            string auraName = rule.Substring(0, splitIndex);
            string valueStr = rule.Substring(splitIndex + 1);
            if (int.TryParse(valueStr, out int value))
            {
                return new UseRule(skillRule, auraName, comparison, value, shouldSkip);
            }
        }
        
        return null;
    }

    public void Save(string file)
    {
    }

    public void OnTargetReset()
    {
        if (ResetOnTarget && !_player.HasTarget)
            Root.Reset();
    }
    public bool? ShouldUseSkill(int skillIndex, bool canUse)
    {
        return Root.ShouldUse(_player, skillIndex, canUse, _self, _target);
    }

    public void Stop()
    {
        _combat.CancelAutoAttack();
        _combat.CancelTarget();
        Root.Reset();
    }
}

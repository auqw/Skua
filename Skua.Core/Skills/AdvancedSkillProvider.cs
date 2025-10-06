using CommunityToolkit.Mvvm.Messaging;
using Skua.Core.Interfaces;
using Skua.Core.Interfaces.Auras;
using Skua.Core.Messaging;
using Skua.Core.Utils;

namespace Skua.Core.Skills;

public class AdvancedSkillProvider : ISkillProvider
{
    private readonly IScriptPlayer _player;
    private readonly IScriptCombat _combat;
    private readonly IScriptSelfAuras? _auras;
    private readonly AdvancedSkillCommand _currentCommand;
    private readonly UseRule[] _none = new[] { new UseRule(SkillRule.None) };

    public AdvancedSkillProvider(IScriptPlayer player, IScriptCombat combat, IScriptSelfAuras? auras = null)
    {
        _player = player;
        _combat = combat;
        _auras = auras;
        _currentCommand = new(auras);
    }

    public bool ResetOnTarget { get; set; } = false;

    public (int, int) GetNextSkill()
    {
        return _currentCommand.GetNextSkill();
    }

    public void Load(string skills)
    {
        int index = 0;
        foreach (string command in skills.ToLower().Split('|').Select(s => s.Trim()).ToList())
        {
            if (int.TryParse(command.AsSpan(0, 1), out int skill))
            {
                _currentCommand.Skills.Add(index, skill);
                _currentCommand.UseRules.Add(command.Length <= 1 ? _none : ParseUseRule(command[1..]));
                ++index;
            }
        }
    }

    private UseRule[] ParseUseRule(string useRule)
    {
        ReadOnlySpan<string> stringRules = useRule.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
        UseRule[] rules = new UseRule[stringRules.Length];

        bool shouldSkip = useRule.Last() == 's';
        for (int i = 0; i < stringRules.Length; i++)
        {
            if (stringRules[i].Contains('h'))
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

            if (stringRules[i].Contains('a') && stringRules[i].Contains(':'))
            {
                string auraName = stringRules[i][(stringRules[i].IndexOf(':') + 1)..];
                rules[i] = new UseRule(SkillRule.Aura, auraName, shouldSkip);
                continue;
            }
        }

        return rules;
    }

    public void Save(string file)
    {
    }

    public void OnTargetReset()
    {
        if (ResetOnTarget && !_player.HasTarget)
            _currentCommand.Reset();
    }

    public bool? ShouldUseSkill(int skillIndex, bool canUse)
    {
        return _currentCommand.ShouldUse(_player, skillIndex, canUse);
    }

    public void Stop()
    {
        _combat.CancelAutoAttack();
        _combat.CancelTarget();
        _currentCommand.Reset();
    }

    public void OnPlayerDeath()
    {
        _currentCommand.Reset();
    }
}

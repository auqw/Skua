using Skua.Core.Interfaces;
using Skua.Core.Models.Players;

namespace Skua.Core.Skills;

public class AdvancedSkillCommand
{
    private readonly IFlashUtil? _flash;

    public AdvancedSkillCommand()
    { }

    public AdvancedSkillCommand(IFlashUtil flash)
    {
        _flash = flash;
    }

    public Dictionary<int, int> Skills { get; set; } = new();
    public List<UseRule[]> UseRules { get; set; } = new();

    /// <summary>Per step: <see langword="true"/> when the step is used if any of its conditions holds (OR), not only all of them.</summary>
    public List<bool> MatchAny { get; set; } = new();

    private int _index = 0;

    public int SkillCount => Skills.Count;

    public (int, int) GetNextSkill()
    {
        if (Skills.Count == 0)
            return (-1, -1);

        int skill = Skills[_index];
        int index = _index;
        ++_index;
        if (_index >= Skills.Count)
            _index = 0;
        return (index, skill);
    }

    public bool? ShouldUse(IScriptPlayer player, IScriptSelfAuras self, IScriptTargetAuras target, int skillIndex, bool canUse)
    {
        if (skillIndex < 0 || skillIndex >= UseRules.Count)
            return true;

        // "Skip if not available": a step whose skill cannot be used now is passed over at once, rather than waited
        // for (Wait for Cooldown). The flag is the same on every rule of the step.
        if (!canUse && UseRules[skillIndex].Any(r => r.ShouldSkip))
            return null;

        if (UseRules[skillIndex].First().Rule == SkillRule.None)
            return true;

        bool matchAny = skillIndex < MatchAny.Count && MatchAny[skillIndex];
        bool shouldUse = true;
        bool anyCondition = false, anyHeld = false, skipIfNone = false;
        foreach (UseRule useRule in UseRules[skillIndex])
        {
            if (!player.Alive)
                return false;

            if (useRule.Rule == SkillRule.Wait)
            {
                Task.Delay((int)useRule.Value).Wait();
                continue;
            }

            if (useRule.Rule == SkillRule.None)
                continue;

            if (matchAny)
            {
                // OR: one condition that holds is enough; the rest need not be read.
                anyCondition = true;
                skipIfNone |= useRule.ShouldSkip;
                if (!anyHeld)
                    anyHeld = Holds(useRule, player, self, target);
                continue;
            }

            shouldUse = Holds(useRule, player, self, target);

            if (useRule.ShouldSkip && !shouldUse)
                return null;

            if (!shouldUse)
                break;
        }

        if (matchAny)
            return !anyCondition || anyHeld ? true : skipIfNone ? null : false;

        return shouldUse;
    }

    private bool Holds(UseRule useRule, IScriptPlayer player, IScriptSelfAuras self, IScriptTargetAuras target)
    {
        switch (useRule.Rule)
        {
            case SkillRule.Health:
                return HealthUseRule(player, useRule.Greater, (int)useRule.Value, useRule.IsPercentage);

            case SkillRule.Mana:
                return ManaUseRule(player, useRule.Greater, (int)useRule.Value, useRule.IsPercentage);

            case SkillRule.Aura:
                List<AuraCheck> singleCheck = new()
                {
                    new()
                    {
                        AuraName = useRule.AuraName,
                        AuraTarget = useRule.AuraTarget,
                        Greater = useRule.Greater,
                        StackCount = useRule.Value
                    }
                };
                return AuraUseRule(player, self, target, singleCheck);

            case SkillRule.MultiAura:
                return MultiAuraUseRule(player, self, target, useRule.MultiAuraChecks, useRule.MultiAuraOperator);

            case SkillRule.PartyHealth:
                return PartyHealthUseRule(player, useRule.Greater, (int)useRule.Value, useRule.IsPercentage);

            default:
                return true;
        }
    }

    private bool HealthUseRule(IScriptPlayer player, bool greater, int health, bool isPercentage = true)
    {
        if (player.Health == 0)
            return false;

        if (isPercentage)
        {
            if (player.MaxHealth == 0)
                return false;
            int ratio = (int)(player.Health / (double)player.MaxHealth * 100.0);
            return greater ? ratio >= health : ratio <= health;
        }
        else
        {
            return greater ? player.Health >= health : player.Health <= health;
        }
    }

    private bool ManaUseRule(IScriptPlayer player, bool greater, int mana, bool isPercentage = true)
    {
        if (isPercentage)
        {
            if (player.MaxMana == 0)
                return false;
            int ratio = (int)(player.Mana / (double)player.MaxMana * 100.0);
            return greater ? ratio >= mana : ratio <= mana;
        }
        else
        {
            return greater ? player.Mana >= mana : player.Mana <= mana;
        }
    }

    private bool PartyHealthUseRule(IScriptPlayer player, bool greater, int health, bool isPercentage = true)
    {
        if (_flash == null)
            return false;
        try
        {
            // world.uoTree holds every player in the map by name, this one included (the game has no world.players).
            Dictionary<string, PlayerInfo>? players = _flash.GetGameObject<Dictionary<string, PlayerInfo>>("world.uoTree");
            if (players == null || players.Count == 0)
                return false;

            string cell = player.Cell;
            foreach (PlayerInfo? targetPlayer in players.Values)
            {
                if (targetPlayer is null || string.IsNullOrEmpty(targetPlayer.Cell) || !targetPlayer.Cell.Equals(cell, StringComparison.OrdinalIgnoreCase))
                    continue;

                int targetHealth = targetPlayer.HP;
                int targetMaxHealth = targetPlayer.MaxHP;

                if (targetHealth == 0 || (isPercentage && targetMaxHealth == 0))
                    continue;

                if (isPercentage)
                {
                    int ratio = (int)(targetHealth / (double)targetMaxHealth * 100.0);
                    if (greater ? ratio >= health : ratio <= health)
                        return true;
                }
                else
                {
                    if (greater ? targetHealth >= health : targetHealth <= health)
                        return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private bool AuraUseRule(IScriptPlayer player, IScriptSelfAuras self, IScriptTargetAuras target, List<AuraCheck> checks)
    {
        if (checks == null || checks.Count == 0)
            return true;

        foreach (AuraCheck check in checks)
        {
            float stacks = GetAuraStacks(player, self, target, check.AuraTarget, check.AuraName);
            if (!(check.Greater ? stacks >= check.StackCount : stacks <= check.StackCount))
                return false;
        }
        return true;
    }

    private bool MultiAuraUseRule(IScriptPlayer player, IScriptSelfAuras self, IScriptTargetAuras target, List<AuraCheck> checks, MultiAuraOperator op)
    {
        if (checks == null || checks.Count == 0)
            return true;

        if (op == MultiAuraOperator.Or)
        {
            foreach (AuraCheck check in checks)
            {
                float stacks = GetAuraStacks(player, self, target, check.AuraTarget, check.AuraName);
                if (check.Greater ? stacks >= check.StackCount : stacks <= check.StackCount)
                    return true;
            }
            return false;
        }
        else
        {
            foreach (AuraCheck check in checks)
            {
                float stacks = GetAuraStacks(player, self, target, check.AuraTarget, check.AuraName);
                if (!(check.Greater ? stacks >= check.StackCount : stacks <= check.StackCount))
                    return false;
            }
            return true;
        }
    }

    private float GetAuraStacks(IScriptPlayer player, IScriptSelfAuras self, IScriptTargetAuras target, string auraTarget, string auraName)
    {
        // The HUD stack count or the summed aura values, whichever is higher: rules were written against the summed
        // values (Lich's Ex Mortis has value 2 but no HUD stack count, so the HUD shows 1), and auras the server
        // stacks with a stack count read as the HUD shows them.
        if (auraTarget.Equals("self", StringComparison.OrdinalIgnoreCase))
            return self.GetAuraRuleStacks(auraName);

        if (auraTarget.Equals("target", StringComparison.OrdinalIgnoreCase) && player.HasTarget)
            return target.GetAuraRuleStacks(auraName);

        return 0;
    }

    public void Reset()
    {
        _index = 0;
    }
}

public enum SkillRule
{
    None,
    Health,
    Mana,
    Aura,
    MultiAura,
    PartyHealth,
    Wait
}

public struct AuraCheck
{
    public string AuraName { get; set; }
    public float StackCount { get; set; }
    public bool Greater { get; set; }
    public string AuraTarget { get; set; }

    public AuraCheck()
    {
        AuraName = "";
        StackCount = 0;
        Greater = true;
        AuraTarget = "self";
    }
}

public enum MultiAuraOperator
{
    And,
    Or
}

public struct UseRule
{
    public SkillRule Rule { get; set; }
    public bool Greater { get; set; }
    public float Value { get; set; }
    public bool ShouldSkip { get; set; }
    public string AuraTarget { get; set; }
    public string AuraName { get; set; }
    public int PartyMemberIndex { get; set; }
    public bool IsPercentage { get; set; }
    public List<AuraCheck> MultiAuraChecks { get; set; }
    public MultiAuraOperator MultiAuraOperator { get; set; }
    public bool IsMultiAura { get; set; }

    public UseRule()
    {
        Rule = SkillRule.None;
        Greater = default;
        Value = default;
        ShouldSkip = default;
        AuraTarget = "self";
        AuraName = "";
        PartyMemberIndex = -1;
        IsPercentage = true;
        MultiAuraChecks = new();
        MultiAuraOperator = MultiAuraOperator.And;
        IsMultiAura = false;
    }

    public UseRule(SkillRule rule)
    {
        Rule = rule;
        Greater = default;
        Value = default;
        ShouldSkip = default;
        AuraTarget = "self";
        AuraName = "";
        PartyMemberIndex = -1;
        IsPercentage = true;
        MultiAuraChecks = new();
        MultiAuraOperator = MultiAuraOperator.And;
        IsMultiAura = false;
    }

    public UseRule(SkillRule rule, bool greater, int value, bool shouldSkip, bool isPercentage = true)
    {
        Rule = rule;
        Greater = greater;
        Value = value;
        ShouldSkip = shouldSkip;
        AuraTarget = "self";
        AuraName = "";
        PartyMemberIndex = -1;
        IsPercentage = isPercentage;
        MultiAuraChecks = new();
        MultiAuraOperator = MultiAuraOperator.And;
        IsMultiAura = false;
    }

    public UseRule(SkillRule rule, bool greater, int value, bool shouldSkip, string auraTarget, string auraName = "", int partyMemberIndex = -1, bool isPercentage = true)
    {
        Rule = rule;
        Greater = greater;
        Value = value;
        ShouldSkip = shouldSkip;
        AuraTarget = auraTarget;
        AuraName = auraName;
        PartyMemberIndex = partyMemberIndex;
        IsPercentage = isPercentage;
        MultiAuraChecks = new();
        MultiAuraOperator = MultiAuraOperator.And;
        IsMultiAura = false;
    }

    public UseRule(SkillRule rule, bool shouldSkip, string auraName, string auraTarget, bool greater, int stackCount, bool isPercentage = false)
    {
        Rule = rule;
        Greater = greater;
        Value = stackCount;
        ShouldSkip = shouldSkip;
        AuraTarget = auraTarget;
        AuraName = auraName;
        PartyMemberIndex = -1;
        IsPercentage = isPercentage;
        MultiAuraChecks = new();
        MultiAuraOperator = MultiAuraOperator.And;
        IsMultiAura = false;
    }

    public UseRule(SkillRule rule, bool shouldSkip, string auraName, string auraTarget, bool greater, float stackCount, bool isPercentage = false)
    {
        Rule = rule;
        Greater = greater;
        Value = stackCount;
        ShouldSkip = shouldSkip;
        AuraTarget = auraTarget;
        AuraName = auraName;
        PartyMemberIndex = -1;
        IsPercentage = isPercentage;
        MultiAuraChecks = new();
        MultiAuraOperator = MultiAuraOperator.And;
        IsMultiAura = false;
    }

    public UseRule(SkillRule rule, bool shouldSkip, List<AuraCheck> auraChecks, MultiAuraOperator op = MultiAuraOperator.And)
    {
        Rule = auraChecks.Count > 1 ? SkillRule.MultiAura : rule;
        Greater = default;
        Value = default;
        ShouldSkip = shouldSkip;
        AuraTarget = "self";
        AuraName = "";
        PartyMemberIndex = -1;
        IsPercentage = false;
        MultiAuraChecks = auraChecks;
        MultiAuraOperator = op;
        IsMultiAura = auraChecks.Count > 1;
    }
}

using Skua.Core.Interfaces;

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

        if (UseRules.Count == 0 || UseRules[skillIndex].First().Rule == SkillRule.None)
            return true;

        bool shouldUse = true;
        foreach (UseRule useRule in UseRules[skillIndex])
        {
            if (!player.Alive)
                return false;

            switch (useRule.Rule)
            {
                case SkillRule.Health:
                    shouldUse = HealthUseRule(player, useRule.Greater, (int)useRule.Value, useRule.IsPercentage);
                    break;

                case SkillRule.Mana:
                    shouldUse = ManaUseRule(player, useRule.Greater, (int)useRule.Value, useRule.IsPercentage);
                    break;

                case SkillRule.Aura:
                    List<AuraCheck> singleCheck = new()
                    {
                            new() {
                                AuraName = useRule.AuraName,
                                AuraTarget = useRule.AuraTarget,
                                Greater = useRule.Greater,
                                StackCount = useRule.Value
                            }
                        };
                    shouldUse = AuraUseRule(player, self, target, singleCheck);
                    break;

                case SkillRule.MultiAura:
                    shouldUse = MultiAuraUseRule(player, self, target, useRule.MultiAuraChecks, useRule.MultiAuraOperator);
                    break;

                case SkillRule.PartyHealth:
                    shouldUse = PartyHealthUseRule(player, useRule.Greater, (int)useRule.Value, useRule.IsPercentage);
                    break;

                case SkillRule.Wait:
                    if (useRule.ShouldSkip && !canUse)
                        return null;
                    Task.Delay((int)useRule.Value).Wait();
                    break;

                case SkillRule.None:
                    break;
            }

            if (useRule.ShouldSkip && !shouldUse)
                return null;

            if (!shouldUse)
                break;
        }
        return shouldUse;
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
            dynamic[]? players = _flash.GetGameObject<dynamic[]>("world.players");
            if (players == null || players.Length == 0)
                return false;

            foreach (dynamic targetPlayer in players)
            {
                string? targetCell = targetPlayer.strFrame;
                if (string.IsNullOrEmpty(targetCell) || targetCell != player.Cell)
                    continue;

                int targetHealth = targetPlayer.dataLeaf.intHP;
                int targetMaxHealth = targetPlayer.dataLeaf.intHPMax;

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
        if (auraTarget.Equals("self", StringComparison.OrdinalIgnoreCase))
            return Stacks(self.GetAuraStacks(auraName), self.Auras, auraName);

        if (auraTarget.Equals("target", StringComparison.OrdinalIgnoreCase) && player.HasTarget)
            return Stacks(target.GetAuraStacks(auraName), target.Auras, auraName);

        return 0;
    }

    /// <summary>
    /// The HUD stack count or the summed aura values, whichever is higher. Rules were written against the summed values
    /// (Lich's Ex Mortis has value 2 but no HUD stack count, so the HUD shows 1), and auras the server stacks with a
    /// stack count read as the HUD shows them.
    /// </summary>
    private static float Stacks(int hudStacks, List<Models.Auras.Aura>? auras, string auraName)
    {
        float values = auras?
            .Where(a => a.Name != null && a.Name.Equals(auraName, StringComparison.OrdinalIgnoreCase))
            .Sum(a => a.Value) ?? 0;
        return Math.Max(hudStacks, values);
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

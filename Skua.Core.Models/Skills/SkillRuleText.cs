namespace Skua.Core.Models.Skills;

/// <summary>
/// Helpers for the rule text of one Advanced Skills step, such as <c>H&lt;60% OR M&gt;50%</c>.
/// </summary>
public static class SkillRuleText
{
    /// <summary>
    /// Whether a step's rules say OR: the word "or" on its own, in any case, outside quoted aura names. A step with OR
    /// is used when any of its conditions holds; without it, only when all of them hold.
    /// </summary>
    /// <param name="rules">The step's rules, without the skill number.</param>
    public static bool HasOr(string rules)
    {
        bool quoted = false;
        for (int i = 0; i < rules.Length; i++)
        {
            char c = rules[i];
            if (quoted && c == '\\' && i + 1 < rules.Length && rules[i + 1] == '"')
            {
                i++;
                continue;
            }

            if (c == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (quoted || c is not ('o' or 'O') || i + 1 >= rules.Length || rules[i + 1] is not ('r' or 'R'))
                continue;

            bool startsWord = i == 0 || char.IsWhiteSpace(rules[i - 1]);
            bool endsWord = i + 2 == rules.Length || char.IsWhiteSpace(rules[i + 2]);
            if (startsWord && endsWord)
                return true;
        }

        return false;
    }
}

namespace Skua.Core.Models.Skills;

public class AdvancedSkill
{
    public AdvancedSkill()
    { }

    public AdvancedSkill(string className, string skills, int skillTimeout = -1, string classUseMode = "Base", string skillUseMode = "UseIfAvailable", bool resetComboOnTargetChange = false)
    {
        ClassName = className;
        Skills = skills;
        SkillTimeout = skillTimeout;
        ClassUseMode = (ClassUseMode)Enum.Parse(typeof(ClassUseMode), classUseMode, ignoreCase: true);
        SkillUseMode = (SkillUseMode)Enum.Parse(typeof(SkillUseMode), skillUseMode, ignoreCase: true);
        ResetComboOnTargetChange = resetComboOnTargetChange;
    }

    public AdvancedSkill(string className, string skills, int skillTimeout = -1, int classUseMode = 0, SkillUseMode skillUseMode = SkillUseMode.UseIfAvailable, bool resetComboOnTargetChange = false)
    {
        ClassName = className;
        Skills = skills;
        SkillTimeout = skillTimeout;
        ClassUseMode = (ClassUseMode)classUseMode;
        SkillUseMode = skillUseMode;
        ResetComboOnTargetChange = resetComboOnTargetChange;
    }

    public AdvancedSkill(string className, string skills, int skillTimeout, ClassUseMode classUseMode, SkillUseMode skillUseMode, bool resetComboOnTargetChange = false)
    {
        ClassName = className;
        Skills = skills;
        SkillTimeout = skillTimeout;
        ClassUseMode = classUseMode;
        SkillUseMode = skillUseMode;
        ResetComboOnTargetChange = resetComboOnTargetChange;
    }

    public AdvancedSkill(string className, string skills, int skillTimeout, string classUseMode, SkillUseMode skillUseMode, bool resetComboOnTargetChange = false)
    {
        ClassName = className;
        Skills = skills;
        SkillTimeout = skillTimeout;
        ClassUseMode = (ClassUseMode)Enum.Parse(typeof(ClassUseMode), classUseMode, ignoreCase: true);
        SkillUseMode = skillUseMode;
        ResetComboOnTargetChange = resetComboOnTargetChange;
    }

    public string ClassName { get; set; } = "Generic";
    public string Skills { get; set; } = "0 | 1 | 2 | 3 | 4";
    /// <summary>
    /// How long Wait for Cooldown waits for a skill's cooldown before skipping it, in milliseconds. 0 or less: as long as it takes.
    /// </summary>
    public int SkillTimeoutMs { get; set; } = 25000;

    /// <summary>
    /// The same limit in the old unit, ticks of <see cref="TickMs"/> milliseconds, as older files, scripts and constructors give it.
    /// </summary>
    public int SkillTimeout
    {
        // Rounded up, in long arithmetic: a huge value saturates at int.MaxValue instead of wrapping to "no limit".
        get => SkillTimeoutMs <= 0 ? SkillTimeoutMs : (int)(((long)SkillTimeoutMs + TickMs - 1) / TickMs);
        set => SkillTimeoutMs = value <= 0 ? value : (int)Math.Min(int.MaxValue, (long)value * TickMs);
    }

    /// <summary>The length of one timeout tick in files and older scripts: the skill timer's default interval.</summary>
    public const int TickMs = 100;
    public ClassUseMode ClassUseMode { get; set; } = ClassUseMode.Base;
    public SkillUseMode SkillUseMode { get; set; } = SkillUseMode.UseIfAvailable;
    public bool ResetComboOnTargetChange { get; set; } = false;
    public string SaveString => $"{ClassUseMode} = {ClassName} = {Skills} = {(SkillUseMode == SkillUseMode.UseIfAvailable ? "Use if Available" : SkillTimeout)}";

    public override string ToString()
    {
        return $"{ClassUseMode} : {ClassName} = {Skills} [{(SkillUseMode == SkillUseMode.UseIfAvailable ? "Use if Available" : "Wait for Cooldown")}]{(ResetComboOnTargetChange ? " [Reset on Target Change]" : string.Empty)}";
    }

    public override bool Equals(object? obj)
    {
        return obj is AdvancedSkill skill && skill.ClassName == ClassName && skill.ClassUseMode == ClassUseMode;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClassName, ClassUseMode);
    }
}
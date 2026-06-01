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
    public int SkillTimeout { get; set; } = 250;
    public ClassUseMode ClassUseMode { get; set; } = ClassUseMode.Base;
    public SkillUseMode SkillUseMode { get; set; } = SkillUseMode.UseIfAvailable;
    public bool ResetComboOnTargetChange { get; set; } = false;
    public string SaveString => $"{ClassUseMode} = {ClassName} = {Skills} = {(SkillUseMode == SkillUseMode.UseIfAvailable ? "Use if Available" : SkillTimeout)}";

    public override string ToString()
    {
        return $"{ClassUseMode} : {ClassName} = {Skills} [{(SkillUseMode == SkillUseMode.UseIfAvailable ? "Use if Available" : "Wait for Cooldown")}]";
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
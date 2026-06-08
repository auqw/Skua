
using System.Text.Json.Serialization;

namespace Skua.Core.Models.Skills;

public class AdvancedSkillJson
{
    [JsonPropertyName("skillId")]
    public int SkillId { get; set; }

    [JsonPropertyName("rules")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<SkillRuleJson>? Rules { get; set; }

    [JsonPropertyName("skipOnMatch")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SkipOnMatch { get; set; }

    [JsonPropertyName("isMultiAura")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool MultiAura { get; set; }

    [JsonPropertyName("multiAuraOperator")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MultiAuraOperator { get; set; }
}

public class SkillRuleJson
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "None";

    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public float? Value { get; set; }

    [JsonPropertyName("comparison")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Comparison { get; set; }

    [JsonPropertyName("auraName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AuraName { get; set; }

    [JsonPropertyName("auraTarget")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AuraTarget { get; set; }

    [JsonPropertyName("timeout")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Timeout { get; set; }

    [JsonPropertyName("isPercentage")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsPercentage { get; set; }
}

public class SkillModeJson
{
    [JsonPropertyName("skillUseMode")]
    public string SkillUseMode { get; set; } = "UseIfAvailable";

    [JsonPropertyName("skillTimeout")]
    public int SkillTimeout { get; set; } = 100;

    [JsonPropertyName("resetComboOnTargetChange")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ResetComboOnTargetChange { get; set; } = false;

    [JsonPropertyName("skills")]
    public List<AdvancedSkillJson> Skills { get; set; } = new();
}

public class AdvancedSkillsConfigJson : Dictionary<string, Dictionary<string, SkillModeJson>>
{
}

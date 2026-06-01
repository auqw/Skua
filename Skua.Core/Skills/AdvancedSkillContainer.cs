using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.Models.Skills;
using System.Text.Json;

namespace Skua.Core.Skills;

public class AdvancedSkillContainer : ObservableRecipient, IAdvancedSkillContainer, IDisposable
{
    private List<AdvancedSkill> _loadedSkills = new();
    private readonly string _defaultSkillsSetsPath;
    private readonly string _userSkillsSetsPath;
    private CancellationTokenSource? _saveCts;
    private Task? _saveTask;
    private AdvancedSkillsConfigJson? _jsonConfig;
    private string? _loadedFilePath;

    public List<AdvancedSkill> LoadedSkills
    {
        get => _loadedSkills;
        set => SetProperty(ref _loadedSkills, value, true);
    }

    public AdvancedSkillContainer()
    {
        _defaultSkillsSetsPath = ClientFileSources.SkuaAdvancedSkillsFile;
        _userSkillsSetsPath = Path.Combine(ClientFileSources.SkuaDIR, "UserAdvancedSkills.json");

        string rootDefaultSkills = Path.Combine(AppContext.BaseDirectory, "AdvancedSkills.json");
        if (File.Exists(rootDefaultSkills) && !File.Exists(_defaultSkillsSetsPath))
        {
            File.Copy(rootDefaultSkills, _defaultSkillsSetsPath, true);
        }
        LoadSkills();
    }

    public void Add(AdvancedSkill skill)
    {
        _loadedSkills.Add(skill);
        Save();
    }

    public void Remove(AdvancedSkill skill)
    {
        _loadedSkills.Remove(skill);
        Save();
    }

    public void TryOverride(AdvancedSkill skill)
    {
        if (!_loadedSkills.Contains(skill))
        {
            _loadedSkills.Add(skill);
            Save();
            return;
        }

        int index = _loadedSkills.IndexOf(skill);
        _loadedSkills[index] = skill;
        Save();
    }

    private void _CopyDefaultSkills()
    {
        IGetScriptsService getScripts = Ioc.Default.GetRequiredService<IGetScriptsService>();
        if (!File.Exists(_defaultSkillsSetsPath))
            getScripts.UpdateSkillSetsFile().GetAwaiter().GetResult();

        if (File.Exists(_userSkillsSetsPath))
            File.Delete(_userSkillsSetsPath);

        File.Copy(_defaultSkillsSetsPath, _userSkillsSetsPath);
    }

    public async void SyncSkills()
    {
        try
        {
            _saveCts?.Cancel();
            await (_saveTask ?? Task.CompletedTask);
            _saveCts?.Dispose();
            _saveCts = new CancellationTokenSource();

            await Task.Factory.StartNew(() =>
            {
                _CopyDefaultSkills();
                LoadSkills();
            }, _saveCts.Token);
        }
        catch {/* ignored */}
    }

    public void LoadSkills()
    {
        LoadedSkills.Clear();
        _jsonConfig = null;

        _loadedFilePath = _userSkillsSetsPath;

        if (File.Exists(_userSkillsSetsPath))
        {
            string fileContent = File.ReadAllText(_userSkillsSetsPath);
            LoadFromJson(fileContent);
        }
        else
        {
            _CopyDefaultSkills();
            if (File.Exists(_userSkillsSetsPath))
            {
                string fileContent = File.ReadAllText(_userSkillsSetsPath);
                LoadFromJson(fileContent);
            }
        }

        OnPropertyChanged(nameof(LoadedSkills));
        Broadcast(new(), _loadedSkills, nameof(LoadedSkills));
    }

    private void LoadFromJson(string jsonContent)
    {
        try
        {
            _jsonConfig = JsonSerializer.Deserialize<AdvancedSkillsConfigJson>(jsonContent);
            if (_jsonConfig == null)
                return;

            foreach (KeyValuePair<string, Dictionary<string, SkillModeJson>> classEntry in _jsonConfig)
            {
                string className = classEntry.Key;
                foreach (KeyValuePair<string, SkillModeJson> modeEntry in classEntry.Value)
                {
                    string classUseMode = modeEntry.Key;
                    SkillModeJson skillMode = modeEntry.Value;

                    string skillsStr = ConvertSkillsToString(skillMode.Skills);

                    _loadedSkills.Add(new AdvancedSkill(
                        className,
                        skillsStr,
                        skillMode.SkillTimeout,
                        classUseMode,
                        skillMode.SkillUseMode == "UseIfAvailable" ? SkillUseMode.UseIfAvailable : SkillUseMode.WaitForCooldown,
                        skillMode.ResetComboOnTargetChange
                    ));
                }
            }
        }
        catch { /* ignored */ }
    }

    private string ConvertSkillsToString(List<AdvancedSkillJson> skills)
    {
        List<string> parts = new();
        foreach (AdvancedSkillJson skill in skills)
        {
            string skillStr = skill.SkillId.ToString();
            if (skill.Rules?.Count > 0)
            {
                skillStr += " " + ConvertRulesToString(skill.Rules, skill.MultiAura, skill.MultiAuraOperator);
            }
            if (skill.SkipOnMatch)
            {
                skillStr += "S";
            }
            parts.Add(skillStr);
        }
        return string.Join(" | ", parts);
    }

    private string ConvertRulesToString(List<SkillRuleJson> rules, bool isMultiAura, string? multiAuraOperator)
    {
        List<string> ruleParts = new();
        List<SkillRuleJson> multiAuraRules = new();
        List<SkillRuleJson> singleAuraRules = new();
        List<SkillRuleJson> otherRules = new();

        foreach (SkillRuleJson rule in rules)
        {
            if (isMultiAura && rule.Type == "MultiAura")
                multiAuraRules.Add(rule);
            else if (!isMultiAura && rule.Type == "Aura")
                singleAuraRules.Add(rule);
            else if (rule.Type is not "MultiAura" and not "Aura" and not "Skip")
                otherRules.Add(rule);
        }

        foreach (SkillRuleJson? rule in otherRules)
        {
            string percentIndicator = (rule.IsPercentage ?? true) ? "%" : "#";
            switch (rule.Type)
            {
                case "Health":
                    ruleParts.Add($"H{(rule.Comparison == "greater" ? ">" : "<")}{rule.Value}{percentIndicator}");
                    break;

                case "Mana":
                    ruleParts.Add($"M{(rule.Comparison == "greater" ? ">" : "<")}{rule.Value}{percentIndicator}");
                    break;

                case "PartyHealth":
                    ruleParts.Add($"PH{(rule.Comparison == "greater" ? ">" : "<")}{rule.Value}{percentIndicator}");
                    break;

                case "Wait":
                    ruleParts.Add($"WW{rule.Timeout}");
                    break;
            }
        }

        if (isMultiAura && multiAuraRules.Count > 0)
        {
            string opChar = "&";
            if (!string.IsNullOrEmpty(multiAuraOperator))
            {
                opChar = multiAuraOperator switch
                {
                    "OR" => ":",
                    _ => "&"
                };
            }

            for (int i = 0; i < multiAuraRules.Count; i++)
            {
                SkillRuleJson rule = multiAuraRules[i];
                string suffix = i < multiAuraRules.Count - 1 ? opChar : "";
                ruleParts.Add($"MA{(rule.Comparison == "greater" ? ">" : "<")}\"{rule.AuraName}\" {rule.Value}{(rule.AuraTarget == "target" ? " TARGET" : "")}{suffix}");
            }
        }
        else if (singleAuraRules.Count > 1)
        {
            for (int i = 0; i < singleAuraRules.Count; i++)
            {
                SkillRuleJson rule = singleAuraRules[i];
                string suffix = i < singleAuraRules.Count - 1 ? "&" : "";
                ruleParts.Add($"MA{(rule.Comparison == "greater" ? ">" : "<")}\"{rule.AuraName}\" {rule.Value}{(rule.AuraTarget == "target" ? " TARGET" : "")}{suffix}");
            }
        }
        else if (singleAuraRules.Count == 1)
        {
            SkillRuleJson rule = singleAuraRules[0];
            ruleParts.Add($"A{(rule.Comparison == "greater" ? ">" : "<")}\"{rule.AuraName}\" {rule.Value}{(rule.AuraTarget == "target" ? " TARGET" : "")}");
        }

        return string.Join(" ", ruleParts);
    }

    public Dictionary<string, List<string>> GetAvailableClassModes()
    {
        if (_jsonConfig == null)
        {
            string jsonPath = Path.ChangeExtension(_userSkillsSetsPath, ".json");
            if (File.Exists(jsonPath))
            {
                string fileContent = File.ReadAllText(jsonPath);
                _jsonConfig = JsonSerializer.Deserialize<AdvancedSkillsConfigJson>(fileContent);
            }
        }

        Dictionary<string, List<string>> result = new();
        if (_jsonConfig != null)
        {
            foreach (KeyValuePair<string, Dictionary<string, SkillModeJson>> classEntry in _jsonConfig)
            {
                result[classEntry.Key] = new List<string>(classEntry.Value.Keys);
            }
        }
        return result;
    }

    public AdvancedSkill? GetClassModeSkills(string className, string mode)
    {
        return _loadedSkills.FirstOrDefault(s => s.ClassName == className && s.ClassUseMode.ToString() == mode);
    }

    public void ResetSkillsSets()
    {
        SyncSkills();
    }

    public async void Save()
    {
        _saveCts?.Cancel();
        await (_saveTask ?? Task.CompletedTask);
        _saveCts?.Dispose();
        _saveCts = new CancellationTokenSource();

        _saveTask = Task.Factory.StartNew(() =>
        {
            try
            {
                string jsonPath = _loadedFilePath ?? Path.ChangeExtension(_userSkillsSetsPath, ".json");
                if (!jsonPath.EndsWith(".json"))
                    jsonPath = Path.ChangeExtension(jsonPath, ".json");
                SaveToJson(jsonPath);

                if (!_saveCts.Token.IsCancellationRequested)
                {
                    LoadSkills();
                }
            }
            catch
            {
            }
        }, _saveCts.Token);
    }

    private void SaveToJson(string filePath)
    {
        AdvancedSkillsConfigJson config = new();

        foreach (AdvancedSkill skill in _loadedSkills)
        {
            if (!config.ContainsKey(skill.ClassName))
                config[skill.ClassName] = new Dictionary<string, SkillModeJson>();

            SkillModeJson skillMode = new()
            {
                SkillUseMode = skill.SkillUseMode == SkillUseMode.UseIfAvailable ? "UseIfAvailable" : "WaitForCooldown",
                SkillTimeout = skill.SkillTimeout,
                ResetComboOnTargetChange = skill.ResetComboOnTargetChange,
                Skills = AdvancedSkillsParser.ParseSkillString(skill.Skills)
            };

            config[skill.ClassName][skill.ClassUseMode.ToString()] = skillMode;
        }

        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string json = JsonSerializer.Serialize(config, options);
        File.WriteAllText(filePath, json);
    }

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _saveCts?.Cancel();
            try
            {
                _saveTask?.Wait(1000);
            }
            catch { }
            _saveCts?.Dispose();
            _loadedSkills.Clear();
        }

        _disposed = true;
    }

    ~AdvancedSkillContainer()
    {
        Dispose(false);
    }
}
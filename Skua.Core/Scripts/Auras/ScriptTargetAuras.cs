using Newtonsoft.Json;
using Skua.Core.Interfaces;
using Skua.Core.Models.Auras;

namespace Skua.Core.Scripts;

public partial class ScriptTargetAuras : IScriptTargetAuras
{
    private readonly Lazy<IFlashUtil> _lazyFlash;
    private readonly Lazy<IScriptPlayer> _lazyPlayer;
    private IFlashUtil Flash => _lazyFlash.Value;
    private IScriptPlayer Player => _lazyPlayer.Value;

    public ScriptTargetAuras(Lazy<IFlashUtil> lazyFlash, Lazy<IScriptPlayer> lazyPlayer)
    {
        _lazyFlash = lazyFlash;
        _lazyPlayer = lazyPlayer;
    }

    public List<Aura> Auras
    {
        get
        {
            int mapId = Player.Target?.MapID ?? 0;
            string? auraData = Flash.Call("GetMonsterAuraByID", mapId);
            if (string.IsNullOrWhiteSpace(auraData))
                return new List<Aura>();

            try
            {
                return JsonConvert.DeserializeObject<List<Aura>>(auraData) ?? new List<Aura>();
            }
            catch (JsonException)
            {
                return new List<Aura>();
            }
        }
    }

    public List<AuraSnapshot> Snapshots
    {
        get
        {
            string? data = Flash.Call("GetAuraSnapshots", nameof(SubjectType.Target));
            try
            {
                return JsonConvert.DeserializeObject<List<AuraSnapshot>>(data ?? "[]") ?? new();
            }
            catch (JsonException)
            {
                return new();
            }
        }
    }

    public AuraSnapshot? GetAuraSnapshot(string auraName) =>
        Snapshots.FirstOrDefault(a => a.Name.Equals(auraName, StringComparison.OrdinalIgnoreCase));

    public int GetAuraStacks(string auraName) => GetAuraSnapshot(auraName)?.Stacks ?? 0;

    public float GetAuraRuleStacks(string auraName) =>
        float.TryParse(Flash.Call("GetAuraRuleStacks", nameof(SubjectType.Target), auraName), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float stacks) ? stacks : 0;

    public Aura? GetAura(string auraName)
    {
        return Auras.FirstOrDefault(a => a.Name.Equals(auraName, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasActiveAura(string auraName)
    {
        return GetAura(auraName) != null;
    }

    public string GetMonsterAura(string monsterName)
    {
        return Flash.Call("GetMonsterAuraByName", monsterName) ?? "[]";
    }

    public string GetMonsterAura(int monID)
    {
        return Flash.Call("GetMonsterAuraByID", monID) ?? "[]";
    }

    public float GetAuraValue(string auraName)
    {
        return Flash.Call<float>("GetAurasValue", nameof(SubjectType.Target), auraName);
    }

    public bool HasAnyActiveAura(params string[] auraNames)
    {
        return Flash.Call<bool>("HasAnyActiveAura", nameof(SubjectType.Target), string.Join(",", auraNames));
    }

    public bool TryGetAura(string auraName, out Aura? aura)
    {
        if (HasActiveAura(auraName))
        {
            aura = GetAura(auraName);
            return true;
        }
        aura = null;
        return false;
    }
}
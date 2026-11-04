using Newtonsoft.Json;
using Skua.Core.Interfaces;
using Skua.Core.Models.Auras;

namespace Skua.Core.Scripts;

public partial class ScriptSelfAuras : IScriptSelfAuras
{
    private readonly Lazy<IFlashUtil> _lazyFlash;
    private readonly Lazy<IScriptPlayer> _lazyPlayer;
    private IFlashUtil Flash => _lazyFlash.Value;
    private IScriptPlayer Player => _lazyPlayer.Value;

    public ScriptSelfAuras(Lazy<IFlashUtil> lazyFlash, Lazy<IScriptPlayer> lazyPlayer)
    {
        _lazyFlash = lazyFlash;
        _lazyPlayer = lazyPlayer;
    }

    public List<Aura> Auras
    {
        get
        {
            string? auraData = Flash.Call("GetPlayerAura", Player.Username.ToLower());
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
            string? data = Flash.Call("GetAuraSnapshots", nameof(SubjectType.Self));
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
        float.TryParse(Flash.Call("GetAuraRuleStacks", nameof(SubjectType.Self), auraName), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float stacks) ? stacks : 0;

    public Aura? GetAura(string auraName)
    {
        return Auras.FirstOrDefault(a => a.Name.Equals(auraName, StringComparison.OrdinalIgnoreCase));
    }

    public bool HasActiveAura(string auraName)
    {
        return GetAura(auraName) != null;
    }

    public string GetPlayerAura(string playerName)
    {
        return Flash.Call("GetPlayerAura", playerName) ?? "[]";
    }

    public float GetAuraValue(string auraName)
    {
        return Flash.Call<float>("GetAurasValue", nameof(SubjectType.Self), auraName);
    }

    public bool HasAnyActiveAura(params string[] auraNames)
    {
        return Flash.Call<bool>("HasAnyActiveAura", nameof(SubjectType.Self), string.Join(",", auraNames));
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
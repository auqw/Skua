using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.Services;

namespace Skua.App.Avalonia.Services;

public class SettingsService : ISettingsService
{
    private readonly UnifiedSettingsService _settings = new();

    public SettingsService()
    {
        _settings.Initialize(AppRole.Client);
    }

    public T? Get<T>(string key) => _settings.Get<T>(key);

    public T Get<T>(string key, T defaultValue) => _settings.Get(key, defaultValue);

    public void Set<T>(string key, T value) => _settings.Set(key, value);

    public void Initialize(AppRole role) => _settings.Initialize(role);

    public SharedSettings GetShared() => _settings.GetShared();

    public ClientSettings GetClient() => _settings.GetClient();

    public ManagerSettings GetManager() => _settings.GetManager();

    public void SetApplicationVersion() => _settings.SetApplicationVersion();
}

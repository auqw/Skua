using Skua.Core.Models;
using System.Collections.Specialized;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Skua.Core.Services;

public class UnifiedSettingsService
{
    private readonly string _clientSettingsFile;
    private readonly string _managerSettingsFile;
    private readonly object _lock = new();
    private SettingsRoot _root = SettingsRoot.CreateDefaults();
    private AppRole _currentRole;
    private bool _initialized = false;
    private readonly Mutex _fileMutex;

    public UnifiedSettingsService()
    {
        _clientSettingsFile = Path.Combine(ClientFileSources.SkuaDIR, "ClientSettings.json");
        _managerSettingsFile = Path.Combine(ClientFileSources.SkuaDIR, "ManagerSettings.json");

        Directory.CreateDirectory(ClientFileSources.SkuaDIR);

        _fileMutex = new Mutex(false, @"Global\Skua.Settings.IO");
    }

    public void Initialize(AppRole role)
    {
        lock (_lock)
        {
            if (_initialized)
                return;

            _currentRole = role;

            try
            {
                _fileMutex.WaitOne();

                if (!File.Exists(ClientFileSources.SkuaSettingsDIR))
                {
                    if (!MigrateOldSettings())
                    {
                        _root = SettingsRoot.CreateDefaults();
                    }
                }
                else
                {
                    LoadSettings();
                }

                EnsureRoleDefaults();
                _initialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing settings: {ex.Message}");
                _root = SettingsRoot.CreateDefaults();
                _initialized = true;
            }
            finally
            {
                _fileMutex.ReleaseMutex();
            }
        }
    }

    public T? Get<T>(string key)
    {
        lock (_lock)
        {
            try
            {
                PropertyInfo? clientProp = FindPropertyByJsonName(_root.Client.GetType(), key);
                if (clientProp?.GetValue(_root.Client) is object clientVal)
                    if (typeof(T).IsAssignableFrom(clientVal.GetType()))
                        return (T)clientVal;

                PropertyInfo? managerProp = FindPropertyByJsonName(_root.Manager.GetType(), key);
                if (managerProp?.GetValue(_root.Manager) is object managerVal)
                    if (typeof(T).IsAssignableFrom(managerVal.GetType()))
                        return (T)managerVal;

                PropertyInfo? sharedProp = FindPropertyByJsonName(_root.Shared.GetType(), key);
                if (sharedProp?.GetValue(_root.Shared) is object sharedVal)
                    if (typeof(T).IsAssignableFrom(sharedVal.GetType()))
                        return (T)sharedVal;
            }
            catch (Exception)
            {
            }

            return default;
        }
    }

    public T Get<T>(string key, T defaultValue)
    {
        T? value = Get<T>(key);
        return value is null || value.Equals(default(T)) ? defaultValue : value;
    }

    public void Set<T>(string key, T value)
    {
        lock (_lock)
        {
            try
            {
                PropertyInfo? sharedProp = FindPropertyByJsonName(_root.Shared.GetType(), key);
                if (sharedProp != null)
                {
                    sharedProp.SetValue(_root.Shared, value);
                }
                else if (_currentRole == AppRole.Client)
                {
                    PropertyInfo? clientProp = FindPropertyByJsonName(_root.Client.GetType(), key);
                    if (clientProp != null)
                        clientProp.SetValue(_root.Client, value);
                    else
                    {
                        // Allow client-side code to update shared manager-backed values
                        // (e.g., unified theme settings) when keys live under manager.
                        PropertyInfo? managerProp = FindPropertyByJsonName(_root.Manager.GetType(), key);
                        managerProp?.SetValue(_root.Manager, value);
                    }
                }
                else if (_currentRole == AppRole.Manager)
                {
                    PropertyInfo? managerProp = FindPropertyByJsonName(_root.Manager.GetType(), key);
                    if (managerProp != null)
                        managerProp.SetValue(_root.Manager, value);
                    else
                    {
                        PropertyInfo? clientProp = FindPropertyByJsonName(_root.Client.GetType(), key);
                        clientProp?.SetValue(_root.Client, value);
                    }
                }

                SaveSettings();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting value for key '{key}': {ex.Message}");
            }
        }
    }

    public SharedSettings GetShared()
    {
        return _root.Shared;
    }

    public ClientSettings GetClient()
    {
        return _root.Client;
    }

    public ManagerSettings GetManager()
    {
        return _root.Manager;
    }

    private bool MigrateOldSettings()
    {
        try
        {
            bool clientExists = File.Exists(_clientSettingsFile);
            bool managerExists = File.Exists(_managerSettingsFile);

            if (!clientExists && !managerExists)
                return false;

            SettingsRoot newRoot = SettingsRoot.CreateDefaults();

            if (clientExists && managerExists)
            {
                Dictionary<string, object> clientData = LoadOldFile(_clientSettingsFile);
                Dictionary<string, object> managerData = LoadOldFile(_managerSettingsFile);

                MergeClientSettings(newRoot, clientData);
                MergeManagerSettings(newRoot, managerData);
                MergeSharedSettings(newRoot, managerData);
            }
            else if (clientExists)
            {
                Dictionary<string, object> clientData = LoadOldFile(_clientSettingsFile);
                MergeClientSettings(newRoot, clientData);
                MergeSharedSettings(newRoot, clientData);
            }
            else
            {
                Dictionary<string, object> managerData = LoadOldFile(_managerSettingsFile);
                MergeManagerSettings(newRoot, managerData);
                MergeSharedSettings(newRoot, managerData);
            }

            _root = newRoot;
            SaveSettings();

            string backupFile = ClientFileSources.SkuaSettingsDIR + ".bak";
            if (File.Exists(ClientFileSources.SkuaSettingsDIR) && !File.Exists(backupFile))
            {
                try { File.Copy(ClientFileSources.SkuaSettingsDIR, backupFile, overwrite: false); } catch { }
            }

            if (clientExists)
            {
                try { File.Delete(_clientSettingsFile); } catch { }
            }
            if (managerExists)
            {
                try { File.Delete(_managerSettingsFile); } catch { }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration failed: {ex.Message}");
            return false;
        }
    }

    private Dictionary<string, object> LoadOldFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                JsonSerializerOptions options = GetJsonOptions();
                return JsonSerializer.Deserialize<Dictionary<string, object>>(json, options) ?? new();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading old settings file {filePath}: {ex.Message}");
        }

        return new();
    }

    private void MergeClientSettings(SettingsRoot newRoot, Dictionary<string, object> oldData)
    {
        if (oldData.TryGetValue("AnimationFrameRate", out object? val))
            if (int.TryParse(val?.ToString(), out int framerate))
                newRoot.Client.AnimationFrameRate = framerate;

        if (oldData.TryGetValue("CheckAdvanceSkillSetsUpdates", out val))
            if (bool.TryParse(val?.ToString(), out bool check))
                newRoot.Client.CheckAdvanceSkillSetsUpdates = check;

        if (oldData.TryGetValue("AutoUpdateAdvanceSkillSetsUpdates", out val))
            if (bool.TryParse(val?.ToString(), out bool autoUpdate))
                newRoot.Client.AutoUpdateAdvanceSkillSetsUpdates = autoUpdate;

        if (oldData.TryGetValue("AutoUpdateBotScripts", out val))
            if (bool.TryParse(val?.ToString(), out bool autoUpdateScripts))
                newRoot.Client.AutoUpdateBotScripts = autoUpdateScripts;

        if (oldData.TryGetValue("CheckJunkItemsUpdates", out val))
            if (bool.TryParse(val?.ToString(), out bool checkJunk))
                newRoot.Client.CheckJunkItemsUpdates = checkJunk;

        if (oldData.TryGetValue("AutoUpdateJunkItems", out val))
            if (bool.TryParse(val?.ToString(), out bool autoUpdateJunk))
                newRoot.Client.AutoUpdateJunkItems = autoUpdateJunk;

        if (oldData.TryGetValue("CheckBotScriptsUpdates", out val))
            if (bool.TryParse(val?.ToString(), out bool checkScripts))
                newRoot.Shared.CheckBotScriptsUpdates = checkScripts;

        if (oldData.TryGetValue("IgnoreGHAuth", out val))
            if (bool.TryParse(val?.ToString(), out bool ignoreAuth))
                newRoot.Client.IgnoreGHAuth = ignoreAuth;

        if (oldData.TryGetValue("UseLocalVSC", out val))
            if (bool.TryParse(val?.ToString(), out bool useLocal))
                newRoot.Client.UseLocalVSC = useLocal;

        if (oldData.TryGetValue("UserOptions", out val))
            newRoot.Client.UserOptions = ConvertToStringCollection(val);

        if (oldData.TryGetValue("FastTravels", out val))
            newRoot.Client.FastTravels = ConvertToStringCollection(val);

        if (oldData.TryGetValue("DefaultFastTravels", out val))
            newRoot.Client.DefaultFastTravels = ConvertToStringCollection(val);

        if (oldData.TryGetValue("HotKeys", out val))
            newRoot.Client.HotKeys = ConvertToStringCollection(val);

        if (oldData.TryGetValue("UpgradeRequired", out val))
            if (bool.TryParse(val?.ToString(), out bool upgrade))
                newRoot.Client.UpgradeRequired = upgrade;
    }

    private void MergeManagerSettings(SettingsRoot newRoot, Dictionary<string, object> oldData)
    {
        if (oldData.TryGetValue("CheckClientUpdates", out object? val))
            if (bool.TryParse(val?.ToString(), out bool check))
                newRoot.Manager.CheckClientUpdates = check;

        if (oldData.TryGetValue("CheckClientPrereleases", out val))
            if (bool.TryParse(val?.ToString(), out bool checkPre))
                newRoot.Manager.CheckClientPrereleases = checkPre;

        if (oldData.TryGetValue("ClientDownloadPath", out val))
            newRoot.Manager.ClientDownloadPath = val?.ToString() ?? string.Empty;

        if (oldData.TryGetValue("DeleteZipFileAfter", out val))
            if (bool.TryParse(val?.ToString(), out bool deleteZip))
                newRoot.Manager.DeleteZipFileAfter = deleteZip;

        if (oldData.TryGetValue("ChangeLogActivated", out val))
            if (bool.TryParse(val?.ToString(), out bool changeLog))
                newRoot.Manager.ChangeLogActivated = changeLog;

        if (oldData.TryGetValue("syncTheme", out val))
            if (bool.TryParse(val?.ToString(), out bool syncTheme))
                newRoot.Manager.SyncTheme = syncTheme;

        if (oldData.TryGetValue("ManagedAccounts", out val))
            newRoot.Manager.ManagedAccounts = ConvertToAccountDataDictionary(val);

        if (oldData.TryGetValue("LastServer", out val))
            newRoot.Manager.LastServer = val?.ToString() ?? string.Empty;
    }

    private void MergeSharedSettings(SettingsRoot newRoot, Dictionary<string, object> oldData)
    {
        if (oldData.TryGetValue("DefaultBackground", out object? val))
            newRoot.Shared.sBG = val?.ToString() ?? "Generic2.swf";
        else if (oldData.TryGetValue("sBG", out val))
            newRoot.Shared.sBG = val?.ToString() ?? "Generic2.swf";

        if (oldData.TryGetValue("UserThemes", out val))
            newRoot.Shared.UserThemes = ConvertToStringCollection(val);

        if (oldData.TryGetValue("DefaultThemes", out val))
            newRoot.Shared.DefaultThemes = ConvertToStringCollection(val);

        if (oldData.TryGetValue("CurrentTheme", out val))
            newRoot.Shared.CurrentTheme = val?.ToString() ?? "Skua,Dark,#FF607D8B,#FF607D8B,#FF000000,#FF000000,true,4.5,Medium,All";

        if (oldData.TryGetValue("UserGitHubToken", out val))
            newRoot.Shared.UserGitHubToken = val?.ToString() ?? string.Empty;

        if (oldData.TryGetValue("ApplicationVersion", out val))
            newRoot.Shared.ApplicationVersion = val?.ToString() ?? ClientFileSources.AssemblyVersion;

        if (oldData.TryGetValue("CheckBotScriptsUpdates", out val))
            if (bool.TryParse(val?.ToString(), out bool checkScripts))
                newRoot.Shared.CheckBotScriptsUpdates = checkScripts;

        if (oldData.TryGetValue("CustomBackgroundPath", out val))
            newRoot.Shared.CustomBackgroundPath = val?.ToString();
    }

    private System.Collections.Specialized.StringCollection ConvertToStringCollection(object? value)
    {
        StringCollection collection = new();

        if (value == null)
            return collection;

        if (value is StringCollection sc)
            return sc;

        if (value is System.Collections.Specialized.StringCollection syssc)
        {
            foreach (string item in syssc)
                collection.Add(item);
            return collection;
        }

        if (value is JsonElement je)
        {
            try
            {
                List<string>? list = JsonSerializer.Deserialize<List<string>>(je.GetRawText());
                if (list != null)
                {
                    foreach (string item in list)
                        collection.Add(item);
                }
            }
            catch { /* ignored */ }
            return collection;
        }

        if (value is IEnumerable<string> enumerable)
        {
            foreach (string item in enumerable)
                collection.Add(item);
            return collection;
        }

        return collection;
    }

    private const string _legacySeparator = "{=}";
    private readonly string[] _legacyArrSeparator = { _legacySeparator };

    private Dictionary<string, AccountData> ConvertToAccountDataDictionary(object? value)
    {
        Dictionary<string, AccountData> dictionary = new(StringComparer.OrdinalIgnoreCase);

        if (value == null)
            return dictionary;

        if (value is Dictionary<string, AccountData> existing)
            return existing;

        if (value is JsonElement je)
        {
            try
            {
                if (je.ValueKind == JsonValueKind.Array)
                {
                    List<string>? list = JsonSerializer.Deserialize<List<string>>(je.GetRawText());
                    if (list != null)
                    {
                        foreach (string item in list)
                        {
                            if (item.Contains(_legacySeparator))
                            {
                                string[] info = item.Split(_legacyArrSeparator, StringSplitOptions.None);
                                if (info.Length >= 3)
                                {
                                    string displayName = info[0];
                                    string username = info[1];
                                    string password = info[2];
                                    dictionary[username] = new AccountData
                                    {
                                        DisplayName = displayName,
                                        Password = password,
                                        Tags = new List<string>()
                                    };
                                }
                            }
                        }
                    }
                }
                else if (je.ValueKind == JsonValueKind.Object)
                {
                    JsonSerializerOptions options = GetJsonOptions();
                    Dictionary<string, AccountData>? dict = JsonSerializer.Deserialize<Dictionary<string, AccountData>>(je.GetRawText(), options);
                    if (dict != null)
                        return dict;
                }
            }
            catch { /* ignored */ }
            return dictionary;
        }

        if (value is StringCollection sc)
        {
            foreach (string item in sc)
            {
                if (item.Contains(_legacySeparator))
                {
                    string[] info = item.Split(_legacyArrSeparator, StringSplitOptions.None);
                    if (info.Length >= 3)
                    {
                        string displayName = info[0];
                        string username = info[1];
                        string password = info[2];
                        dictionary[username] = new AccountData
                        {
                            DisplayName = displayName,
                            Password = password,
                            Tags = new List<string>()
                        };
                    }
                }
            }
            return dictionary;
        }

        return dictionary;
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists(ClientFileSources.SkuaSettingsDIR))
            {
                string json = File.ReadAllText(ClientFileSources.SkuaSettingsDIR);
                JsonSerializerOptions options = GetJsonOptions();
                SettingsRoot? loaded = JsonSerializer.Deserialize<SettingsRoot>(json, options);

                if (loaded != null)
                {
                    _root = loaded;
                    if (_root.FormatVersion == 0)
                        _root.FormatVersion = 1;

                    _root.Shared ??= new SharedSettings();
                    _root.Client ??= new ClientSettings();
                    _root.Manager ??= new ManagerSettings();

                    _root.Shared.InitializeDefaults();
                    _root.Client.InitializeDefaults();
                    _root.Manager.InitializeDefaults();

                    if (CleanupDeprecatedKeys())
                        SaveSettings();
                }
                else
                {
                    _root = SettingsRoot.CreateDefaults();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}");
            BackupCorruptFile();
            _root = SettingsRoot.CreateDefaults();
        }
    }

    private void SaveSettings()
    {
        try
        {
            _fileMutex.WaitOne();

            string tempFile = ClientFileSources.SkuaSettingsDIR + ".tmp";

            JsonSerializerOptions options = GetJsonOptions();
            string json = JsonSerializer.Serialize(_root, options);
            File.WriteAllText(tempFile, json);

            if (File.Exists(ClientFileSources.SkuaSettingsDIR))
            {
                File.Delete(ClientFileSources.SkuaSettingsDIR);
            }

            File.Move(tempFile, ClientFileSources.SkuaSettingsDIR, overwrite: true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving settings: {ex.Message}");
        }
        finally
        {
            _fileMutex.ReleaseMutex();
        }
    }

    private void EnsureRoleDefaults()
    {
        if (_currentRole == AppRole.Client)
        {
            _root.Client.InitializeDefaults();
        }
        else if (_currentRole == AppRole.Manager)
        {
            _root.Manager.InitializeDefaults();
        }

        _root.Shared.InitializeDefaults();
    }

    private bool CleanupDeprecatedKeys()
    {
        bool changed = false;

        if (_root.Shared.ExtensionData != null)
        {
            if (_root.Shared.ExtensionData.ContainsKey("DefaultBackground"))
            {
                if (_root.Shared.ExtensionData.TryGetValue("DefaultBackground", out object? oldValue))
                {
                    string? oldBg = oldValue?.ToString();
                    if (!string.IsNullOrEmpty(oldBg) && string.IsNullOrEmpty(_root.Shared.sBG))
                        _root.Shared.sBG = oldBg;
                }
                _root.Shared.ExtensionData.Remove("DefaultBackground");
                changed = true;
            }
        }

        if (_root.Client.ExtensionData != null)
        {
            if (_root.Client.ExtensionData.ContainsKey("CustomBackgroundPath"))
            {
                if (_root.Client.ExtensionData.TryGetValue("CustomBackgroundPath", out object? oldCustomPath))
                {
                    string? customPath = oldCustomPath?.ToString();
                    if (!string.IsNullOrEmpty(customPath) && string.IsNullOrEmpty(_root.Shared.CustomBackgroundPath))
                        _root.Shared.CustomBackgroundPath = customPath;
                }
                _root.Client.ExtensionData.Remove("CustomBackgroundPath");
                changed = true;
            }
        }

        return changed;
    }

    private void BackupCorruptFile()
    {
        try
        {
            if (File.Exists(ClientFileSources.SkuaSettingsDIR))
            {
                string backupPath = ClientFileSources.SkuaSettingsDIR + ".corrupt";
                File.Copy(ClientFileSources.SkuaSettingsDIR, backupPath, overwrite: true);
                File.Delete(ClientFileSources.SkuaSettingsDIR);
            }
        }
        catch { }
    }

    private JsonSerializerOptions GetJsonOptions()
    {
        JsonSerializerOptions options = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = null,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        options.Converters.Add(new StringCollectionJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());

        return options;
    }

    private System.Reflection.PropertyInfo? FindPropertyByJsonName(Type type, string jsonName)
    {
        PropertyInfo[] properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        foreach (PropertyInfo prop in properties)
        {
            if (prop.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonPropertyNameAttribute), false)
                .FirstOrDefault() is System.Text.Json.Serialization.JsonPropertyNameAttribute jsonAttr && jsonAttr.Name == jsonName)
                return prop;
        }
        return null;
    }

    public void SetApplicationVersion()
    {
        lock (_lock)
        {
            _root.Shared.ApplicationVersion = ClientFileSources.AssemblyVersion;
            SaveSettings();
        }
    }

    public void Dispose()
    {
        _fileMutex?.Dispose();
    }
}

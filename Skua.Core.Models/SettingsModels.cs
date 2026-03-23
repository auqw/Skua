using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Skua.Core.Models;

public class AccountData
{
    [JsonPropertyName("DisplayName")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("Password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("Tags")]
    public List<string> Tags { get; set; } = new();
}

public class GroupData
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Accounts")]
    public List<string> Accounts { get; set; } = new();
}

public class AccountDataDictionaryJsonConverter : JsonConverter<Dictionary<string, AccountData>>
{
    private const string LegacySeparator = "{=}";
    private static readonly string[] LegacyArrSeparator = { LegacySeparator };

    public override Dictionary<string, AccountData> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Dictionary<string, AccountData> dictionary = new(StringComparer.OrdinalIgnoreCase);

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string? key = reader.GetString();
                    reader.Read();
                    if (key != null)
                    {
                        AccountData? accountData = JsonSerializer.Deserialize<AccountData>(ref reader, options);
                        if (accountData != null)
                            dictionary[key] = accountData;
                    }
                }
            }
        }
        else if (reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    string? item = reader.GetString();
                    if (item != null && item.Contains(LegacySeparator))
                    {
                        string[] info = item.Split(LegacyArrSeparator, StringSplitOptions.None);
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

        return dictionary;
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, AccountData> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (KeyValuePair<string, AccountData> kvp in value)
        {
            writer.WritePropertyName(kvp.Key);
            JsonSerializer.Serialize(writer, kvp.Value, options);
        }
        writer.WriteEndObject();
    }
}

public enum AppRole
{
    Client,
    Manager
}

public class StringCollectionJsonConverter : JsonConverter<System.Collections.Specialized.StringCollection>
{
    public override System.Collections.Specialized.StringCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        StringCollection collection = new();
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType == JsonTokenType.String)
                {
                    string? value = reader.GetString();
                    if (value != null)
                        collection.Add(value);
                }
            }
        }
        return collection;
    }

    public override void Write(Utf8JsonWriter writer, System.Collections.Specialized.StringCollection value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (string item in value)
        {
            writer.WriteStringValue(item);
        }
        writer.WriteEndArray();
    }
}

public class SharedSettings
{
    [JsonPropertyName("sBG")]
    public string sBG { get; set; } = "Generic2.swf";

    [JsonPropertyName("UserThemes")]
    public StringCollection UserThemes { get; set; } = new();

    [JsonPropertyName("DefaultThemes")]
    public StringCollection DefaultThemes { get; set; } = new();

    [JsonPropertyName("CurrentTheme")]
    public string CurrentTheme { get; set; } = "Skua,Dark,#FF607D8B,#FF607D8B,#FF000000,#FF000000,true,4.5,Medium,All";

    [JsonPropertyName("UserGitHubToken")]
    public string UserGitHubToken { get; set; } = string.Empty;

    [JsonPropertyName("ApplicationVersion")]
    public string ApplicationVersion { get; set; } = ClientFileSources.AssemblyVersion;

    [JsonPropertyName("CustomBackgroundPath")]
    public string? CustomBackgroundPath { get; set; } = null;

    [JsonPropertyName("UpgradeRequired")]
    public bool UpgradeRequired { get; set; } = true;

    [JsonPropertyName("CheckBotScriptsUpdates")]
    public bool CheckBotScriptsUpdates { get; set; } = true;

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    public void InitializeDefaults()
    {
        if (string.IsNullOrEmpty(sBG))
            sBG = "Generic2.swf";

        if (UserThemes == null || UserThemes.Count == 0)
        {
            UserThemes = new();
        }

        if (DefaultThemes == null || DefaultThemes.Count == 0)
        {
            DefaultThemes = new()
            {
                "Skua,Dark,#FF607D8B,#FF607D8B,#FF000000,#FF000000,true,4.5,Medium,All",
                "RBot,Light,#FF9C934E,#FF9C934E,#FF000000,#FF000000",
                "Grimoire,Dark,#FFCC1F41,#FFCC1F41,#FFFFFFFF,#FFFFFFFF",
                "Purple,Dark,#FF9651D6,#FF9651D6,#FFFFFFFF,#FFFFFFFF,true,4.5,Medium,All",
                "Phonk,Dark,#FFFE27D7,#FF607D8B,#FF000000,#FF000000,true,4.5,Medium,All"
            };
        }

        if (string.IsNullOrEmpty(CurrentTheme))
            CurrentTheme = "Skua,Dark,#FF607D8B,#FF607D8B,#FF000000,#FF000000,true,4.5,Medium,All";

        if (string.IsNullOrEmpty(ApplicationVersion))
            ApplicationVersion = ClientFileSources.AssemblyVersion;
    }
}

public class ClientSettings
{
    [JsonPropertyName("AnimationFrameRate")]
    public int AnimationFrameRate { get; set; } = 30;

    [JsonPropertyName("CheckAdvanceSkillSetsUpdates")]
    public bool CheckAdvanceSkillSetsUpdates { get; set; } = true;

    [JsonPropertyName("AutoUpdateAdvanceSkillSetsUpdates")]
    public bool AutoUpdateAdvanceSkillSetsUpdates { get; set; } = true;

    [JsonPropertyName("AutoUpdateBotScripts")]
    public bool AutoUpdateBotScripts { get; set; } = true;

    [JsonPropertyName("CheckJunkItemsUpdates")]
    public bool CheckJunkItemsUpdates { get; set; } = true;

    [JsonPropertyName("AutoUpdateJunkItems")]
    public bool AutoUpdateJunkItems { get; set; } = true;

    [JsonPropertyName("IgnoreGHAuth")]
    public bool IgnoreGHAuth { get; set; } = false;

    [JsonPropertyName("UseLocalVSC")]
    public bool UseLocalVSC { get; set; } = true;

    [JsonPropertyName("UserOptions")]
    public StringCollection UserOptions { get; set; } = new();

    [JsonPropertyName("FastTravels")]
    public StringCollection FastTravels { get; set; } = new();

    [JsonPropertyName("DefaultFastTravels")]
    public StringCollection DefaultFastTravels { get; set; } = new();

    [JsonPropertyName("HotKeys")]
    public StringCollection HotKeys { get; set; } = new();

    [JsonPropertyName("UpgradeRequired")]
    public bool UpgradeRequired { get; set; } = true;

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    public void InitializeDefaults()
    {
        UserOptions ??= new();

        if (FastTravels == null || FastTravels.Count == 0)
        {
            FastTravels = new()
            {
                "Tercessuinotlim,tercessuinotlim,Enter,Spawn",
                "Nulgath,tercessuinotlim,Boss2,Right",
                "VHL & Taro,tercessuinotlim,Taro,Left"
            };
        }

        if (DefaultFastTravels == null || DefaultFastTravels.Count == 0)
        {
            DefaultFastTravels = new()
            {
                "Tercessuinotlim,tercessuinotlim,Enter,Spawn",
                "Nulgath,tercessuinotlim,Boss2,Right",
                "VHL & Taro,tercessuinotlim,Taro,Left"
            };
        }

        if (HotKeys == null || HotKeys.Count == 0)
        {
            HotKeys = new()
            {
                "ToggleScript|F10",
                "LoadScript|F9",
                "OpenBank|F2",
                "OpenConsole|F3",
                "ToggleAutoAttack|F4",
                "ToggleAutoHunt|F5",
                "ToggleLagKiller|F6"
            };
        }
    }
}

public class ManagerSettings
{
    [JsonPropertyName("CheckClientUpdates")]
    public bool CheckClientUpdates { get; set; } = true;

    [JsonPropertyName("CheckClientPrereleases")]
    public bool CheckClientPrereleases { get; set; } = false;

    [JsonPropertyName("ClientDownloadPath")]
    public string ClientDownloadPath { get; set; } = string.Empty;

    [JsonPropertyName("DeleteZipFileAfter")]
    public bool DeleteZipFileAfter { get; set; } = false;

    [JsonPropertyName("ChangeLogActivated")]
    public bool ChangeLogActivated { get; set; } = false;

    [JsonPropertyName("syncTheme")]
    public bool SyncTheme { get; set; } = true;

    [JsonPropertyName("ManagedAccounts")]
    [JsonConverter(typeof(AccountDataDictionaryJsonConverter))]
    public Dictionary<string, AccountData> ManagedAccounts { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    [JsonPropertyName("AccountGroups")]
    public List<GroupData>? AccountGroups { get; set; } = new();

    [JsonPropertyName("LastServer")]
    public string LastServer { get; set; } = string.Empty;

    [JsonPropertyName("AnonymiseAccounts")]
    public bool AnonymiseAccounts { get; set; } = false;

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    public void InitializeDefaults()
    {
        if (string.IsNullOrEmpty(ClientDownloadPath))
            ClientDownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Skua");

        ManagedAccounts ??= new(StringComparer.OrdinalIgnoreCase);
    }
}

public class SettingsRoot
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; } = 1;

    [JsonPropertyName("shared")]
    public SharedSettings Shared { get; set; } = new();

    [JsonPropertyName("client")]
    public ClientSettings Client { get; set; } = new();

    [JsonPropertyName("manager")]
    public ManagerSettings Manager { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

    public static SettingsRoot CreateDefaults()
    {
        SettingsRoot root = new()
        {
            FormatVersion = 1,
            Shared = new SharedSettings(),
            Client = new ClientSettings(),
            Manager = new ManagerSettings()
        };

        root.Shared.InitializeDefaults();
        root.Client.InitializeDefaults();
        root.Manager.InitializeDefaults();

        return root;
    }
}

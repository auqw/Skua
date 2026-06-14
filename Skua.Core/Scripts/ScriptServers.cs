using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Skua.Core.Flash;
using Skua.Core.Interfaces;
using Skua.Core.Models.Servers;
using Skua.Core.Utils;

namespace Skua.Core.Scripts;

public partial class ScriptServers : ObservableRecipient, IScriptServers
{
    public ScriptServers(
        Lazy<IFlashUtil> flash,
        Lazy<IScriptPlayer> player,
        Lazy<IScriptWait> wait,
        Lazy<IScriptOption> options,
        Lazy<IScriptBotStats> stats,
        Lazy<IScriptManager> manager,
        Lazy<ILogService> logger)
    {
        _lazyFlash = flash;
        _lazyPlayer = player;
        _lazyWait = wait;
        _lazyOptions = options;
        _lazyStats = stats;
        _lazyManager = manager;
        _lazyLogger = logger;
    }

    private readonly Lazy<IFlashUtil> _lazyFlash;
    private readonly Lazy<IScriptPlayer> _lazyPlayer;
    private readonly Lazy<IScriptWait> _lazyWait;
    private readonly Lazy<IScriptOption> _lazyOptions;
    private readonly Lazy<IScriptBotStats> _lazyStats;
    private readonly Lazy<IScriptManager> _lazyManager;
    private readonly Lazy<ILogService> _lazyLogger;

    private IFlashUtil Flash => _lazyFlash.Value;
    private IScriptPlayer Player => _lazyPlayer.Value;
    private IScriptWait Wait => _lazyWait.Value;
    private IScriptOption Options => _lazyOptions.Value;
    private IScriptBotStats Stats => _lazyStats.Value;
    private IScriptManager Manager => _lazyManager.Value;
    private ILogService Logger => _lazyLogger.Value;

    private bool _loginInfoSetted = false;
    private string _username;
    private string _password;

    public string LastIP { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Flash Objects Binding

    [ObjectBinding("serialCmd.servers")]
    private List<Server> _serverList = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    private List<Server> _cachedServers = new();

    // Flash Methods Binding

    [MethodCallBinding("login", GameFunction = true)]
    private void _login(string username, string password)
    { }

    [MethodCallBinding("logout", RunMethodPost = true, GameFunction = true)]
    private void _logout()
    {
        if (IsConnected)
            Flash.CallGameFunction("sfc.disconnect");
        Flash.CallGameFunction("gotoAndPlay", "Login");
    }

    [MethodCallBinding("connectTo", RunMethodPost = true, GameFunction = true)]
    private bool _connectIP(string ip)
    {
        Wait.ForTrue(() => !Manager.ShouldExit && Player.Playing && Flash.IsWorldLoaded, 30);
        return Player.Playing;
    }

    [MethodCallBinding("connectTo", RunMethodPost = true, GameFunction = true)]
    private bool _connectIP(string ip, int port)
    {
        Wait.ForTrue(() => !Manager.ShouldExit && Player.Playing && Flash.IsWorldLoaded, 30);
        return Player.Playing;
    }

    [MethodCallBinding("connectToServer", RunMethodPost = true)]
    private bool _connectToServer(string server)
    {
        Wait.ForTrue(() => !Manager.ShouldExit && Player.Playing && Flash.IsWorldLoaded, 30);
        return Player.Playing;
    }

    [ObjectBinding("sfc.isConnected", RequireNotNull = "sfc", Default = "false")]
    private bool _isConnected;

    public void Login()
    {
        Login(Player.Username, Player.Password);
    }

    public async ValueTask<List<Server>> GetServers(bool forceUpdate = false)
    {
        if (CachedServers.Count > 0 && !forceUpdate)
            return CachedServers;

        try
        {
            string response = await ValidatedHttpExtensions.GetStringAsync(HttpClients.GetGHClient()
, $"http://content.aq.com/game/api/data/servers")
                .ConfigureAwait(false);

            List<Server>? servers = JsonConvert.DeserializeObject<List<Server>>(response);
            if (servers == null || servers.Count == 0)
                return new();

            CachedServers = servers;
            return CachedServers;
        }
        catch
        {
            return new();
        }
    }

    public void SetLoginInfo(string username, string password)
    {
        _username = username;
        _password = password;
        _loginInfoSetted = true;
    }

    public bool Reconnect(string serverName, int loginDelay = 2000)
    {
        Login(Player.Username, Player.Password);
        Thread.Sleep(loginDelay);
        return ((IScriptServers)this).Connect(serverName);
    }

    public bool Reconnect(Server server, int loginDelay = 2000)
    {
        Login(Player.Username, Player.Password);
        Thread.Sleep(loginDelay);
        return ((IScriptServers)this).Connect(server);
    }

    private void ReloginLog(string message)
    {
        Logger.ScriptLog($"[Relogin] {message}");
    }
    private static bool IsValidReloginServerData(Server server)
    {
        return !string.IsNullOrWhiteSpace(server.Name) && !string.IsNullOrWhiteSpace(server.IP);
    }

    private static bool IsServerFull(Server server)
    {
        return server.MaxPlayers > 0 && server.PlayerCount >= server.MaxPlayers;
    }

    private bool IsNonMemberAccount()
    {
        int loginUpgradeDays = Flash.GetGameObjectStatic<int>("objLogin.iUpgDays", int.MinValue);
        if (loginUpgradeDays != int.MinValue)
            return loginUpgradeDays < 0;

        int worldUpgradeDays = Flash.GetGameObject<int>("world.myAvatar.objData.iUpgDays", int.MinValue);
        if (worldUpgradeDays != int.MinValue)
            return worldUpgradeDays < 0;

        return false;
    }

    private bool IsServerBlacklistedForRelogin(Server server)
    {
        if (!IsValidReloginServerData(server))
            return true;
        string serverName = server.Name ?? string.Empty;
        bool memberOnly = server.Upgrade && IsNonMemberAccount();
        return !server.Online
            || serverName.Contains("Test", StringComparison.OrdinalIgnoreCase)
            || IsServerFull(server)
            || memberOnly;
    }

    private string? GetBlacklistReason(Server server)
    {
        if (!IsValidReloginServerData(server))
            return "invalid";
        if (!server.Online)
            return "offline";
        if ((server.Name ?? string.Empty).Contains("Test", StringComparison.OrdinalIgnoreCase))
            return "test server";
        if (IsServerFull(server))
            return "full";
        if (server.Upgrade && IsNonMemberAccount())
            return "member-only";
        return null;
    }

    private List<Server> BuildReloginCandidates()
    {
        List<Server> candidates = new();
        AddReloginCandidates(candidates, CachedServers);
        AddReloginCandidates(candidates, ServerList);
        return candidates;
    }

    private void RefreshServersSync()
    {
        try
        {
            GetServers(true).AsTask().GetAwaiter().GetResult();
            ReloginLog($"Refreshed server list ({CachedServers.Count} servers from API).");
        }
        catch
        {
            ReloginLog("Failed to refresh server list; using existing cache.");
        }
    }

    private void AddReloginCandidates(List<Server> candidates, List<Server> source)
    {
        foreach (Server server in source)
        {
            if (IsServerBlacklistedForRelogin(server))
                continue;

            if (candidates.Exists(x => string.Equals(x.Name, server.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            candidates.Add(server);
        }
    }

    private static Server? FindByName(List<Server> candidates, string? serverName, bool exactMatch, HashSet<string>? attemptedServers = null)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            return null;

        if (exactMatch)
        {
            return candidates.Find(x =>
                string.Equals(x.Name, serverName, StringComparison.OrdinalIgnoreCase)
                && (attemptedServers is null || !attemptedServers.Contains(x.Name ?? string.Empty)));
        }

        return candidates.Find(x =>
            (x.Name ?? string.Empty).Contains(serverName, StringComparison.OrdinalIgnoreCase)
            && (attemptedServers is null || !attemptedServers.Contains(x.Name ?? string.Empty)));
    }

    private Server? SelectReloginServer(string? preferredName = null, HashSet<string>? attemptedServers = null)
    {
        List<Server> candidates = BuildReloginCandidates();
        if (candidates.Count == 0)
            return null;

        HashSet<string> triedServers = attemptedServers ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        Server? selectedServer = FindByName(candidates, preferredName, true, triedServers);
        if (selectedServer is not null)
            return selectedServer;

        if (Options.AutoReloginAny)
        {
            selectedServer = candidates.Find(x => x.IP != LastIP && !triedServers.Contains(x.Name ?? string.Empty));
            if (selectedServer is not null)
                return selectedServer;
        }

        selectedServer = FindByName(candidates, Options.ReloginServer, true, triedServers);
        if (selectedServer is not null)
            return selectedServer;

        selectedServer = candidates.Find(x => !triedServers.Contains(x.Name ?? string.Empty));
        if (selectedServer is not null)
            return selectedServer;

        if (attemptedServers is not null)
            attemptedServers.Clear();

        selectedServer = FindByName(candidates, preferredName, true);
        if (selectedServer is not null)
            return selectedServer;

        if (Options.AutoReloginAny)
        {
            selectedServer = candidates.Find(x => x.IP != LastIP);
            if (selectedServer is not null)
                return selectedServer;
        }

        selectedServer = FindByName(candidates, Options.ReloginServer, true);
        if (selectedServer is not null)
            return selectedServer;

        return candidates[0];
    }

    private Server? SelectReloginServerByName(string serverName, bool exactMatch, HashSet<string>? attemptedServers = null)
    {
        List<Server> candidates = BuildReloginCandidates();
        Server? selectedServer = FindByName(candidates, serverName, exactMatch, attemptedServers);
        if (selectedServer is not null)
            return selectedServer;

        return SelectReloginServer(serverName, attemptedServers);
    }

    public bool Relogin(Server? server = null)
    {
        RefreshServersSync();
        Server? reloginServer = server;
        if (reloginServer is not null)
        {
            string? blacklistReason = GetBlacklistReason(reloginServer);
            if (blacklistReason is not null)
            {
                ReloginLog($"Skipping requested server {reloginServer.Name} ({blacklistReason}).");
                reloginServer = null;
            }
        }

        if (reloginServer is null)
            reloginServer = SelectReloginServer();
        if (reloginServer is null)
        {
            ReloginLog("No eligible relogin server found.");
            return false;
        }

        return ReloginIP(reloginServer.IP);
    }

    public bool ReloginIP(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            ReloginLog("Cannot relogin: empty server IP.");
            return false;
        }
        bool autoRelogSwitch = Options.AutoRelogin;
        Options.AutoRelogin = false;
        ReloginLog($"Relogging using IP {ip}.");

        Thread.Sleep(2000);
        Logout();

        Stats.Relogins++;
        if (_loginInfoSetted)
            Login(_username, _password);
        else
            Login(Player.Username, Player.Password);

        Thread.Sleep(2000);
        ConnectIP(ip);

        Wait.ForTrue(() => Player.Playing && Flash.IsWorldLoaded, 30);
        Options.AutoRelogin = autoRelogSwitch;
        bool connected = Player.Playing;
        ReloginLog(connected ? "Relogin by IP successful." : "Relogin by IP failed.");
        return connected;
    }

    public bool Relogin(string serverName)
    {
        RefreshServersSync();
        Server? requestedServer = CachedServers.Find(x => (x.Name ?? string.Empty).Contains(serverName, StringComparison.OrdinalIgnoreCase))
            ?? ServerList.Find(x => (x.Name ?? string.Empty).Contains(serverName, StringComparison.OrdinalIgnoreCase));
        if (requestedServer is not null)
        {
            string? blacklistReason = GetBlacklistReason(requestedServer);
            if (blacklistReason is not null)
                ReloginLog($"Server {requestedServer.Name} is excluded ({blacklistReason}), selecting fallback.");
        }

        Server? server = SelectReloginServerByName(serverName, false) ?? SelectReloginServer();
        if (server is null)
        {
            ReloginLog("No eligible relogin server found.");
            return false;
        }
        bool autoRelogSwitch = Options.AutoRelogin;
        Options.AutoRelogin = false;
        ReloginLog($"Relogging on server {server.Name} [{server.PlayerCount}/{server.MaxPlayers}].");

        Logout();
        Stats.Relogins++;

        if (_loginInfoSetted)
            Login(_username, _password);
        else
            Login(Player.Username, Player.Password);
        ConnectToServer(JsonConvert.SerializeObject(server));

        Wait.ForTrue(() => Player.Playing && Flash.IsWorldLoaded, 30);
        Options.AutoRelogin = autoRelogSwitch;
        bool connected = Player.Playing;
        ReloginLog(connected ? $"Relogin successful on {server.Name}." : $"Relogin failed on {server.Name}.");
        return connected;
    }

    public bool EnsureRelogin(string serverName)
    {
        for (int tries = 0; tries < Options.ReloginTries && !Manager.ShouldExit && !Player.Playing; tries++)
        {
            if (Relogin(serverName))
                return true;

            if (tries + 1 < Options.ReloginTries && Options.ReloginTryDelay > 0)
                Task.Delay(Options.ReloginTryDelay).Wait();
        }

        return Player.Playing;
    }

    public async Task<bool> EnsureRelogin(CancellationToken token)
    {
        await GetServers(true);
        Server? server = SelectReloginServer();
        if (server is null)
        {
            ReloginLog("No eligible servers available for auto relogin.");
            return false;
        }

        ReloginLog($"Auto relogin started. Preferred server: {server.Name}.");

        return await EnsureLogin(server, token);
    }

    public async Task<bool> EnsureRelogin(string serverName, CancellationToken token)
    {
        await GetServers(true);
        Server? server = SelectReloginServerByName(serverName, true) ?? SelectReloginServer();
        if (server is null)
        {
            ReloginLog($"No eligible servers available for requested relogin target {serverName}.");
            return false;
        }

        ReloginLog($"Auto relogin started for target {serverName}. Preferred server: {server.Name}.");

        return await EnsureLogin(server, token);
    }

    private async Task WaitReloginDelay(CancellationToken token)
    {
        if (Options.ReloginTryDelay <= 0)
            return;

        try
        {
            await Task.Delay(Options.ReloginTryDelay, token);
        }
        catch { }
    }

    private async Task<bool> WaitForServerListReady(CancellationToken token)
    {
        using CancellationTokenSource waitServerList = new(Options.LoginTimeout);
        try
        {
            while (!token.IsCancellationRequested && !Manager.ShouldExit && !waitServerList.IsCancellationRequested)
            {
                bool loginVisible = Flash.GetGameObject<bool>("mcLogin.visible", false);
                bool serverListExists = !Flash.IsNull("mcLogin.sl.iList");
                int serverEntries = Flash.GetGameObject<int>("mcLogin.sl.iList.numChildren", 0);
                if (loginVisible && serverListExists && serverEntries > 0 && ServerList.Exists(IsValidReloginServerData))
                    return true;

                await Task.Delay(250, token);
            }
        }
        catch { }
        bool finalLoginVisible = Flash.GetGameObject<bool>("mcLogin.visible", false);
        bool finalServerListExists = !Flash.IsNull("mcLogin.sl.iList");
        int finalServerEntries = Flash.GetGameObject<int>("mcLogin.sl.iList.numChildren", 0);
        return finalLoginVisible && finalServerListExists && finalServerEntries > 0 && ServerList.Exists(IsValidReloginServerData);
        return ServerList.Exists(IsValidReloginServerData);
    }

    private async Task<bool> EnsureLogin(Server server, CancellationToken token)
    {
        int tries = 0;
        HashSet<string> attemptedServers = new(StringComparer.OrdinalIgnoreCase);
        string? preferredServerName = server.Name;
        try
        {
            while (!token.IsCancellationRequested && !Manager.ShouldExit && !Player.Playing && tries < Options.ReloginTries)
            {
                tries++;
                await GetServers(true);
                ReloginLog($"Attempt {tries}/{Options.ReloginTries}: refreshed server list ({CachedServers.Count} servers).");

                Server? targetServer = SelectReloginServer(preferredServerName, attemptedServers);
                if (targetServer is null)
                {
                    ReloginLog($"Attempt {tries}/{Options.ReloginTries}: no eligible servers available.");
                    if (tries < Options.ReloginTries)
                        await WaitReloginDelay(token);
                    continue;
                }
                attemptedServers.Add(targetServer.Name ?? string.Empty);

                ReloginLog($"Attempt {tries}/{Options.ReloginTries}: trying {targetServer.Name} [{targetServer.PlayerCount}/{targetServer.MaxPlayers}].");

                if (IsConnected || tries > 1)
                    Logout();

                Login();
                ReloginLog($"Attempt {tries}/{Options.ReloginTries}: waiting for server list screen.");
                if (!await WaitForServerListReady(token))
                {
                    ReloginLog($"Attempt {tries}/{Options.ReloginTries}: server list screen not ready.");
                    preferredServerName = null;
                    if (tries < Options.ReloginTries)
                        await WaitReloginDelay(token);
                    continue;
                }

                if (!Flash.Call<bool>("clickServer", targetServer.Name))
                {
                    ReloginLog($"Server {targetServer.Name} not present in login list, connecting via IP {targetServer.IP}:{targetServer.Port}.");
                    if (targetServer.Port > 0)
                        ConnectIP(targetServer.IP, targetServer.Port);
                    else
                        ConnectIP(targetServer.IP);
                }

                using CancellationTokenSource waitLogin = new(Options.LoginTimeout);
                try
                {
                    while ((!Player.Playing || !Flash.IsWorldLoaded) && !waitLogin.IsCancellationRequested && !token.IsCancellationRequested)
                        await Task.Delay(750, token);
                }
                catch { }

                if (Player.Playing && Flash.IsWorldLoaded)
                {
                    ReloginLog($"Connected to {targetServer.Name}.");
                    return true;
                }

                ReloginLog($"Attempt {tries}/{Options.ReloginTries} failed on {targetServer.Name}.");
                preferredServerName = null;

                if (tries < Options.ReloginTries)
                    await WaitReloginDelay(token);
            }
        }
        catch { }

        if (!Player.Playing)
            ReloginLog("Unable to reconnect within configured retries.");
        return Player.Playing;
    }
}
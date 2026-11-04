using Newtonsoft.Json;
using Skua.Core.Flash;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.Models.Items;
using Skua.Core.Models.Players;
using Skua.Core.Utils;
using System.Diagnostics;

namespace Skua.Core.Scripts;

public partial class ScriptMap : IScriptMap
{
    public ScriptMap(
        Lazy<IFlashUtil> flash,
        Lazy<IScriptPlayer> player,
        Lazy<IScriptOption> options,
        Lazy<IScriptSend> send,
        Lazy<IScriptWait> wait,
        Lazy<IScriptManager> manager,
        IDialogService dialogService)
    {
        _lazyFlash = flash;
        _lazyPlayer = player;
        _lazyOptions = options;
        _lazySend = send;
        _lazyWait = wait;
        _lazyManager = manager;
        _dialogService = dialogService;
        LoadSavedMapItems();
    }

    private Dictionary<string, List<MapItem>> _savedMapItems = new();

    private readonly Lazy<IFlashUtil> _lazyFlash;
    private readonly Lazy<IScriptPlayer> _lazyPlayer;
    private readonly Lazy<IScriptOption> _lazyOptions;
    private readonly Lazy<IScriptWait> _lazyWait;
    private readonly Lazy<IScriptManager> _lazyManager;
    private readonly IDialogService _dialogService;
    private readonly Lazy<IScriptSend> _lazySend;

    private IFlashUtil Flash => _lazyFlash.Value;
    private IScriptPlayer Player => _lazyPlayer.Value;
    private IScriptOption Options => _lazyOptions.Value;
    private IScriptWait Wait => _lazyWait.Value;
    private IScriptManager Manager => _lazyManager.Value;
    private IScriptSend Send => _lazySend.Value;

    public string LastMap { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileName => string.IsNullOrEmpty(FilePath) ? string.Empty : FilePath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).Last();
    public string FlaName => string.IsNullOrEmpty(FileName) ? string.Empty : Path.GetFileNameWithoutExtension(FileName).Replace("-", "_") + "_fla";

    public string FullName => Loaded ? Flash.GetGameObject("ui.mcInterface.areaList.title.t1.text")?.Split(' ').Last().Replace("\"", string.Empty) ?? string.Empty : string.Empty;

    [ObjectBinding("world.strMapName", RequireNotNull = "world", Default = "string.Empty")]
    private string _name = string.Empty;

    [ObjectBinding("world.mapLoadInProgress", Default = "false")]
    private bool _loading;

    [ObjectBinding("world.curRoom")]
    private int _roomID;

    [ObjectBinding("world.areaUsers.length")]
    private int _playerCount;

    [ObjectBinding("world.areaUsers", Default = "new()")]
    private List<string> _playerNames = new();

    [ObjectBinding("world.uoTree", Default = "new()")]
    private readonly Dictionary<string, PlayerInfo> _playersDictionary = new();

    public List<PlayerInfo> Players => _playersDictionary.Values.ToList();
    public List<PlayerInfo> CellPlayers => Players.FindAll(p => p.Cell == Player.Cell);

    public bool Loaded => !Loading
                          && Flash.IsNull("mcConnDetail.stage");

    [ObjectBinding("world.map.currentScene.labels", Select = "name", Default = "new()")]
    private List<string> _cells;

    [MethodCallBinding("jumpCorrectRoom", RunMethodPost = true)]
    private void _jump(string cell, string pad, bool autoCorrect = true, bool clientOnly = false)
    {
        Thread.Sleep(Options.ActionDelay);
        Wait.ForCellChange(cell);
    }

    public void Join(string map, string cell = "Enter", string pad = "Spawn", bool ignoreCheck = false, bool autoCorrect = true)
    {
        _Join(map, cell, pad, ignoreCheck, autoCorrect);
    }

    private void _Join(string map, string cell = "Enter", string pad = "Spawn", bool ignoreCheck = false, bool autoCorrect = true)
    {
        string mapName = map.Split('-')[0];
        LastMap = mapName;
        if (!Player.Playing || !Player.Loaded || (!ignoreCheck && Name == map))
            return;
        int i = 0;
        while (Name != mapName && !Manager.ShouldExit && ++i < Options.JoinMapTries)
        {
            if ((Options.PrivateRooms && !map.Contains('-')) || map.Contains("-1e9"))
                map = $"{mapName}-{(Options.PrivateNumber > 0 ? Options.PrivateNumber : 100000)}";
            Wait.ForActionCooldown(GameActions.Transfer);
            JoinPacket(map, cell, pad);
            if (!Wait.ForMapLoad(map, 20) && !Manager.ShouldExit)
                Jump(Player.Cell, Player.Pad, autoCorrect);
            else
                Jump(cell, pad, autoCorrect);
            Thread.Sleep(Options.ActionDelay);
        }
    }

    public void JoinPacket(string map, string cell = "Enter", string pad = "Spawn")
    {
        Send.Packet($"%xt%zm%cmd%{RoomID}%tfer%{Player.Username}%{map}%{cell}%{pad}%");
    }

    public PlayerInfo? GetPlayer(string username)
    {
        string lowerUsername = username.ToLower();

        if (_playersDictionary.TryGetValue(lowerUsername, out PlayerInfo? cachedPlayer))
            return cachedPlayer;

        for (int attempt = 0; attempt < 3; attempt++)
        {
            PlayerInfo? player = Flash.GetGameObject<PlayerInfo>($"world.uoTree[\"{lowerUsername}\"]");
            if (player != null)
                return player;

            if (attempt < 2)
                Thread.Sleep(50);
        }

        return null;
    }

    [MethodCallBinding("world.reloadCurrentMap", GameFunction = true)]
    private void _reload()
    { }

    [MethodCallBinding("world.getMapItem", RunMethodPre = true, GameFunction = true)]
    private void _getMapItem(int id)
    {
        Wait.ForActionCooldown(Skua.Core.Models.GameActions.GetMapItem);
        Thread.Sleep(Options.ActionDelay);
    }

    private Dictionary<string, List<MapItem>>? LoadSavedMapItems()
    {
        return !File.Exists(_savedCacheFilePath)
            ? null
            : (_savedMapItems = JsonConvert.DeserializeObject<Dictionary<string, List<MapItem>>>(File.ReadAllText(_savedCacheFilePath))!);
    }

    private readonly string _cachePath = Path.Combine(ClientFileSources.SkuaDIR, "cache");
    private readonly string _savedCacheFilePath = Path.Combine(ClientFileSources.SkuaDIR, "cache", "0SavedMaps.json");

    public List<MapItem>? FindMapItems(bool forceRefresh = false)
    {
        if (string.IsNullOrEmpty(FilePath))
            return null;

        if (!Directory.Exists(_cachePath))
            Directory.CreateDirectory(_cachePath);

        if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "FFDec")))
        {
            _dialogService.ShowMessageBox("FFDec folder not found.", "FFDec");
            return null;
        }

        // Return cached data if available and not forcing refresh
        if (!forceRefresh && _savedMapItems.ContainsKey(FileName))
            return _savedMapItems[FileName];

        List<string> files = new(Directory.GetFiles(_cachePath));
        Stopwatch sw = Stopwatch.StartNew();

        // Always decompile when forcing refresh, even if SWF exists
        return files.Count > 0 && files.Contains(Path.Combine(_cachePath, FileName))
            ? !DecompileSWF(FileName) ? null : ParseMapSWFData()
            : !DownloadMapSWF(FileName) ? null : !DecompileSWF(FileName) ? null : ParseMapSWFData();
        void SaveMapItemInfo(List<MapItem> info)
        {
            if (_savedMapItems.ContainsKey(FileName))
                _savedMapItems[FileName] = info;
            else
                _savedMapItems.Add(FileName, info);
            File.WriteAllText(_savedCacheFilePath, JsonConvert.SerializeObject(_savedMapItems, Formatting.Indented));
        }

        List<MapItem>? ParseMapSWFData()
        {
            sw.Restart();
            List<MapItem> items = new();
            List<string> MainTimelineText;

            try
            {
                string scriptsPath = $"{_cachePath}\\tmp\\scripts";
                string flaDirectory = Directory.Exists($"{Path.Combine(scriptsPath, FlaName)}") ? Path.Combine(scriptsPath, FlaName) : Path.Combine(scriptsPath, "town_fla");

                if (!Directory.Exists(flaDirectory))
                    return null;
                string mainTimelinePath = Path.Combine(flaDirectory, "MainTimeline.as");

                if (!File.Exists(mainTimelinePath))
                    return null;

                MainTimelineText = File.ReadAllLines(mainTimelinePath).ToList();
                string[] files = Directory.GetFiles($@"{_cachePath}\tmp\scripts", "*APOP*", SearchOption.TopDirectoryOnly);

                IEnumerable<Tuple<string, int>> mapItemLines = MainTimelineText.Select((l, i) => new Tuple<string, int>(l, i)).Where(l => l.Item1.Contains("mapItem", StringComparison.OrdinalIgnoreCase) || l.Item1.Contains("itemdrop", StringComparison.OrdinalIgnoreCase));
                foreach ((string mapItemLine, int index) in mapItemLines)
                {
                    string questID = string.Empty;
                    string mapItem = string.Empty;
                    switch (mapItemLine.Contains("getmapitem"))
                    {
                        case true:
                            questID = MainTimelineText.Skip(index - 5 < 0 ? 0 : index - 5).Take(10).FirstOrDefault(l => l.Contains("isquestinprogress"))!;

                            questID = questID.ToLower().Split("isquestinprogress")[1].Split(')')[0].RemoveLetters() ?? "";

                            mapItem = mapItemLine.RemoveLetters();
                            break;

                        case false:
                            questID = MainTimelineText.Skip(index - 5 < 0 ? 0 : index - 5).Take(10).FirstOrDefault(l => l.Contains("questnum") || (l.Contains("intquest") && !l.Contains("intquestval")))!;

                            questID = questID.Split('=')[1].RemoveLetters() ?? "";

                            mapItem = mapItemLine.Split('=')[1].RemoveLetters();
                            break;
                    }
                    if (!string.IsNullOrEmpty(questID))
                        AddMapItem(int.Parse(mapItem), int.Parse(questID), FilePath, LastMap);
                }

                List<string> linesToParse = new();
                foreach (string t in files)
                {
                    bool take = false;
                    foreach (string line in File.ReadLines(t).Reverse())
                    {
                        switch (take)
                        {
                            case false when !line.Contains("getmapitem"):
                                continue;
                            case true when line.Contains("isquestinprogress"):
                                linesToParse.Add(line.Trim().ToLower());
                                take = false;
                                continue;
                        }

                        if (line.Contains("getmapitem"))
                        {
                            linesToParse.Add(line.Trim().ToLower());
                            take = true;
                            continue;
                        }
                    }
                }

                if (linesToParse.Count > 0)
                {
                    foreach ((string mapItem, string questId) in linesToParse.PairUp())
                    {
                        if (string.IsNullOrEmpty(mapItem) || string.IsNullOrEmpty(questId))
                            continue;
                        AddMapItem(int.Parse(mapItem.RemoveLetters()), int.Parse(questId.Split("isquestinprogress")[1].Split(')')[0].RemoveLetters()), FilePath, LastMap);
                    }
                }
                Directory.Delete($@"{_cachePath}\tmp\", true);

                void AddMapItem(int mapItem, int questid, string mapFilePath, string mapName)
                {
                    if (!items.Contains(i => i.ID == mapItem))
                        items.Add(new MapItem(mapItem, questid, mapFilePath, mapName));
                }
            }
            catch (Exception ex)
            {
                switch (ex)
                {
                    case FileNotFoundException:
                    case DirectoryNotFoundException:
                        _dialogService.ShowMessageBox("Could not find one or more files to read.", "Get Map Item");
                        break;
                    case PathTooLongException:
                        _dialogService.ShowMessageBox($"The path for the file is too long.\r\n{_cachePath}\\tmp\\scripts\\*_fla\\MainTimeline.as", "Get Map Item");
                        break;
                    case UnauthorizedAccessException:
                        _dialogService.ShowMessageBox("The program don't have permission to access the file", "Get Map Item");
                        break;
                    default:
                        _dialogService.ShowMessageBox($"An error occurred.\r\nMessage: {ex.Message}\r\nStackTrace:{ex.StackTrace}", "Get Map Item");
                        break;
                }
            }
            if (items.Count > 0)
            {
                items = items.OrderBy(i => i.ID).ToList();
                SaveMapItemInfo(items);
            }
            Trace.WriteLine($"Parsing took {sw.Elapsed:s\\.ff}s");
            return items;
        }

        bool DownloadMapSWF(string fileName)
        {
            sw.Restart();
            Task.Run(async () =>
            {
                byte[] fileBytes = await HttpClients.GetAQContent.GetByteArrayAsync($"https://game.aq.com/game/gamefiles/maps/{FilePath}");
                await File.WriteAllBytesAsync(Path.Combine(_cachePath, fileName), fileBytes);
            }).Wait();
            Trace.WriteLine($"Download of \"{fileName}\" took {sw.Elapsed:s\\.ff}s");
            return File.Exists($"{_cachePath}\\{fileName}");
        }

        bool DecompileSWF(string fileName)
        {
            sw.Restart();
            fileName = fileName.EndsWith(".swf") ? fileName : fileName + ".swf";
            Process decompile = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    FileName = "cmd.exe",
                    WorkingDirectory = Path.Combine(AppContext.BaseDirectory, "FFDec"),
                    Arguments = $"/c ffdec.bat -export script \"{_cachePath}\\tmp\" \"{_cachePath}\\{fileName}\""
                }
            };
            decompile.Start();
            string error = decompile.StandardError.ReadToEnd();
            decompile.WaitForExit();
            if (!string.IsNullOrEmpty(error))
            {
                string errorMsg = $"Error while decompiling the SWF: {error}";
                Trace.WriteLine(errorMsg);
                _dialogService.ShowMessageBox(errorMsg, "SWF Decompile Error");
            }
            else
                Trace.WriteLine($"Decompilation of \"{fileName}\" took {sw.Elapsed:s\\.ff}s");
            return Directory.Exists($"{_cachePath}\\tmp");
        }
    }

    public void ClearMapItemsCache(string? specificMap = null)
    {
        if (specificMap is not null)
        {
            if (_savedMapItems.ContainsKey(specificMap))
            {
                _savedMapItems.Remove(specificMap);
                File.WriteAllText(_savedCacheFilePath, JsonConvert.SerializeObject(_savedMapItems, Formatting.Indented));
            }
        }
        else
        {
            _savedMapItems.Clear();
            if (File.Exists(_savedCacheFilePath))
                File.Delete(_savedCacheFilePath);
        }
    }
}

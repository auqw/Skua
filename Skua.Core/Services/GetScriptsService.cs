using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Skua.Core.Interfaces;
using Skua.Core.Models;
using Skua.Core.Models.GitHub;
using Skua.Core.Utils;
using System.Net.Sockets;

namespace Skua.Core.Services;

public partial class GetScriptsService : ObservableObject, IGetScriptsService
{
    private readonly IDialogService _dialogService;

    private const string _rawScriptsJsonUrl = "auqw/Scripts/refs/heads/Skua/scripts.json";
    private const string _skillsSetsRawUrl = "auqw/Scripts/refs/heads/Skua/Skills/AdvancedSkills.json";
    private const string _questDataRawUrl = "auqw/Scripts/refs/heads/Skua/QuestData.json";
    private const string _junkItemsRawUrl = "auqw/Scripts/refs/heads/Skua/JunkItems.json";

    private const string _repoOwner = "auqw";
    private const string _repoName = "Scripts";
    private const string _repoBranch = "Skua";

    [ObservableProperty]
    private RangedObservableCollection<ScriptInfo> _scripts = new();

    public GetScriptsService(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async ValueTask<List<ScriptInfo>> GetScriptsAsync(IProgress<string>? progress, CancellationToken token)
    {
        if (_scripts.Count > 0)
            return _scripts.ToList();

        await GetScripts(progress, false, token);
        return _scripts.ToList();
    }

    public Task RefreshScriptsAsync(IProgress<string>? progress, CancellationToken token)
        => GetScripts(progress, true, token);

    private async Task GetScripts(IProgress<string>? progress, bool refresh, CancellationToken token)
    {
        try
        {
            Scripts.Clear();

            progress?.Report("Fetching scripts...");
            List<ScriptInfo> scripts = await GetScriptsInfo(refresh, token);

            progress?.Report($"Found {scripts.Count} scripts.");

            _scripts.AddRange(scripts);

            progress?.Report($"Fetched {scripts.Count} scripts.");
            OnPropertyChanged(nameof(Scripts));
        }
        catch (TaskCanceledException)
        {
            progress?.Report("Task cancelled.");
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException)
        {
            _dialogService.ShowMessageBox(
                "Unable to connect to GitHub.\r\nCheck your connection and try again.",
                "Network Error");
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessageBox(
                $"Failed to retrieve scripts.\r\n{ex.Message}",
                "Search Scripts Error");
        }
    }

    private async Task<List<ScriptInfo>> GetScriptsInfo(bool refresh, CancellationToken token)
    {
        if (_scripts.Count != 0 && !refresh)
            return _scripts.ToList();

        using HttpResponseMessage response =
            await ValidatedHttpExtensions.GetAsync(HttpClients.GitHubRaw, _rawScriptsJsonUrl, token);

        string content = await response.Content.ReadAsStringAsync(token);
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidDataException("scripts.json is empty.");

        List<ScriptInfo>? scripts =
            JsonConvert.DeserializeObject<List<ScriptInfo>>(content);

        if (scripts is null || scripts.Count == 0)
            throw new InvalidDataException("scripts.json contains no valid scripts.");

        return scripts;
    }

    public async Task DownloadScriptAsync(ScriptInfo info)
    {
        string? directory = Path.GetDirectoryName(info.LocalFile);

        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using HttpResponseMessage response =
            await ValidatedHttpExtensions.GetAsync(HttpClients.GitHubRaw, info.DownloadUrl);

        byte[] scriptBytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(info.LocalFile, scriptBytes);
    }

    public async Task<int> DownloadAllWhereAsync(Func<ScriptInfo, bool> pred)
    {
        List<ScriptInfo> toUpdate = _scripts.Where(pred).ToList();

        await Parallel.ForEachAsync(toUpdate, async (script, _) =>
        {
            await DownloadScriptAsync(script);
        });

        if (toUpdate.Count > 0)
            ClearCachedScriptsDirectory();

        return toUpdate.Count;
    }

    private static void ClearCachedScriptsDirectory()
    {
        string path = Path.Combine(ClientFileSources.SkuaScriptsDIR, "Cached-Scripts");

        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        catch { }
    }

    public Task DeleteScriptAsync(ScriptInfo info)
    {
        try
        {
            if (File.Exists(info.LocalFile))
                File.Delete(info.LocalFile);
        }
        catch { }

        return Task.CompletedTask;
    }

    public async Task<long> CheckAdvanceSkillSetsUpdates()
    {
        try
        {
            long localSize = File.Exists(ClientFileSources.SkuaAdvancedSkillsFile)
                ? new FileInfo(ClientFileSources.SkuaAdvancedSkillsFile).Length
                : 0;

            string content =
                await ValidatedHttpExtensions.GetStringAsync(HttpClients.GitHubRaw, _skillsSetsRawUrl);

            long remoteSize = content.Length;

            return remoteSize != localSize ? remoteSize : 0;
        }
        catch
        {
            return -1;
        }
    }

    public async Task<bool> UpdateSkillSetsFile()
    {
        try
        {
            string content =
                await ValidatedHttpExtensions.GetStringAsync(HttpClients.GitHubRaw, _skillsSetsRawUrl);

            await File.WriteAllTextAsync(ClientFileSources.SkuaAdvancedSkillsFile, content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateQuestDataFile()
    {
        try
        {
            string content =
                await ValidatedHttpExtensions.GetStringAsync(HttpClients.GitHubRaw, _questDataRawUrl);

            await File.WriteAllTextAsync(ClientFileSources.SkuaQuestsFile, content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<long> CheckJunkItemsUpdates()
    {
        try
        {
            long localSize = File.Exists(ClientFileSources.SkuaJunkItemsFile)
                ? new FileInfo(ClientFileSources.SkuaJunkItemsFile).Length
                : 0;

            string content =
                await ValidatedHttpExtensions.GetStringAsync(HttpClients.GitHubRaw, _junkItemsRawUrl);

            long remoteSize = content.Length;

            return remoteSize != localSize ? remoteSize : 0;
        }
        catch
        {
            return -1;
        }
    }

    public async Task<bool> UpdateJunkItemsFile()
    {
        try
        {
            string content =
                await ValidatedHttpExtensions.GetStringAsync(HttpClients.GitHubRaw, _junkItemsRawUrl);

            await File.WriteAllTextAsync(ClientFileSources.SkuaJunkItemsFile, content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string?> GetLastCommitShaAsync(CancellationToken token)
    {
        try
        {
            string url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/commits/{_repoBranch}";

            using HttpResponseMessage response =
                await HttpClients.MakeGitHubApiRequestAsync(url);

            string content = await response.Content.ReadAsStringAsync(token);

            GitHubCommit? commit =
                JsonConvert.DeserializeObject<GitHubCommit>(content);

            return commit?.Sha;
        }
        catch
        {
            return null;
        }
    }

    private async Task<HashSet<string>> GetChangedFilesAsync(string oldSha, string newSha, CancellationToken token)
    {
        try
        {
            string url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/compare/{oldSha}...{newSha}";

            using HttpResponseMessage response =
                await HttpClients.MakeGitHubApiRequestAsync(url);

            string content = await response.Content.ReadAsStringAsync(token);

            GitHubCompare? compare =
                JsonConvert.DeserializeObject<GitHubCompare>(content);

            return compare?.Files?
                .Where(f => f.Status != "removed")
                .Select(f => f.FileName)
                .ToHashSet() ?? new HashSet<string>();
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessageBox($"Error getting changed files: {ex.Message}", "Debug Info");
            return new HashSet<string>();
        }
    }

    private string? GetStoredCommitSha()
    {
        try
        {
            return File.Exists(ClientFileSources.SkuaScriptsCommitFile)
                ? File.ReadAllText(ClientFileSources.SkuaScriptsCommitFile).Trim()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private Task StoreCommitShaAsync(string sha)
    {
        try
        {
            return File.WriteAllTextAsync(ClientFileSources.SkuaScriptsCommitFile, sha);
        }
        catch
        {
            return Task.CompletedTask;
        }
    }

    public IEnumerable<ScriptInfo> GetOutdatedScripts()
        => _scripts.Where(s => s.Outdated).ToList();

    public async Task<int> IncrementalUpdateScriptsAsync(IProgress<string>? progress, CancellationToken token)
    {
        try
        {
            progress?.Report("Checking for updates...");

            string? currentSha = await GetLastCommitShaAsync(token);

            if (string.IsNullOrEmpty(currentSha))
            {
                progress?.Report("Full refresh required.");
                await RefreshScriptsAsync(progress, token);
                return 0;
            }

            string? storedSha = GetStoredCommitSha();

            if (string.IsNullOrEmpty(storedSha))
            {
                progress?.Report("Initial sync...");
                await RefreshScriptsAsync(progress, token);
                await StoreCommitShaAsync(currentSha);
                return _scripts.Count;
            }

            if (storedSha == currentSha)
            {
                progress?.Report("Already up to date.");
                return 0;
            }

            progress?.Report("Checking changes...");
            HashSet<string> changedFiles = await GetChangedFilesAsync(storedSha, currentSha, token);

            HashSet<string> scriptChanges = changedFiles
                .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && f != "scripts.json")
                .ToHashSet();

            if (scriptChanges.Count == 0)
            {
                progress?.Report("No script changes detected.");
                await StoreCommitShaAsync(currentSha);
                return 0;
            }

            List<ScriptInfo> scripts = await GetScriptsInfo(true, token);

            List<ScriptInfo> toUpdate = scripts
                .Where(s => scriptChanges.Contains(s.FilePath))
                .ToList();

            int updated = 0;

            foreach (ScriptInfo script in toUpdate)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    await DownloadScriptAsync(script);
                    updated++;
                    progress?.Report($"Updated {updated}/{toUpdate.Count}: {script.Name}");
                }
                catch (Exception ex)
                {
                    progress?.Report($"Failed: {script.Name} - {ex.Message}");
                }
            }

            await StoreCommitShaAsync(currentSha);

            if (updated > 0)
                ClearCachedScriptsDirectory();

            progress?.Report($"Done. {updated} scripts updated.");
            return updated;
        }
        catch (TaskCanceledException)
        {
            progress?.Report("Update cancelled.");
            return 0;
        }
        catch (Exception ex)
        {
            _dialogService.ShowMessageBox(
                $"Incremental update failed:\r\n{ex.Message}\r\nFalling back to full refresh.",
                "Update Error");

            await RefreshScriptsAsync(progress, token);
            return 0;
        }
    }
}
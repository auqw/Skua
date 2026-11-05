using Newtonsoft.Json;

namespace Skua.Core.Models.GitHub;

public class ScriptInfo
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("tags")]
    public string[] Tags { get; set; }

    [JsonProperty("path")]
    public string FilePath { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("sha256")]
    public string? Sha256 { get; set; }

    [JsonProperty("creationDate")]
    public DateTime? CreationDate { get; set; }

    [JsonProperty("fileName")]
    public string FileName { get; set; }

    [JsonProperty("downloadUrl")]
    public string DownloadUrl { get; set; }

    public string RelativePath => FilePath == FileName ? "Scripts/" : $"Scripts/{FilePath.Replace(FileName, "")}";

    public string LocalFile => Path.Combine(ClientFileSources.SkuaScriptsDIR, FilePath);

    public string ManagerLocalFile => Path.Combine(ClientFileSources.SkuaScriptsDIR, FilePath);

    public bool Downloaded => File.Exists(LocalFile);

    public int LocalSize => Downloaded ? (int)new FileInfo(LocalFile).Length : 0;

    private sealed record LocalHashEntry(long Length, DateTime LastWriteUtc, string Hash);

    // Hashing every downloaded script is slow, so reuse the last hash until the file's size or write time changes.
    private LocalHashEntry? _localHash;

    public string? LocalSha256
    {
        get
        {
            try
            {
                FileInfo file = new(LocalFile);
                if (!file.Exists) return null;

                LocalHashEntry? cached = _localHash;
                if (cached is not null && cached.Length == file.Length && cached.LastWriteUtc == file.LastWriteTimeUtc)
                    return cached.Hash;

                using FileStream stream = file.OpenRead();
                string hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(stream)).ToLowerInvariant();
                _localHash = new(file.Length, file.LastWriteTimeUtc, hash);
                return hash;
            }
            catch { return null; }
        }
    }

    public bool Outdated => Downloaded && (LocalSize != Size || (!string.IsNullOrEmpty(Sha256) && LocalSha256 != Sha256));

    public override string ToString()
    {
        return FileName;
    }
}
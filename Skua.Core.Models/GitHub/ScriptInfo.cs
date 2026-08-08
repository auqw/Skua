using Newtonsoft.Json;
using System.Security.Cryptography;

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

    public string? LocalSha256
    {
        get
        {
            if (!Downloaded) return null;
            try
            {
                using SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
                using FileStream stream = File.OpenRead(LocalFile);
                byte[] hash = sha256.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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
using System.Text.Json.Serialization;

namespace SatinalmaPro.Shared.Models;

public class GuncellemeManifesti
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    /// <summary>Android ApplicationVersion (build). Masaüstü için 0 kalabilir.</summary>
    [JsonPropertyName("build")]
    public int Build { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = "";

    [JsonPropertyName("downloadUrlZip")]
    public string? DownloadUrlZip { get; set; }

    [JsonPropertyName("downloadUrlApk")]
    public string? DownloadUrlApk { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("zorunlu")]
    public bool Zorunlu { get; set; }
}

namespace SatinalmaPro.Models;

public class GuncellemeManifesti
{
    public string Version { get; set; } = "";
    public int Build { get; set; }
    public string DownloadUrl { get; set; } = "";
    public string? DownloadUrlZip { get; set; }
    public string? DownloadUrlApk { get; set; }
    public string? Notes { get; set; }
    public bool Zorunlu { get; set; }
}

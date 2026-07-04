namespace SatinalmaPro.Shared.Models;

public class FirebaseAyarlar
{
    public string ApiKey { get; set; } = "";
    public string ProjectId { get; set; } = "";
    public string GuncellemeManifestUrl { get; set; } = "";
    /// <summary>FCM v1 Service Account JSON dosya yolu.</summary>
    public string FcmServiceAccountYolu { get; set; } = "";
    /// <summary>FCM Legacy Server Key — eski projeler (opsiyonel).</summary>
    public string FcmServerKey { get; set; } = "";

    public bool Yapilandirildi =>
        !string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ProjectId);
}

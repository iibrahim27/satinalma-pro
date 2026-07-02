namespace SatinalmaPro.Mobile.Services;

/// <summary>FCM ve polling arasında paylaşılan toast gösterim kaydı.</summary>
public static class BildirimGosterimKaydi
{
    private const string Anahtar = "bildirim_gosterilen";

    public static bool IlkGosterimMi(Guid bildirimId)
    {
        var ham = Preferences.Default.Get(Anahtar, "");
        if (string.IsNullOrWhiteSpace(ham))
            return true;

        foreach (var parca in ham.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(parca, out var id) && id == bildirimId)
                return false;
        }

        return true;
    }

    public static void Isaretle(Guid bildirimId)
    {
        var mevcut = new HashSet<Guid>();
        var ham = Preferences.Default.Get(Anahtar, "");
        foreach (var parca in ham.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Guid.TryParse(parca, out var id))
                mevcut.Add(id);
        }

        if (!mevcut.Add(bildirimId))
            return;

        Preferences.Default.Set(Anahtar, string.Join(',', mevcut));
    }

    public static bool FcmAktif =>
        !string.IsNullOrWhiteSpace(Preferences.Default.Get(OturumServisi.FcmTokenAnahtari, (string?)null));
}

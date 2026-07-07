using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>Aynı iş akışı bildirimini tekilleştirmek için mantıksal anahtar (talep + tip + hedef).</summary>
public static class BildirimMantikAnahtari
{
    public static string Olustur(BildirimKaydi bildirim)
    {
        if (bildirim.TalepId is not { } talepId)
            return $"id:{bildirim.Id}";

        var hedef = !string.IsNullOrWhiteSpace(bildirim.HedefUid)
            ? $"u:{bildirim.HedefUid.Trim()}"
            : $"r:{(bildirim.HedefRol ?? "").Trim()}";

        return $"{talepId:N}:{bildirim.Tip}:{hedef}";
    }
}

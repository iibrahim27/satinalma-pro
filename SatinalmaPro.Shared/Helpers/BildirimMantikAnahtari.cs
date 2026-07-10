using System.Security.Cryptography;
using System.Text;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>Aynı iş akışı bildirimini tekilleştirmek için mantıksal anahtar (talep + tip + hedef).</summary>
public static class BildirimMantikAnahtari
{
    public static string Olustur(BildirimKaydi bildirim)
    {
        // Inbox kaynaklı / talepsiz kayıtlar: docId ile sabitle — her yüklemede Guid.NewGuid()
        // ile şişen veri/bildirimler belgesini önler.
        if (!string.IsNullOrWhiteSpace(bildirim.InboxDocId))
            return $"inbox:{bildirim.InboxDocId.Trim()}";

        if (bildirim.TalepId is not { } talepId)
            return $"id:{bildirim.Id}";

        var hedef = !string.IsNullOrWhiteSpace(bildirim.HedefUid)
            ? $"u:{bildirim.HedefUid.Trim()}"
            : $"r:{(bildirim.HedefRol ?? "").Trim()}";

        return $"{talepId:N}:{bildirim.Tip}:{hedef}";
    }

    /// <summary>Firestore inbox documentId → kararlı Guid (her okumada yeni id üretmez).</summary>
    public static Guid InboxDocIddenGuid(string docId)
    {
        if (Guid.TryParse(docId, out var g))
            return g;

        var bytes = MD5.HashData(Encoding.UTF8.GetBytes("inbox:" + docId.Trim()));
        return new Guid(bytes);
    }
}
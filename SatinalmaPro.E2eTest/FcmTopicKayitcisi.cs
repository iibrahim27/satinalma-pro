using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.E2eTest;

/// <summary>FCM topic abonelikleri ve push olaylarını bellek içi kaydeder (console/logcat doğrulaması).</summary>
public sealed class FcmTopicKayitcisi
{
    private readonly HashSet<string> _abonelikler = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<FcmPushKaydi> _pushlar = [];

    public IReadOnlyCollection<string> Abonelikler => _abonelikler;
    public IReadOnlyList<FcmPushKaydi> Pushlar => _pushlar;

    public static string TopicForRole(string? rol)
    {
        var key = KullaniciRolleri.Normalize(rol) switch
        {
            KullaniciRolleri.Admin => "admin",
            KullaniciRolleri.Yonetim => "yonetim",
            KullaniciRolleri.Satinalma => "satinalma",
            KullaniciRolleri.Sef => "sef",
            KullaniciRolleri.Saha => "saha",
            KullaniciRolleri.Depo => "depo",
            KullaniciRolleri.Atolye => "atolye",
            _ => (rol ?? "").Trim().ToLowerInvariant()
        };
        return $"/topics/{key}";
    }

    public void OturumAc(KullaniciProfili user)
    {
        foreach (var topic in _abonelikler.Where(t => t != TopicForRole(user.Rol)).ToList())
            _abonelikler.Remove(topic);

        var hedef = TopicForRole(user.Rol);
        _abonelikler.Add(hedef);
        Console.WriteLine($"[FCM] Oturum açıldı → {user.AdSoyad} ({user.Rol}) abone: {hedef}");
    }

    public void Push(string topic, string tip, Guid? talepId = null, string? mesaj = null)
    {
        var kayit = new FcmPushKaydi(topic, tip, talepId, mesaj ?? tip, DateTimeOffset.UtcNow);
        _pushlar.Add(kayit);
        Console.WriteLine($"[FCM] Push → {topic} | {tip} | talep={talepId}");
    }

    public void PushRol(string? hedefRol, string tip, Guid? talepId = null, string? mesaj = null) =>
        Push(TopicForRole(hedefRol), tip, talepId, mesaj);

    public void PushUid(string hedefUid, string tip, Guid? talepId = null, string? mesaj = null) =>
        Push($"/topics/user-{hedefUid}", tip, talepId, mesaj);

    public bool AboneMi(string topic) => _abonelikler.Contains(topic);

    public bool PushGittiMi(string topic, string tip, Guid? talepId = null) =>
        _pushlar.Any(p =>
            p.Topic.Equals(topic, StringComparison.OrdinalIgnoreCase)
            && p.Tip == tip
            && (talepId is null || p.TalepId == talepId));

    public void Temizle()
    {
        _abonelikler.Clear();
        _pushlar.Clear();
    }
}

public sealed record FcmPushKaydi(
    string Topic,
    string Tip,
    Guid? TalepId,
    string Mesaj,
    DateTimeOffset Zaman);

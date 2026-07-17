using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

/// <summary>
/// Kalem miktarını birden fazla firmaya bölme — geriye uyumlu tek OnaylananTeklifId desteği.
/// </summary>
public static class KalemFirmaAtamaYardimcisi
{
    public const double Tolerans = 0.0001;

    public static bool OnayliMi(SatinalmaTalepKalemi kalem) =>
        EtkinAtamalar(kalem).Count > 0;

    /// <summary>
    /// FirmaAtamalari doluysa onu; değilse OnaylananTeklifId → tek tam-miktar atama.
    /// </summary>
    public static List<KalemFirmaAtamasi> EtkinAtamalar(SatinalmaTalepKalemi kalem)
    {
        var liste = kalem.FirmaAtamalari?
            .Where(a => a.TeklifId != Guid.Empty && a.Miktar > Tolerans)
            .OrderByDescending(a => a.Miktar)
            .ToList() ?? [];

        if (liste.Count > 0)
            return liste;

        if (kalem.OnaylananTeklifId is { } tid)
        {
            return
            [
                new KalemFirmaAtamasi
                {
                    TeklifId = tid,
                    Miktar = kalem.Miktar,
                    KabulEdilenMiktar = kalem.KabulEdilenMiktar,
                    SiparisTamamlandi = kalem.SiparisTamamlandi
                }
            ];
        }

        return [];
    }

    public static IEnumerable<Guid> OnayliTeklifIdleri(SatinalmaTalepKalemi kalem) =>
        EtkinAtamalar(kalem).Select(a => a.TeklifId).Distinct();

    public static void Dogrula(SatinalmaTalepKalemi kalem, IReadOnlyList<KalemFirmaAtamasi> atamalar)
    {
        if (atamalar.Count == 0)
            throw new InvalidOperationException($"«{kalem.Malzeme}» için en az bir firma ataması gerekli.");

        if (atamalar.Any(a => a.Miktar <= Tolerans))
            throw new InvalidOperationException($"«{kalem.Malzeme}» atama miktarları sıfırdan büyük olmalı.");

        if (atamalar.GroupBy(a => a.TeklifId).Any(g => g.Count() > 1))
            throw new InvalidOperationException($"«{kalem.Malzeme}» için aynı firma birden fazla kez seçilemez.");

        var toplam = atamalar.Sum(a => a.Miktar);
        if (Math.Abs(toplam - kalem.Miktar) > Tolerans)
            throw new InvalidOperationException(
                $"«{kalem.Malzeme}» atama toplamı ({toplam:N2}) talep miktarına ({kalem.Miktar:N2}) eşit olmalı.");
    }

    /// <summary>Atamaları yazar; OnaylananTeklifId = en büyük miktarlı firma; kalem kabul özetini senkronlar.</summary>
    public static void Uygula(SatinalmaTalepKalemi kalem, IEnumerable<KalemFirmaAtamasi> atamalar)
    {
        var liste = atamalar
            .Where(a => a.TeklifId != Guid.Empty && a.Miktar > Tolerans)
            .Select(a => new KalemFirmaAtamasi
            {
                TeklifId = a.TeklifId,
                Miktar = a.Miktar,
                KabulEdilenMiktar = a.KabulEdilenMiktar,
                SiparisTamamlandi = a.SiparisTamamlandi
            })
            .ToList();

        Dogrula(kalem, liste);
        kalem.FirmaAtamalari = liste;
        kalem.OnaylananTeklifId = liste
            .OrderByDescending(a => a.Miktar)
            .Select(a => (Guid?)a.TeklifId)
            .First();
        KabulOzetiniSenkronla(kalem);
    }

    public static void TekFirmayaAta(SatinalmaTalepKalemi kalem, Guid teklifId) =>
        Uygula(kalem, [new KalemFirmaAtamasi { TeklifId = teklifId, Miktar = kalem.Miktar }]);

    public static void Temizle(SatinalmaTalepKalemi kalem)
    {
        kalem.FirmaAtamalari = [];
        kalem.OnaylananTeklifId = null;
        kalem.KabulEdilenMiktar = 0;
        kalem.SiparisTamamlandi = false;
    }

    public static KalemFirmaAtamasi? AtamaBul(SatinalmaTalepKalemi kalem, Guid teklifId) =>
        EtkinAtamalar(kalem).FirstOrDefault(a => a.TeklifId == teklifId);

    public static void KabulEkle(SatinalmaTalepKalemi kalem, Guid teklifId, double miktar)
    {
        var atamalar = EtkinAtamalar(kalem).Select(a => new KalemFirmaAtamasi
        {
            TeklifId = a.TeklifId,
            Miktar = a.Miktar,
            KabulEdilenMiktar = a.KabulEdilenMiktar,
            SiparisTamamlandi = a.SiparisTamamlandi
        }).ToList();

        var atama = atamalar.FirstOrDefault(a => a.TeklifId == teklifId)
            ?? throw new InvalidOperationException("Firma ataması bulunamadı.");

        atama.KabulEdilenMiktar += miktar;
        if (atama.KabulEdilenMiktar > atama.Miktar + Tolerans)
            atama.Miktar = atama.KabulEdilenMiktar;
        if (atama.KabulEdilenMiktar >= atama.Miktar - Tolerans)
            atama.SiparisTamamlandi = true;

        kalem.FirmaAtamalari = atamalar;
        if (kalem.OnaylananTeklifId is null)
            kalem.OnaylananTeklifId = atamalar.OrderByDescending(a => a.Miktar).First().TeklifId;
        KabulOzetiniSenkronla(kalem);
    }

    public static void SevkiyatiTamamla(SatinalmaTalepKalemi kalem, Guid teklifId)
    {
        var atamalar = EtkinAtamalar(kalem).Select(a => new KalemFirmaAtamasi
        {
            TeklifId = a.TeklifId,
            Miktar = a.Miktar,
            KabulEdilenMiktar = a.KabulEdilenMiktar,
            SiparisTamamlandi = a.SiparisTamamlandi
        }).ToList();

        var atama = atamalar.FirstOrDefault(a => a.TeklifId == teklifId)
            ?? throw new InvalidOperationException("Firma ataması bulunamadı.");

        if (atama.KabulEdilenMiktar < atama.Miktar - Tolerans)
            atama.Miktar = atama.KabulEdilenMiktar;
        atama.SiparisTamamlandi = true;

        kalem.FirmaAtamalari = atamalar;
        KabulOzetiniSenkronla(kalem);
    }

    public static void KabulOzetiniSenkronla(SatinalmaTalepKalemi kalem)
    {
        var atamalar = kalem.FirmaAtamalari?
            .Where(a => a.TeklifId != Guid.Empty && a.Miktar > Tolerans)
            .ToList() ?? [];

        if (atamalar.Count == 0)
            return;

        kalem.KabulEdilenMiktar = atamalar.Sum(a => a.KabulEdilenMiktar);
        kalem.SiparisTamamlandi = atamalar.All(a =>
            a.SiparisTamamlandi || a.KabulEdilenMiktar >= a.Miktar - Tolerans);
        // Kalem toplam miktarı atama toplamına hizala (fazla teslim sonrası)
        var atamaToplam = atamalar.Sum(a => a.Miktar);
        if (atamaToplam > Tolerans && Math.Abs(atamaToplam - kalem.Miktar) > Tolerans)
            kalem.Miktar = atamaToplam;
    }

    public static string OzetMetni(
        SatinalmaTalepKalemi kalem,
        IEnumerable<SatinalmaTeklif> teklifler)
    {
        var atamalar = EtkinAtamalar(kalem);
        if (atamalar.Count == 0)
            return "Firma seçilmedi";

        return string.Join(" + ", atamalar.Select(a =>
        {
            var firma = teklifler.FirstOrDefault(t => t.Id == a.TeklifId)?.FirmaAdi ?? "?";
            return $"{a.Miktar:N2} {firma}";
        }));
    }

    /// <summary>
    /// Firma miktarını ayarlar; diğer firmaların payını kalan miktara oransal ölçekler.
    /// </summary>
    public static void FirmaMiktariniAyarla(SatinalmaTalepKalemi kalem, Guid teklifId, double miktar)
    {
        if (miktar <= Tolerans)
            throw new InvalidOperationException("Miktar sıfırdan büyük olmalı.");
        if (miktar > kalem.Miktar + Tolerans)
            throw new InvalidOperationException("Miktar talep miktarını aşamaz.");

        var diger = EtkinAtamalar(kalem)
            .Where(a => a.TeklifId != teklifId)
            .Select(a => new KalemFirmaAtamasi
            {
                TeklifId = a.TeklifId,
                Miktar = a.Miktar,
                KabulEdilenMiktar = a.KabulEdilenMiktar,
                SiparisTamamlandi = a.SiparisTamamlandi
            })
            .ToList();

        var kalan = kalem.Miktar - miktar;
        if (diger.Count == 0)
        {
            if (Math.Abs(miktar - kalem.Miktar) > Tolerans)
                throw new InvalidOperationException(
                    "Tek firmada miktar talep toplamına eşit olmalı. Başka firma ekleyin veya tüm miktarı seçin.");
            Uygula(kalem, [new KalemFirmaAtamasi { TeklifId = teklifId, Miktar = miktar }]);
            return;
        }

        var sonuc = new List<KalemFirmaAtamasi>();
        if (kalan > Tolerans)
        {
            var digerToplam = diger.Sum(a => a.Miktar);
            if (digerToplam <= Tolerans)
            {
                diger[0].Miktar = kalan;
                sonuc.Add(diger[0]);
            }
            else
            {
                foreach (var a in diger)
                {
                    a.Miktar = a.Miktar / digerToplam * kalan;
                    if (a.Miktar > Tolerans)
                        sonuc.Add(a);
                }

                var fark = kalan - sonuc.Sum(a => a.Miktar);
                if (sonuc.Count > 0)
                    sonuc[0].Miktar += fark;
            }
        }

        sonuc.Add(new KalemFirmaAtamasi { TeklifId = teklifId, Miktar = miktar });
        Uygula(kalem, sonuc);
    }
}

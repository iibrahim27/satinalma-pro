using System.Globalization;
using SatinalmaPro.Models;

namespace SatinalmaPro.Helpers;

public static class SatinalmaOneriYardimcisi
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static bool HerhangiKalemOnerili(SatinalmaTalep talep) =>
        talep.Kalemler?.Any(k => k.OnerilenTeklifId != null) == true;

    public static bool TumKalemlerOnerili(SatinalmaTalep talep) =>
        talep.Kalemler is { Count: > 0 } && talep.Kalemler.All(k => k.OnerilenTeklifId != null);

    public static SatinalmaTeklifFiyati? KalemOneriFiyati(SatinalmaTalep talep, SatinalmaTalepKalemi kalem)
    {
        if (kalem.OnerilenTeklifId is not { } teklifId)
            return null;

        var teklif = (talep.Teklifler ?? []).FirstOrDefault(t => t.Id == teklifId);
        return teklif?.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
    }

    public static SatinalmaTeklif? KalemOneriTeklifi(SatinalmaTalep talep, SatinalmaTalepKalemi kalem) =>
        kalem.OnerilenTeklifId is { } id
            ? (talep.Teklifler ?? []).FirstOrDefault(t => t.Id == id)
            : null;

    public static (decimal AraToplam, decimal Kdv, decimal GenelToplam) OnerilenToplamlar(SatinalmaTalep talep)
    {
        talep.TeklifFiyatlariniGuncellePublic();

        if (talep.SatinalmaKalemOnerisiElleSecildi && HerhangiKalemOnerili(talep))
        {
            decimal ara = 0, kdv = 0;
            foreach (var kalem in talep.Kalemler.Where(k => k.OnerilenTeklifId != null))
            {
                var fiyat = KalemOneriFiyati(talep, kalem);
                if (fiyat is null)
                    continue;
                ara += fiyat.ToplamTutar;
                kdv += fiyat.KdvTutari;
            }

            return (ara, kdv, ara + kdv);
        }

        var teklif = talep.OnerilenTeklifFirma();
        if (teklif is null)
            return (0, 0, 0);

        return (teklif.AraToplam, teklif.KdvTutari, teklif.GenelToplam);
    }

    public static string OneriMetni(SatinalmaTalep talep)
    {
        talep.TeklifFiyatlariniGuncellePublic();

        if (talep.SatinalmaKalemOnerisiElleSecildi && HerhangiKalemOnerili(talep))
        {
            var satirlar = talep.Kalemler
                .OrderBy(k => k.SiraNo)
                .Where(k => k.OnerilenTeklifId != null)
                .Select(k =>
                {
                    var teklif = KalemOneriTeklifi(talep, k)!;
                    var fiyat = KalemOneriFiyati(talep, k);
                    var birim = fiyat?.BirimFiyatGosterim(teklif.UsdKuru, teklif.EurKuru) ?? "—";
                    return $"{k.Malzeme}: {teklif.FirmaAdi} ({birim})";
                })
                .ToList();

            var (_, _, genel) = OnerilenToplamlar(talep);
            return $"Satınalma önerisi (kalem bazlı): {string.Join(" · ", satirlar)} — KDV Dahil: {genel.ToString("N2", Tr)} ₺";
        }

        if (talep.SatinalmaOnerisiElleSecildi && talep.YonetimOnerilenTeklifId is { } oneriId)
        {
            var onerilen = (talep.Teklifler ?? []).FirstOrDefault(t => t.Id == oneriId);
            if (onerilen is not null)
            {
                return
                    $"Satınalma önerisi: {onerilen.FirmaAdi} — KDV Hariç: {onerilen.AraToplam.ToString("N2", Tr)} ₺ | KDV Dahil: {onerilen.GenelToplam.ToString("N2", Tr)} ₺ (firma seçildi)";
            }
        }

        var otomatik = talep.EnDusukFiyatliTeklif();
        if (otomatik is not null && otomatik.GenelToplam > 0)
        {
            return
                $"Satınalma önerisi: {otomatik.FirmaAdi} — KDV Hariç: {otomatik.AraToplam.ToString("N2", Tr)} ₺ | KDV Dahil: {otomatik.GenelToplam.ToString("N2", Tr)} ₺ (en uygun fiyat)";
        }

        return (talep.Teklifler?.Count ?? 0) == 0
            ? "Teklif girildikten sonra «Onaya Gönder» ile yönetime iletebilirsiniz; tek teklif yeterlidir."
            : "Fiyatları tamamlayın; kalem bazlı öneri seçebilir veya firmayı «Öneri Olarak İşaretle» ile işaretleyebilirsiniz.";
    }

    public static void FirmaOnerisiAyarla(SatinalmaTalep talep, Guid teklifId)
    {
        talep.YonetimOnerilenTeklifId = teklifId;
        talep.SatinalmaOnerisiElleSecildi = true;
        talep.SatinalmaKalemOnerisiElleSecildi = false;
        foreach (var kalem in talep.Kalemler)
            kalem.OnerilenTeklifId = null;
    }

    public static void KalemOnerisiGuncelle(SatinalmaTalep talep)
    {
        if (!HerhangiKalemOnerili(talep))
        {
            talep.SatinalmaKalemOnerisiElleSecildi = false;
            if (talep.YonetimOnerilenTeklifId is null)
                talep.SatinalmaOnerisiElleSecildi = false;
            return;
        }

        // Kısmi seçim gönderimi engellemesin — tamamı dolunca kalem önerisi sayılır
        talep.SatinalmaKalemOnerisiElleSecildi = TumKalemlerOnerili(talep);
        if (talep.SatinalmaKalemOnerisiElleSecildi)
        {
            talep.SatinalmaOnerisiElleSecildi = true;
            talep.YonetimOnerilenTeklifId = null;
        }
    }

    public static void TeklifSilindi(SatinalmaTalep talep, Guid teklifId)
    {
        foreach (var kalem in talep.Kalemler.Where(k => k.OnerilenTeklifId == teklifId))
            kalem.OnerilenTeklifId = null;

        if (talep.YonetimOnerilenTeklifId == teklifId)
        {
            talep.YonetimOnerilenTeklifId = null;
            talep.SatinalmaOnerisiElleSecildi = false;
        }

        KalemOnerisiGuncelle(talep);
    }

    public static bool HucreOneriVurgula(SatinalmaTalep talep, SatinalmaTalepKalemi kalem, SatinalmaTeklif teklif,
        SatinalmaTeklif? onerilenTeklif, bool enDusuk)
    {
        if (talep.SatinalmaKalemOnerisiElleSecildi)
            return kalem.OnerilenTeklifId == teklif.Id;

        return (onerilenTeklif != null && teklif.Id == onerilenTeklif.Id) || enDusuk;
    }
}

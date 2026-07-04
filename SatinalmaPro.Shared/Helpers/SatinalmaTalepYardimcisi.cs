using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Helpers;

public static class SatinalmaTalepYardimcisi
{
    /// <summary>Yönetime gönderilmiş — yönetim teklif onayı bekliyor.</summary>
    public static bool TeklifYonetimOnayiBekliyor(SatinalmaTalep talep) =>
        talep.Durum == SatinalmaTalepDurumlari.YonetimOnayinda
        && (talep.Teklifler?.Count ?? 0) > 0
        && !talep.HerhangiKalemOnayli
        && !talep.YonetimOnayKilitli;

    /// <summary>Satınalma — teklifler girildi, henüz yönetime iletilmedi veya düzeltme aşamasında.</summary>
    public static bool SatinalmaTeklifDegerlendirmede(SatinalmaTalep talep) =>
        TeklifDuzenlemeDevamEdiyor(talep);

    /// <summary>Onay öncesi talep kalemi ve teklif düzenleme devam ediyor.</summary>
    public static bool TeklifDuzenlemeDevamEdiyor(SatinalmaTalep talep) =>
        !talep.YonetimOnayKilitli
        && !talep.HerhangiKalemOnayli
        && (talep.Teklifler?.Count ?? 0) > 0
        && talep.Durum is SatinalmaTalepDurumlari.Karsilastirma
            or SatinalmaTalepDurumlari.TeklifGirisi
            or SatinalmaTalepDurumlari.ImzaSurecinde
            or SatinalmaTalepDurumlari.Hazirlaniyor
            or SatinalmaTalepDurumlari.YonetimOnayinda;

    /// <summary>Teklif girilmiş — yönetim teklif kararı verecek (yalnızca yönetime gönderilmiş).</summary>
    public static bool YonetimTeklifKarariBekliyor(SatinalmaTalep talep) =>
        TeklifYonetimOnayiBekliyor(talep)
        && !talep.TeklifsizYonetimOnayi;

    public static bool IcerikVar(SatinalmaTalep talep) =>
        !string.IsNullOrWhiteSpace(talep.TalepAciklamasi)
        || talep.Kalemler?.Any(k => !string.IsNullOrWhiteSpace(k.Malzeme)) == true;

    /// <summary>Kaydedilmiş, yönetime henüz gönderilmemiş veya reddedilmiş — düzenlenebilir talepler.</summary>
    public static bool GonderimOncesiDuzenlenebilir(SatinalmaTalep talep) =>
        talep.Durum is SatinalmaTalepDurumlari.Hazirlaniyor or SatinalmaTalepDurumlari.Reddedildi;

    public static bool FormDuzenlenebilir(SatinalmaTalep talep) =>
        talep.Durum == SatinalmaTalepDurumlari.Taslak || GonderimOncesiDuzenlenebilir(talep);

    /// <summary>Talep kalemleri düzenlenebilir — gönderim öncesi veya onay öncesi teklif aşamasında.</summary>
    public static bool TalepKalemleriDuzenlenebilir(SatinalmaTalep talep) =>
        FormDuzenlenebilir(talep) || TeklifDuzenlemeDevamEdiyor(talep);

    public static void TalepKalemleriniTekliflerleSenkronla(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        foreach (var teklif in talep.Teklifler)
        {
            teklif.Fiyatlar ??= [];
            teklif.Fiyatlar.RemoveAll(f => talep.Kalemler.All(k => k.Id != f.KalemId));

            foreach (var kalem in talep.Kalemler.OrderBy(k => k.SiraNo))
            {
                if (teklif.Fiyatlar.All(f => f.KalemId != kalem.Id))
                {
                    teklif.Fiyatlar.Add(new SatinalmaTeklifFiyati
                    {
                        KalemId = kalem.Id,
                        KdvOrani = teklif.KdvOrani > 0 ? teklif.KdvOrani : 20
                    });
                }
            }

            teklif.FiyatlariHesapla(talep.Kalemler);
        }
    }

    public static void TeklifDegisikligiIsle(SatinalmaTalep talep)
    {
        TalepKalemleriniTekliflerleSenkronla(talep);
        SatinalmaTalepSenkronYardimcisi.Dokun(talep);
        SatinalmaIsAkisi.TeklifRevizyonuBaslat(talep);
    }

    /// <summary>Satınalma rolüyle oluşturulmuş talep — yönetime göndermeden teklif girişi yalnızca buna izin verilir.</summary>
    public static bool SatinalmaOlusturdu(SatinalmaTalep talep) =>
        SatinalmaOlusturdu(talep.OlusturanRol);

    public static bool SatinalmaOlusturdu(string olusturanRol) =>
        !string.IsNullOrWhiteSpace(olusturanRol)
        && KullaniciRolleri.Normalize(olusturanRol) == KullaniciRolleri.Satinalma;

    /// <summary>Satınalma kendi talebine yönetime iletmeden teklif girebilir.</summary>
    public static bool SatinalmaIcTeklifGirisi(SatinalmaTalep talep) =>
        SatinalmaIcTeklifGirisi(
            talep.Durum,
            talep.OlusturanRol,
            talep.Teklifler?.Count ?? 0,
            talep.YonetimOnayKilitli,
            talep.TalepTuru);

    public static bool SatinalmaIcTeklifGirisi(
        string durum,
        string olusturanRol,
        int teklifSayisi,
        bool yonetimOnayKilitli,
        string talepTuru) =>
        SatinalmaOlusturdu(olusturanRol)
        && talepTuru != TalepTurleri.Acil
        && !yonetimOnayKilitli
        && durum is SatinalmaTalepDurumlari.Hazirlaniyor or SatinalmaTalepDurumlari.ImzaSurecinde
        && teklifSayisi == 0;

    public static void KayitOncesiHazirla(SatinalmaTalep talep)
    {
        if (talep.Durum == SatinalmaTalepDurumlari.Taslak)
            talep.Durum = SatinalmaTalepDurumlari.Hazirlaniyor;
    }

    /// <summary>Kalıcı kayıtlarda Taslak kalmamalı — boş olanları at, dolu olanları Hazırlanıyor yap.</summary>
    public static bool BosTaslaklariTemizle(IList<SatinalmaTalep> talepler, Guid? korunanId = null)
    {
        var silindi = false;

        for (var i = talepler.Count - 1; i >= 0; i--)
        {
            var talep = talepler[i];
            if (talep.Durum != SatinalmaTalepDurumlari.Taslak)
                continue;

            if (IcerikVar(talep))
                continue;

            if (korunanId.HasValue && talep.Id == korunanId.Value)
                continue;

            talepler.RemoveAt(i);
            silindi = true;
        }

        return silindi;
    }

    public static bool TaslaklariNormalizeEt(IList<SatinalmaTalep> talepler, bool silBosTaslaklari = true)
    {
        var degisti = false;
        for (var i = talepler.Count - 1; i >= 0; i--)
        {
            var talep = talepler[i];
            if (talep.Durum != SatinalmaTalepDurumlari.Taslak)
                continue;

            if (!IcerikVar(talep))
            {
                if (silBosTaslaklari)
                {
                    talepler.RemoveAt(i);
                    degisti = true;
                }

                continue;
            }

            talep.Durum = SatinalmaTalepDurumlari.Hazirlaniyor;
            degisti = true;
        }

        return degisti;
    }

    /// <summary>Eski kayıtlarda yönetim onayı var ama kilit bayrağı eksikse düzeltir.</summary>
    public static bool YonetimOnayMirasiniGuncelle(SatinalmaTalep talep)
    {
        if (talep.YonetimOnayKilitli)
            return false;

        if (talep.Durum is not (SatinalmaTalepDurumlari.Onaylandi or SatinalmaTalepDurumlari.SiparisOlusturuldu))
            return false;

        if (!string.IsNullOrWhiteSpace(talep.YonetimOnaylayanUid)
            || !string.IsNullOrWhiteSpace(talep.YonetimOnayTarihi)
            || talep.TeklifsizYonetimOnayi)
        {
            talep.YonetimOnayKilitli = true;
            return true;
        }

        return false;
    }

    public static bool YonetimOnayMiraslariniGuncelle(IList<SatinalmaTalep> talepler)
    {
        var degisti = false;
        foreach (var talep in talepler)
        {
            if (YonetimOnayMirasiniGuncelle(talep))
                degisti = true;
        }

        return degisti;
    }
}

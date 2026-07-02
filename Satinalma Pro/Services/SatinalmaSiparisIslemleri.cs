using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class SatinalmaSiparisIslemleri
{
    public static void KalemBazliOnayla(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        var onayliKalemler = talep.Kalemler.Where(k => k.OnaylananTeklifId != null).ToList();
        if (onayliKalemler.Count == 0)
            throw new InvalidOperationException("En az bir malzeme için firma seçin.");

        foreach (var teklif in talep.Teklifler)
            teklif.Onaylandi = onayliKalemler.Any(k => k.OnaylananTeklifId == teklif.Id);

        var anaTeklifId = onayliKalemler
            .GroupBy(k => k.OnaylananTeklifId!.Value)
            .OrderByDescending(g => g.Count())
            .First().Key;

        talep.OnaylananTeklifId = anaTeklifId;
        talep.YonetimOnerilenTeklifId = anaTeklifId;
        talep.Durum = SatinalmaTalepDurumlari.Onaylandi;

        talep.FirmaSiparisNolari ??= [];
        foreach (var teklifId in onayliKalemler.Select(k => k.OnaylananTeklifId!.Value).Distinct())
        {
            if (!talep.FirmaSiparisNolari.ContainsKey(teklifId))
                talep.FirmaSiparisNolari[teklifId] = SatinalmaDepo.YeniSiparisNoOlustur();
        }

        talep.SiparisNo = talep.FirmaSiparisNolari[anaTeklifId];
        YonetimOnayKaydet(talep);
        SatinalmaTalepYardimcisi.Dokun(talep);
        SatinalmaDepo.Kaydet();
    }

    public static async Task KalemBazliOnaylaAsync(SatinalmaTalep talep)
    {
        KalemBazliOnayla(talep);
        await SatinalmaKayitYardimcisi.BulutaHemenGonderAsync();

        try
        {
            await BildirimYoneticisi.GecersizleriOkunduYapAsync();
        }
        catch
        {
            // bildirim hatası onayı engellemez
        }

        var onayliKalemler = talep.Kalemler.Where(k => k.OnaylananTeklifId != null).ToList();
        var anaTeklifId = talep.OnaylananTeklifId;
        var anaTeklif = anaTeklifId is { } tid
            ? talep.Teklifler.FirstOrDefault(t => t.Id == tid)
            : null;
        var firmaSayisi = onayliKalemler.Select(k => k.OnaylananTeklifId).Distinct().Count();
        var firmaAdi = firmaSayisi == 1 ? anaTeklif?.FirmaAdi : null;

        await SatinalmaBildirimleri.OnaylandiAsync(
            talep, firmaAdi, hedefRol: KullaniciRolleri.Satinalma);

        if (!string.IsNullOrWhiteSpace(talep.OlusturanUid))
        {
            await SatinalmaBildirimleri.OnaylandiAsync(
                talep, hedefUid: talep.OlusturanUid);
        }
    }

    public static void SiparisVer(SatinalmaTalep talep)
    {
        if (talep.Durum != SatinalmaTalepDurumlari.Onaylandi)
            throw new InvalidOperationException("Yalnızca onaylanmış talepler için sipariş verilebilir.");

        talep.FirmaSiparisNolari ??= [];
        foreach (var teklifId in talep.Kalemler
                     .Where(k => k.OnaylananTeklifId != null)
                     .Select(k => k.OnaylananTeklifId!.Value)
                     .Distinct())
        {
            SatinalmaDepo.SiparisNoAl(talep, teklifId);
        }

        if (string.IsNullOrWhiteSpace(talep.SiparisNo) && talep.OnaylananTeklifId is { } anaId)
            talep.SiparisNo = SatinalmaDepo.SiparisNoAl(talep, anaId);

        talep.Durum = SatinalmaTalepDurumlari.SiparisOlusturuldu;
        SatinalmaTalepYardimcisi.Dokun(talep);
        SatinalmaDepo.Kaydet();
    }

    public static async Task SiparisVerAsync(SatinalmaTalep talep)
    {
        SiparisVer(talep);
        await SatinalmaKayitYardimcisi.BulutaHemenGonderAsync();
        try { await SatinalmaBildirimleri.SiparisOlusturulduAsync(talep); } catch { /* */ }
    }

    public static void FirmaOnaylariniGeriAl(SatinalmaTalep talep)
    {
        if (!KullaniciYetkileri.SatinalmaFirmaOnayiDuzenlenebilir())
            throw new InvalidOperationException("Bu onayı geri alma yetkiniz yok.");

        if (!talep.HerhangiKalemOnayli && talep.Durum != SatinalmaTalepDurumlari.Onaylandi)
            throw new InvalidOperationException("Geri alınacak firma onayı bulunamadı.");

        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        foreach (var kalem in talep.Kalemler)
        {
            kalem.OnaylananTeklifId = null;
            kalem.KabulEdilenMiktar = 0;
            kalem.SiparisTamamlandi = false;
        }

        foreach (var teklif in talep.Teklifler)
            teklif.Onaylandi = false;

        talep.OnaylananTeklifId = null;
        talep.FirmaSiparisNolari?.Clear();
        talep.SiparisNo = "";
        talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;
        talep.YonetimOnayKilitli = false;

        SatinalmaTalepYardimcisi.Dokun(talep);
        SatinalmaDepo.Kaydet();
    }

    public static async Task FirmaOnaylariniGeriAlAsync(SatinalmaTalep talep)
    {
        FirmaOnaylariniGeriAl(talep);
        await SatinalmaKayitYardimcisi.BulutaHemenGonderAsync();
    }

    public static void MalKabulVeDepoyaKaydet(
        OnaylananMalzemeSatiri satir,
        double miktar,
        string kategori,
        string tarih,
        string fisNo,
        string teslimAlan,
        string depoSaha,
        string? aciklama = null)
    {
        if (!KullaniciYetkileri.MalKabulVeStokAktarYapabilir())
            throw new InvalidOperationException("Mal kabul işlemi yalnızca Satınalma rolü tarafından yapılabilir.");

        if (miktar <= 0)
            throw new InvalidOperationException("Miktar sıfırdan büyük olmalıdır.");

        var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == satir.TalepId)
            ?? throw new InvalidOperationException("Talep bulunamadı.");

        var kalem = talep.Kalemler.FirstOrDefault(k => k.Id == satir.KalemId)
            ?? throw new InvalidOperationException("Kalem bulunamadı.");

        if (miktar > satir.KalanMiktar + 0.0001)
            throw new InvalidOperationException($"Girilen miktar kalan miktardan ({satir.KalanMiktar:N2}) fazla olamaz.");

        kalem.KabulEdilenMiktar += miktar;
        if (kalem.KabulEdilenMiktar >= kalem.Miktar - 0.0001)
            kalem.SiparisTamamlandi = true;

        satir.KabulEdilenMiktar = kalem.KabulEdilenMiktar;
        satir.SiparisTamamlandi = kalem.SiparisTamamlandi;

        MalzemeKategoriDeposu.Ekle(kategori);

        var kayit = satir.AlinanMalzemeKaydinaDonustur(
            miktar, kategori, tarih, fisNo, teslimAlan, depoSaha, aciklama);
        ModulVeriDeposu.AlinanMalzemeler.Add(kayit);

        AlinanMalzemeAktarimServisi.StogaGirisKaydet(
            satir, miktar, kategori, tarih, teslimAlan, depoSaha);

        SatinalmaTalepYardimcisi.Dokun(talep);
        SatinalmaDepo.Kaydet();
        _ = SatinalmaKayitYardimcisi.BulutaHemenGonderAsync();

        try
        {
            _ = SatinalmaBildirimleri.MalKabulEdildiAsync(talep, $"{satir.Malzeme} · {miktar:N2} {satir.Birim}");
        }
        catch
        {
            // bildirim hatası mal kabulü engellemez
        }
    }

    private static void YonetimOnayKaydet(SatinalmaTalep talep)
    {
        if (!string.IsNullOrWhiteSpace(talep.YonetimOnaylayanUid))
            return;

        talep.YonetimOnaylayanUid = OturumYoneticisi.AktifKullanici?.Uid ?? "";
        talep.YonetimOnaylayanAd = KullaniciYetkileri.AktifKullaniciAdi() ?? "";
        talep.YonetimOnaylayanEposta = OturumYoneticisi.AktifKullanici?.Eposta ?? "";
        talep.YonetimOnayTarihi = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        talep.YonetimOnayKilitli = true;
    }
}

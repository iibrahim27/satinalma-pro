using SatinalmaPro.Helpers;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Procurement;

namespace SatinalmaPro.Services;

public static class SatinalmaSiparisIslemleri
{
    public static void KalemBazliOnayla(SatinalmaTalep talep)
    {
        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        var onayliKalemler = talep.Kalemler.Where(KalemFirmaAtamaYardimcisi.OnayliMi).ToList();
        if (onayliKalemler.Count == 0)
            throw new InvalidOperationException("En az bir malzeme için firma seçin.");

        // Tek OnaylananTeklifId kalmış atamaları normalize et; bölünmüşleri doğrula.
        foreach (var kalem in onayliKalemler)
        {
            var atamalar = KalemFirmaAtamaYardimcisi.EtkinAtamalar(kalem);
            KalemFirmaAtamaYardimcisi.Uygula(kalem, atamalar);
        }

        var tumTeklifIdleri = onayliKalemler
            .SelectMany(KalemFirmaAtamaYardimcisi.OnayliTeklifIdleri)
            .Distinct()
            .ToList();

        foreach (var teklif in talep.Teklifler)
            teklif.Onaylandi = tumTeklifIdleri.Contains(teklif.Id);

        var anaTeklifId = onayliKalemler
            .SelectMany(KalemFirmaAtamaYardimcisi.EtkinAtamalar)
            .GroupBy(a => a.TeklifId)
            .OrderByDescending(g => g.Sum(a => a.Miktar))
            .ThenByDescending(g => g.Count())
            .First().Key;

        talep.OnaylananTeklifId = anaTeklifId;
        talep.Status = ProcurementStatus.Approved;
        talep.Priority = ProcurementTalepAdapter.EffectivePriority(talep);
        talep.YonetimOnerilenTeklifId = anaTeklifId;
        talep.Durum = SatinalmaTalepDurumlari.Onaylandi;

        talep.FirmaSiparisNolari ??= [];
        foreach (var teklifId in tumTeklifIdleri)
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

        var onayliKalemler = talep.Kalemler.Where(KalemFirmaAtamaYardimcisi.OnayliMi).ToList();
        var anaTeklifId = talep.OnaylananTeklifId;
        var anaTeklif = anaTeklifId is { } tid
            ? talep.Teklifler.FirstOrDefault(t => t.Id == tid)
            : null;
        var firmaSayisi = onayliKalemler
            .SelectMany(KalemFirmaAtamaYardimcisi.OnayliTeklifIdleri)
            .Distinct()
            .Count();
        var firmaAdi = firmaSayisi == 1 ? anaTeklif?.FirmaAdi : null;

        await SatinalmaBildirimleri.OnaylandiBildirimleriGonderAsync(talep, firmaAdi);
    }

    public static void SiparisVer(SatinalmaTalep talep)
    {
        if (talep.Durum != SatinalmaTalepDurumlari.Onaylandi)
            throw new InvalidOperationException("Yalnızca onaylanmış talepler için sipariş verilebilir.");

        talep.FirmaSiparisNolari ??= [];
        foreach (var teklifId in talep.Kalemler
                     .Where(KalemFirmaAtamaYardimcisi.OnayliMi)
                     .SelectMany(KalemFirmaAtamaYardimcisi.OnayliTeklifIdleri)
                     .Distinct())
        {
            SatinalmaDepo.SiparisNoAl(talep, teklifId);
        }

        if (string.IsNullOrWhiteSpace(talep.SiparisNo) && talep.OnaylananTeklifId is { } anaId)
            talep.SiparisNo = SatinalmaDepo.SiparisNoAl(talep, anaId);

        talep.Durum = SatinalmaTalepDurumlari.SiparisOlusturuldu;
        talep.Status = ProcurementStatus.Ordered;
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

        if (talep.Durum == SatinalmaTalepDurumlari.SiparisOlusturuldu)
            throw new InvalidOperationException("Sipariş verilmiş taleplerde onay geri alınamaz.");

        if (!talep.HerhangiKalemOnayli
            && !talep.YonetimOnayKilitli
            && talep.Durum != SatinalmaTalepDurumlari.Onaylandi)
            throw new InvalidOperationException("Geri alınacak onay bulunamadı.");

        talep.Kalemler ??= [];
        talep.Teklifler ??= [];

        foreach (var kalem in talep.Kalemler)
            KalemFirmaAtamaYardimcisi.Temizle(kalem);

        foreach (var teklif in talep.Teklifler)
            teklif.Onaylandi = false;

        talep.OnaylananTeklifId = null;
        talep.FirmaSiparisNolari?.Clear();
        talep.SiparisNo = "";
        talep.TeklifsizYonetimOnayi = false;
        talep.YonetimOnayKilitli = false;
        talep.YonetimOnaylayanUid = "";
        talep.YonetimOnaylayanAd = "";
        talep.YonetimOnaylayanEposta = "";
        talep.YonetimOnayTarihi = "";

        if ((talep.Teklifler?.Count ?? 0) > 0)
        {
            talep.Durum = SatinalmaTalepDurumlari.Karsilastirma;
            talep.Status = ProcurementStatus.Comparison;
        }
        else
        {
            talep.Durum = SatinalmaTalepDurumlari.TeklifGirisi;
            talep.Status = ProcurementStatus.QuoteEntry;
        }

        SatinalmaTalepYardimcisi.Dokun(talep);
        SatinalmaDepo.Kaydet();
    }

    public static async Task FirmaOnaylariniGeriAlAsync(SatinalmaTalep talep)
    {
        FirmaOnaylariniGeriAl(talep);
        await SatinalmaKayitYardimcisi.BulutaHemenGonderAsync();
    }

    /// <returns>Sahaya direkt ise çıkış fişi satırı; aksi halde null.</returns>
    public static SahayaCikisSatiri? MalKabulVeDepoyaKaydet(
        OnaylananMalzemeSatiri satir,
        double miktar,
        string kategori,
        string tarih,
        string fisNo,
        string teslimAlan,
        string depoSaha,
        string? aciklama = null,
        string? firma = null,
        decimal? birimFiyat = null,
        bool sahayaDirekt = false,
        string? sahaHedef = null)
    {
        if (!KullaniciYetkileri.MalKabulVeStokAktarYapabilir())
            throw new InvalidOperationException("Mal kabul işlemi yalnızca Satınalma rolü tarafından yapılabilir.");

        if (miktar <= 0)
            throw new InvalidOperationException("Miktar sıfırdan büyük olmalıdır.");

        var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == satir.TalepId)
            ?? throw new InvalidOperationException("Talep bulunamadı.");

        var kalem = talep.Kalemler.FirstOrDefault(k => k.Id == satir.KalemId)
            ?? throw new InvalidOperationException("Kalem bulunamadı.");

        var teklifId = satir.TeklifId != Guid.Empty
            ? satir.TeklifId
            : kalem.OnaylananTeklifId
              ?? throw new InvalidOperationException("Firma ataması bulunamadı.");

        KalemFirmaAtamaYardimcisi.KabulEkle(kalem, teklifId, miktar);

        var atama = KalemFirmaAtamaYardimcisi.EtkinAtamalar(kalem)
            .FirstOrDefault(a => a.TeklifId == teklifId);
        if (atama is not null)
        {
            satir.SiparisMiktari = atama.Miktar;
            satir.KabulEdilenMiktar = atama.KabulEdilenMiktar;
            satir.SiparisTamamlandi = atama.SiparisTamamlandi;
        }
        else
        {
            satir.KabulEdilenMiktar = kalem.KabulEdilenMiktar;
            satir.SiparisTamamlandi = kalem.SiparisTamamlandi;
        }

        MalzemeKategoriDeposu.Ekle(kategori);

        var indirildigiSaha = sahayaDirekt && !string.IsNullOrWhiteSpace(sahaHedef)
            ? sahaHedef.Trim()
            : depoSaha;

        var kayit = satir.AlinanMalzemeKaydinaDonustur(
            miktar, kategori, tarih, fisNo, teslimAlan, indirildigiSaha, aciklama, firma, birimFiyat);
        ModulVeriDeposu.AlinanMalzemeler.Add(kayit);

        var cikisSatiri = AlinanMalzemeAktarimServisi.StogaGirisKaydet(
            satir, miktar, kategori, tarih, teslimAlan, depoSaha, sahayaDirekt, sahaHedef);

        // CollectionChanged yetki kapısından geçmese bile yerel dosyayı zorla yaz.
        ModulVeriDeposu.KaydetAlinanMalzemeler();
        ModulVeriDeposu.KaydetStok();
        ModulVeriDeposu.KaydetStokHareketleri();

        SatinalmaTalepYardimcisi.Dokun(talep);
        SatinalmaDepo.Kaydet();
        _ = SatinalmaKayitYardimcisi.MalKabulSonrasiBulutaGonderAsync();

        try
        {
            _ = SatinalmaBildirimleri.MalKabulEdildiAsync(talep, $"{satir.Malzeme} · {miktar:N2} {satir.Birim}");
        }
        catch
        {
            // bildirim hatası mal kabulü engellemez
        }

        // Tüm kalemler tamamlandıysa eski sipariş/mal kabul bildirimlerini düşür.
        if (ProcurementTalepAdapter.ResolveStatus(talep)
                .Equals(ProcurementStatus.Completed, StringComparison.OrdinalIgnoreCase))
            _ = BildirimYoneticisi.GecersizleriOkunduYapAsync();

        return cikisSatiri;
    }

    /// <summary>
    /// Teklifte mal kabulü bekleyen tüm kalemleri kalan sipariş miktarlarıyla tek fiş altında kabul eder.
    /// Her kalemin kendi onaylı teklifindeki firma ve birim fiyat kullanılır.
    /// </summary>
    public static (int KabulEdilenKalemSayisi, List<SahayaCikisSatiri> SahayaCikisSatirlari) TumKalemleriMalKabulVeDepoyaKaydet(
        Guid talepId,
        Guid teklifId,
        string kategori,
        string tarih,
        string fisNo,
        string teslimAlan,
        string depoSaha,
        string? aciklama = null,
        bool sahayaDirekt = false,
        string? sahaHedef = null)
    {
        if (!KullaniciYetkileri.MalKabulVeStokAktarYapabilir())
            throw new InvalidOperationException("Mal kabul işlemi yalnızca Satınalma rolü tarafından yapılabilir.");

        var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == talepId)
            ?? throw new InvalidOperationException("Talep bulunamadı.");

        var satirlar = SatinalmaDepo.OnaylananMalzemeleriOlustur()
            .Where(s => s.TalepId == talep.Id)
            .Where(s => s.TeklifId == teklifId)
            .Where(s => !s.SiparisTamamlandi && s.KalanMiktar > 0.0001)
            .ToList();

        if (satirlar.Count == 0)
            throw new InvalidOperationException("Mal kabulü bekleyen kalem bulunamadı.");

        var indirildigiSaha = sahayaDirekt && !string.IsNullOrWhiteSpace(sahaHedef)
            ? sahaHedef.Trim()
            : depoSaha;
        var ortakCikisBelge = sahayaDirekt && !string.IsNullOrWhiteSpace(sahaHedef)
            ? StokBelgeNoUretici.SonrakiCikisBelgeNo()
            : null;
        var cikisSatirlari = new List<SahayaCikisSatiri>();
        var kabulSayisi = 0;

        foreach (var satir in satirlar)
        {
            var kalem = talep.Kalemler.FirstOrDefault(k => k.Id == satir.KalemId)
                ?? throw new InvalidOperationException("Kalem bulunamadı.");

            var hedefTeklifId = satir.TeklifId != Guid.Empty
                ? satir.TeklifId
                : teklifId;
            var atama = KalemFirmaAtamaYardimcisi.EtkinAtamalar(kalem)
                .FirstOrDefault(a => a.TeklifId == hedefTeklifId);
            var miktar = atama is not null
                ? Math.Max(0, atama.Miktar - atama.KabulEdilenMiktar)
                : Math.Max(0, kalem.Miktar - kalem.KabulEdilenMiktar);
            if (miktar <= 0.0001)
                continue;

            if (atama is not null)
                KalemFirmaAtamaYardimcisi.KabulEkle(kalem, hedefTeklifId, miktar);
            else
            {
                kalem.KabulEdilenMiktar += miktar;
                kalem.SiparisTamamlandi = kalem.KabulEdilenMiktar >= kalem.Miktar - 0.0001;
            }

            var guncelAtama = KalemFirmaAtamaYardimcisi.EtkinAtamalar(kalem)
                .FirstOrDefault(a => a.TeklifId == hedefTeklifId);
            if (guncelAtama is not null)
            {
                satir.SiparisMiktari = guncelAtama.Miktar;
                satir.KabulEdilenMiktar = guncelAtama.KabulEdilenMiktar;
                satir.SiparisTamamlandi = guncelAtama.SiparisTamamlandi;
            }
            else
            {
                satir.KabulEdilenMiktar = kalem.KabulEdilenMiktar;
                satir.SiparisTamamlandi = kalem.SiparisTamamlandi;
            }

            MalzemeKategoriDeposu.Ekle(kategori);

            var kayit = satir.AlinanMalzemeKaydinaDonustur(
                miktar, kategori, tarih, fisNo, teslimAlan, indirildigiSaha, aciklama,
                satir.Firma, satir.BirimFiyati);
            ModulVeriDeposu.AlinanMalzemeler.Add(kayit);

            var cikis = AlinanMalzemeAktarimServisi.StogaGirisKaydet(
                satir, miktar, kategori, tarih, teslimAlan, depoSaha,
                sahayaDirekt, sahaHedef, ortakCikisBelge);
            if (cikis is not null)
                cikisSatirlari.Add(cikis);
            kabulSayisi++;
        }

        ModulVeriDeposu.KaydetAlinanMalzemeler();
        ModulVeriDeposu.KaydetStok();
        ModulVeriDeposu.KaydetStokHareketleri();

        SatinalmaTalepYardimcisi.Dokun(talep);
        SatinalmaDepo.Kaydet();
        _ = SatinalmaKayitYardimcisi.MalKabulSonrasiBulutaGonderAsync();

        try
        {
            _ = SatinalmaBildirimleri.MalKabulEdildiAsync(
                talep, $"{kabulSayisi} kalemin tamamı kabul edildi");
        }
        catch
        {
            // bildirim hatası mal kabulü engellemez
        }

        if (ProcurementTalepAdapter.ResolveStatus(talep)
                .Equals(ProcurementStatus.Completed, StringComparison.OrdinalIgnoreCase))
            _ = BildirimYoneticisi.GecersizleriOkunduYapAsync();

        return (kabulSayisi, cikisSatirlari);
    }

    public static void SevkiyatiTamamla(OnaylananMalzemeSatiri satir)
    {
        if (!KullaniciYetkileri.MalKabulVeStokAktarYapabilir())
            throw new InvalidOperationException("Bu işlem yalnızca Satınalma rolü tarafından yapılabilir.");

        var talep = SatinalmaDepo.Talepler.FirstOrDefault(t => t.Id == satir.TalepId)
            ?? throw new InvalidOperationException("Talep bulunamadı.");

        var kalem = talep.Kalemler.FirstOrDefault(k => k.Id == satir.KalemId)
            ?? throw new InvalidOperationException("Kalem bulunamadı.");

        if (satir.SiparisTamamlandi || kalem.SiparisTamamlandi)
            throw new InvalidOperationException("Bu kalemin sevkiyatı zaten tamamlanmış.");

        var teklifId = satir.TeklifId != Guid.Empty
            ? satir.TeklifId
            : kalem.OnaylananTeklifId
              ?? Guid.Empty;

        if (teklifId != Guid.Empty && KalemFirmaAtamaYardimcisi.EtkinAtamalar(kalem).Count > 0)
        {
            var atama = KalemFirmaAtamaYardimcisi.EtkinAtamalar(kalem)
                .FirstOrDefault(a => a.TeklifId == teklifId)
                ?? throw new InvalidOperationException("Firma ataması bulunamadı.");

            if (atama.SiparisTamamlandi)
                throw new InvalidOperationException("Bu firmanın sevkiyatı zaten tamamlanmış.");

            if (atama.KabulEdilenMiktar <= 0)
                throw new InvalidOperationException("Sevkiyatı tamamlamak için en az bir mal kabul kaydı olmalıdır.");

            var oncekiMiktar = kalem.Miktar;
            KalemFirmaAtamaYardimcisi.SevkiyatiTamamla(kalem, teklifId);
            satir.SiparisTamamlandi = true;
            satir.SiparisMiktari = KalemFirmaAtamaYardimcisi.EtkinAtamalar(kalem)
                .First(a => a.TeklifId == teklifId).Miktar;

            if (Math.Abs(oncekiMiktar - kalem.Miktar) > 0.0001)
            {
                talep.Teklifler ??= [];
                foreach (var teklif in talep.Teklifler)
                    teklif.FiyatlariHesapla(talep.Kalemler);
            }
        }
        else
        {
            if (kalem.KabulEdilenMiktar <= 0)
                throw new InvalidOperationException("Sevkiyatı tamamlamak için en az bir mal kabul kaydı olmalıdır.");

            var gercekMiktar = kalem.KabulEdilenMiktar;
            if (gercekMiktar < kalem.Miktar - 0.0001)
                KalemMiktariniGercekleseneGoreAyarla(talep, kalem, gercekMiktar, satir);

            kalem.SiparisTamamlandi = true;
            satir.SiparisTamamlandi = true;
            satir.SiparisMiktari = gercekMiktar;
        }

        SatinalmaTalepYardimcisi.Dokun(talep);
        SatinalmaDepo.Kaydet();
        _ = SatinalmaKayitYardimcisi.BulutaHemenGonderAsync();

        if (ProcurementTalepAdapter.ResolveStatus(talep)
                .Equals(ProcurementStatus.Completed, StringComparison.OrdinalIgnoreCase))
            _ = BildirimYoneticisi.GecersizleriOkunduYapAsync();
    }

    private static void KalemMiktariniGercekleseneGoreAyarla(
        SatinalmaTalep talep,
        SatinalmaTalepKalemi kalem,
        double yeniMiktar,
        OnaylananMalzemeSatiri? satir = null)
    {
        if (Math.Abs(kalem.Miktar - yeniMiktar) < 0.0001)
            return;

        kalem.Miktar = yeniMiktar;

        talep.Teklifler ??= [];
        foreach (var teklif in talep.Teklifler)
            teklif.FiyatlariHesapla(talep.Kalemler);

        if (satir is not null)
            satir.SiparisMiktari = yeniMiktar;
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

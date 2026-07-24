using System.Globalization;
using SatinalmaPro.Helpers;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class FinansmanRaporlamaServisi
{
  private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

  public static void VerileriYukle()
  {
    RaporlamaServisi.VerileriYukle();
    FinansmanVeriDeposu.Yukle();
  }

  public static List<string> TumSahalar()
  {
    VerileriYukle();
    return GiderSatirlari(new FinansmanFiltreleri())
        .Select(s => s.Saha)
        .Concat(GelirSatirlari(new FinansmanFiltreleri()).Select(s => s.Saha))
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .Distinct(StringComparer.CurrentCultureIgnoreCase)
        .OrderBy(s => s, StringComparer.CurrentCultureIgnoreCase)
        .ToList();
  }

  public static FinansmanGenelOzet GenelOzet(FinansmanFiltreleri filtre)
  {
    var giderler = GiderSatirlari(filtre);
    var gelirler = GelirSatirlari(filtre);
    var vadeler = VadeSatirlari(filtre);
    var bugun = DateTime.Today;

    return new FinansmanGenelOzet
    {
      ToplamGider = giderler.Sum(g => g.Tutar),
      ToplamGelir = gelirler.Sum(g => g.Tutar),
      BekleyenOdeme = vadeler.Where(v => !v.Odendi).Sum(v => v.KdvDahilTutar > 0 ? v.KdvDahilTutar : v.Tutar),
      GecikenOdeme = vadeler.Where(v => !v.Odendi && v.KalanGun < 0)
          .Sum(v => v.KdvDahilTutar > 0 ? v.KdvDahilTutar : v.Tutar),
      KdvToplam = vadeler.Sum(v => Math.Max(0, v.KdvDahilTutar - v.Tutar)),
      GiderKayitSayisi = giderler.Count,
      GelirKayitSayisi = gelirler.Count,
      VadeKayitSayisi = vadeler.Count(v => !v.Odendi)
    };
  }

  public static List<FinansmanModulOzeti> ModulOzetleri(FinansmanFiltreleri filtre)
  {
    var raporFiltre = RaporFiltresineDonustur(filtre);
    var giderOzet = RaporlamaServisi.ModulOzetleri(raporFiltre)
        .Select(o => new FinansmanModulOzeti
        {
          ModulAdi = o.ModulAdi,
          KayitSayisi = o.KayitSayisi,
          ToplamTutar = o.ToplamTutar,
          Renk = o.Renk,
          Tip = FinansmanHareketTipleri.Gider
        })
        .ToList();

    var gelirler = GelirSatirlari(filtre);
    if (gelirler.Count > 0 && ModulSecili(filtre.Modul, FinansmanModulleri.Gelir, FinansmanModulleri.Tumu))
    {
      giderOzet.Add(new FinansmanModulOzeti
      {
        ModulAdi = FinansmanModulleri.Gelir,
        KayitSayisi = gelirler.Count,
        ToplamTutar = gelirler.Sum(g => g.Tutar),
        Renk = "#22C55E",
        Tip = FinansmanHareketTipleri.Gelir
      });
    }

    return giderOzet.Where(o => o.KayitSayisi > 0 || o.ToplamTutar > 0).ToList();
  }

  public static List<FinansmanHareketSatiri> GiderSatirlari(FinansmanFiltreleri filtre)
  {
    if (filtre.HareketTipi == FinansmanHareketTipleri.Gelir)
      return [];

    if (filtre.Modul == FinansmanModulleri.Gelir)
      return [];

    var raporFiltre = RaporFiltresineDonustur(filtre);
    return RaporlamaServisi.DetaySatirlari(raporFiltre)
        .Where(s => SahaUygunMu(s.Saha, filtre.Saha))
        .Select(s => new FinansmanHareketSatiri
        {
          Tip = FinansmanHareketTipleri.Gider,
          Tarih = s.Tarih,
          Modul = s.Modul,
          BelgeNo = s.BelgeNo,
          Kategori = s.Kategori,
          Aciklama = s.Aciklama,
          Tedarikci = s.Tedarikci,
          Saha = s.Saha,
          Tutar = s.Tutar
        })
        .OrderByDescending(s => TarihCoz(s.Tarih))
        .ThenBy(s => s.Aciklama)
        .ToList();
  }

  public static List<FinansmanHareketSatiri> GelirSatirlari(FinansmanFiltreleri filtre)
  {
    if (filtre.HareketTipi == FinansmanHareketTipleri.Gider)
      return [];

    if (filtre.Modul != FinansmanModulleri.Tumu && filtre.Modul != FinansmanModulleri.Gelir)
      return [];

    VerileriYukle();

    return FinansmanVeriDeposu.Gelirler
        .Where(k => TarihUygunMu(k.Tarih, filtre.Baslangic, filtre.Bitis))
        .Where(k => SahaUygunMu(k.Saha, filtre.Saha))
        .Select(k => new FinansmanHareketSatiri
        {
          Tip = FinansmanHareketTipleri.Gelir,
          Tarih = k.Tarih,
          Modul = FinansmanModulleri.Gelir,
          BelgeNo = k.BelgeNo,
          Kategori = k.Kategori,
          Aciklama = string.IsNullOrWhiteSpace(k.Aciklama) ? k.Kaynak : k.Aciklama,
          Tedarikci = k.Kaynak,
          Saha = k.Saha,
          Tutar = k.Tutar,
          OdemeSekli = k.OdemeSekli
        })
        .OrderByDescending(s => TarihCoz(s.Tarih))
        .ThenBy(s => s.Aciklama)
        .ToList();
  }

  public static List<FinansmanHareketSatiri> TumHareketler(FinansmanFiltreleri filtre) =>
      GiderSatirlari(filtre)
          .Concat(GelirSatirlari(filtre))
          .OrderByDescending(s => TarihCoz(s.Tarih))
          .ThenBy(s => s.Tip)
          .ToList();

  public static List<FinansmanAylikOzet> AylikOzetler(FinansmanFiltreleri filtre)
  {
    var giderler = GiderSatirlari(filtre);
    var gelirler = GelirSatirlari(filtre);

    var aylar = giderler.Select(g => (Tarih: TarihCoz(g.Tarih), Tutar: g.Tutar, Tip: FinansmanHareketTipleri.Gider))
        .Concat(gelirler.Select(g => (Tarih: TarihCoz(g.Tarih), Tutar: g.Tutar, Tip: FinansmanHareketTipleri.Gelir)))
        .Where(x => x.Tarih != DateTime.MinValue)
        .GroupBy(x => new { x.Tarih.Year, x.Tarih.Month })
        .Select(g => new FinansmanAylikOzet
        {
          Yil = g.Key.Year,
          AyNo = g.Key.Month,
          Ay = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", Tr),
          Gider = g.Where(x => x.Tip == FinansmanHareketTipleri.Gider).Sum(x => x.Tutar),
          Gelir = g.Where(x => x.Tip == FinansmanHareketTipleri.Gelir).Sum(x => x.Tutar),
          HareketSayisi = g.Count()
        })
        .OrderBy(a => a.Yil)
        .ThenBy(a => a.AyNo)
        .ToList();

    return aylar;
  }

  public static List<FinansmanVadeSatiri> VadeSatirlari(FinansmanFiltreleri filtre)
  {
    VerileriYukle();
    var bugun = DateTime.Today;
    var liste = new List<FinansmanVadeSatiri>();

    foreach (var satir in SatinalmaDepo.OnaylananMalzemeleriOlustur())
    {
      if (satir.SiparisTamamlandi || satir.KabulEdilenMiktar >= satir.SiparisMiktari)
        continue;

      var islemTarihi = TarihCoz(satir.Tarih);
      if (islemTarihi == DateTime.MinValue)
        continue;

      if (!TarihUygunMu(satir.Tarih, filtre.Baslangic, filtre.Bitis))
        continue;

      var vadeTarihi = islemTarihi.AddDays(satir.VadeGunu);
      var kalanGun = (vadeTarihi - bugun).Days;
      var kalanTutar = satir.ToplamTutar - satir.KabulToplamTutar;
      if (kalanTutar <= 0)
        continue;

      var kdvOrani = 0.20m;
      var kdvDahil = Math.Round(kalanTutar * (1 + kdvOrani), 2);

      liste.Add(new FinansmanVadeSatiri
      {
        IslemTarihi = satir.Tarih,
        VadeTarihi = vadeTarihi.ToString("dd.MM.yyyy", Tr),
        Firma = satir.Firma,
        BelgeNo = string.IsNullOrWhiteSpace(satir.SiparisNo) ? satir.TalepNo : satir.SiparisNo,
        Aciklama = satir.Malzeme,
        VadeGunu = satir.VadeGunu,
        Tutar = kalanTutar,
        KdvDahilTutar = kdvDahil,
        KalanGun = kalanGun,
        Odendi = false,
        Durum = kalanGun < 0 ? "Gecikmiş" : kalanGun <= 7 ? "Yaklaşan" : "Bekliyor"
      });
    }

    return liste
        .OrderBy(v => TarihCoz(v.VadeTarihi))
        .ThenBy(v => v.Firma)
        .ToList();
  }

  public static List<FinansmanVadeSatiri> BekleyenOdemeler(FinansmanFiltreleri filtre) =>
      VadeSatirlari(filtre).Where(v => !v.Odendi).ToList();

  public static List<FinansmanGrupOzeti> SahaOzeti(FinansmanFiltreleri filtre) =>
      GrupOzeti(filtre, s => string.IsNullOrWhiteSpace(s.Saha) ? "Belirtilmemiş" : s.Saha);

  public static List<FinansmanGrupOzeti> TedarikciOzeti(FinansmanFiltreleri filtre)
  {
    var giderler = GiderSatirlari(filtre);
    return giderler
        .GroupBy(s => string.IsNullOrWhiteSpace(s.Tedarikci) ? "Belirtilmemiş" : s.Tedarikci,
            StringComparer.CurrentCultureIgnoreCase)
        .Select(g => new FinansmanGrupOzeti
        {
          GrupAdi = g.Key,
          KayitSayisi = g.Count(),
          GiderTutar = g.Sum(x => x.Tutar),
          ToplamTutar = g.Sum(x => x.Tutar),
          ModulDagilimi = string.Join(", ",
              g.GroupBy(x => x.Modul).Select(m => $"{m.Key}: ₺{m.Sum(x => x.Tutar):N0}"))
        })
        .OrderByDescending(g => g.ToplamTutar)
        .ToList();
  }

  public static string FiltreOzetiMetni(FinansmanFiltreleri filtre)
  {
    var parcalar = new List<string> { filtre.RaporTuru };

    if (filtre.Baslangic is DateTime b)
      parcalar.Add($"Başlangıç: {b:dd.MM.yyyy}");
    if (filtre.Bitis is DateTime e)
      parcalar.Add($"Bitiş: {e:dd.MM.yyyy}");
    if (filtre.Modul != FinansmanModulleri.Tumu)
      parcalar.Add($"Modül: {filtre.Modul}");
    if (filtre.HareketTipi != FinansmanHareketTipleri.Tumu)
      parcalar.Add($"Hareket: {filtre.HareketTipi}");
    if (filtre.Saha != "Tümü" && !string.IsNullOrWhiteSpace(filtre.Saha))
      parcalar.Add($"Saha: {filtre.Saha}");

    return string.Join(" · ", parcalar);
  }

  private static List<FinansmanGrupOzeti> GrupOzeti(
      FinansmanFiltreleri filtre,
      Func<FinansmanHareketSatiri, string> anahtar)
  {
    var giderler = GiderSatirlari(filtre);
    var gelirler = GelirSatirlari(filtre);

    var gruplar = giderler
        .GroupBy(s => anahtar(s), StringComparer.CurrentCultureIgnoreCase)
        .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.CurrentCultureIgnoreCase);

    foreach (var g in gelirler.GroupBy(s => anahtar(s), StringComparer.CurrentCultureIgnoreCase))
    {
      if (!gruplar.ContainsKey(g.Key))
        gruplar[g.Key] = [];
      gruplar[g.Key].AddRange(g);
    }

    return gruplar
        .Select(kv => new FinansmanGrupOzeti
        {
          GrupAdi = kv.Key,
          KayitSayisi = kv.Value.Count,
          GiderTutar = kv.Value.Where(x => x.Tip == FinansmanHareketTipleri.Gider).Sum(x => x.Tutar),
          GelirTutar = kv.Value.Where(x => x.Tip == FinansmanHareketTipleri.Gelir).Sum(x => x.Tutar),
          ToplamTutar = kv.Value.Where(x => x.Tip == FinansmanHareketTipleri.Gider).Sum(x => x.Tutar),
          ModulDagilimi = string.Join(", ",
              kv.Value.GroupBy(x => x.Modul).Select(m => $"{m.Key}: ₺{m.Sum(x => x.Tutar):N0}"))
        })
        .OrderByDescending(g => g.GiderTutar + g.GelirTutar)
        .ToList();
  }

  private static RaporFiltreleri RaporFiltresineDonustur(FinansmanFiltreleri filtre)
  {
    var modul = filtre.Modul switch
    {
      FinansmanModulleri.Gelir => RaporModulleri.Tumu,
      _ => filtre.Modul == FinansmanModulleri.Tumu ? RaporModulleri.Tumu : filtre.Modul
    };

    return new RaporFiltreleri
    {
      Baslangic = filtre.Baslangic,
      Bitis = filtre.Bitis,
      Modul = modul,
      RaporTuru = RaporTurleri.DetayliHareketler
    };
  }

  private static bool ModulSecili(string secili, string modul, string tumu) =>
      secili == tumu || secili.Equals(modul, StringComparison.OrdinalIgnoreCase);

  private static bool SahaUygunMu(string saha, string filtreSaha) =>
      filtreSaha == "Tümü" || string.IsNullOrWhiteSpace(filtreSaha) ||
      saha.Equals(filtreSaha, StringComparison.CurrentCultureIgnoreCase);

  private static bool TarihUygunMu(string tarih, DateTime? baslangic, DateTime? bitis) =>
      TarihYardimcisi.Aralikta(tarih, baslangic, bitis);

  private static DateTime TarihCoz(string tarih) =>
      TarihYardimcisi.SiralamaDegeri(tarih);
}

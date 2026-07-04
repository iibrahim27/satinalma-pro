using System.IO;
using SatinalmaPro.Models;

namespace SatinalmaPro.Services;

public static class SatinalmaProVeriKatalogu
{
    public static IReadOnlyList<VeriKaydiTanimi> TumKayitlar { get; } =
    [
        new()
        {
            Kategori = "Genel",
            ModulAdi = "Uygulama Ayarları",
            DosyaAdi = "uygulama_ayarlar.json",
            Aciklama = "Firma adı, firma logosu ve anasayfa logosu"
        },
        new()
        {
            Kategori = "Satınalma",
            ModulAdi = "Satınalma Ayarları",
            DosyaAdi = "satinalma_ayarlar.json",
            Aciklama = "Şartname, imzalar, sıra numaraları"
        },
        new()
        {
            Kategori = "Satınalma",
            ModulAdi = "Satınalma Talepleri",
            DosyaAdi = "satinalma_talepler.json",
            Aciklama = "Talepler, teklifler, onaylar"
        },
        new()
        {
            Kategori = "Malzeme",
            ModulAdi = "Alınan Malzemeler",
            DosyaAdi = "alinan_malzemeler.json",
            Aciklama = "Malzeme giriş kayıtları"
        },
        new()
        {
            Kategori = "Stok",
            ModulAdi = "Stok Hareketleri",
            DosyaAdi = "stok_hareketleri.json",
            Aciklama = "Stok giriş, çıkış ve sayım hareketleri"
        },
        new()
        {
            Kategori = "Stok",
            ModulAdi = "Stok Yönetimi",
            DosyaAdi = "stok.json",
            Aciklama = "Depo stok kalemleri ve miktarları"
        },
        new()
        {
            Kategori = "Malzeme",
            ModulAdi = "Agrega",
            DosyaAdi = "agrega.json",
            Aciklama = "Agrega hareket kayıtları"
        },
        new()
        {
            Kategori = "Malzeme",
            ModulAdi = "Çimento",
            DosyaAdi = "cimento.json",
            Aciklama = "Çimento giriş kayıtları"
        },
        new()
        {
            Kategori = "Operasyon",
            ModulAdi = "Akaryakıt",
            DosyaAdi = "akaryakit.json",
            Aciklama = "Yakıt alım ve dağıtım kayıtları"
        },
        new()
        {
            Kategori = "Operasyon",
            ModulAdi = "Araç Filo",
            DosyaAdi = "filo.json",
            Aciklama = "Araç, bakım ve zimmet kayıtları"
        },
        new()
        {
            Kategori = "Finans",
            ModulAdi = "Finansman Gelirleri",
            DosyaAdi = "finansman_gelir.json",
            Aciklama = "Finansman modülü gelir kayıtları"
        }
    ];

    public static List<VeriKaydiDurumu> DurumlariOlustur()
    {
        var liste = new List<VeriKaydiDurumu>();
        SatinalmaProKlasor.Olustur();

        foreach (var tanim in TumKayitlar)
        {
            var yol = SatinalmaProKlasor.DosyaYolu(tanim.DosyaAdi);
            var durum = new VeriKaydiDurumu
            {
                ModulAdi = tanim.ModulAdi,
                DosyaAdi = tanim.DosyaAdi,
                Kategori = tanim.Kategori,
                Aciklama = tanim.Aciklama,
                Durum = File.Exists(yol) ? "Kayıtlı" : "Boş"
            };

            if (File.Exists(yol))
            {
                var info = new FileInfo(yol);
                durum.Boyut = info.Length < 1024
                    ? $"{info.Length} B"
                    : info.Length < 1024 * 1024
                        ? $"{info.Length / 1024.0:F1} KB"
                        : $"{info.Length / (1024.0 * 1024.0):F1} MB";
                durum.SonGuncelleme = info.LastWriteTime.ToString("dd.MM.yyyy HH:mm");
            }

            liste.Add(durum);
        }

        return liste;
    }
}

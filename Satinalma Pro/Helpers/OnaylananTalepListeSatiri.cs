using System.Windows.Media;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Helpers;

public class OnaylananTalepListeSatiri
{
    private static readonly SolidColorBrush BenimKartArka = new(Color.FromRgb(0xDB, 0xEA, 0xFE));
    private static readonly SolidColorBrush BenimKartKenar = new(Color.FromRgb(0x3B, 0x82, 0xF6));
    private static readonly SolidColorBrush DigerKartArka = new(Color.FromRgb(0xF0, 0xFD, 0xF4));
    private static readonly SolidColorBrush DigerKartKenar = new(Color.FromRgb(0x86, 0xEF, 0xAC));

    public OnaylananTalepListeSatiri(
        SatinalmaTalep talep,
        string? kullaniciUid = null,
        string? kullaniciAd = null,
        Func<Guid, Guid, bool>? stokAktarildiMi = null)
    {
        Talep = talep;
        BenimTalebim = SatinalmaTalepKuyrugu.KullanicininTalebi(talep, kullaniciUid, kullaniciAd);
        DurumEtiketi = SatinalmaTalepDurumEtiketiMasaustu.Olustur(talep, stokAktarildiMi);
        var (_, _, rozetArka, rozetYazi) = TalepDurumRenkleri.Fircalar(DurumEtiketi);

        KartArkaPlan = BenimTalebim ? BenimKartArka : DigerKartArka;
        KartKenar = BenimTalebim ? BenimKartKenar : DigerKartKenar;
        RozetArkaPlan = rozetArka;
        RozetYazi = rozetYazi;

        var firmalar = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        talep.Teklifler ??= [];
        foreach (var kalem in talep.Kalemler ?? [])
        {
            if (kalem.OnaylananTeklifId is not { } teklifId)
                continue;

            var teklif = talep.Teklifler.FirstOrDefault(t => t.Id == teklifId);
            if (!string.IsNullOrWhiteSpace(teklif?.FirmaAdi))
                firmalar.Add(teklif.FirmaAdi);
        }

        var firmaListesi = firmalar.ToList();
        OnayliFirmaOzet = firmaListesi.Count == 0
            ? "Firma henüz atanmadı"
            : firmaListesi.Count == 1
                ? firmaListesi[0]
                : $"{firmaListesi[0]} (+{firmaListesi.Count - 1})";
    }

    public SatinalmaTalep Talep { get; }
    public bool BenimTalebim { get; }
    public string TalepNo => Talep.TalepNo;
    public string Tarih => Talep.Tarih;
    public string TalepEden => Talep.TalepEden;
    public string DurumEtiketi { get; }
    public string OnayliFirmaOzet { get; }
    public Brush KartArkaPlan { get; }
    public Brush KartKenar { get; }
    public Brush RozetArkaPlan { get; }
    public Brush RozetYazi { get; }
}

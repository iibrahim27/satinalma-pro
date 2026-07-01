using System.Windows.Media;
using SatinalmaPro.Models;
using SatinalmaPro.Shared.Helpers;

namespace SatinalmaPro.Helpers;

public class TalepListeSatiri
{
    private static readonly string[] DuzenlenebilirDurumlar =
    [
        SatinalmaTalepDurumlari.Taslak,
        SatinalmaTalepDurumlari.Hazirlaniyor
    ];

    private static readonly SolidColorBrush BenimKartArka = new(Color.FromRgb(0xDB, 0xEA, 0xFE));
    private static readonly SolidColorBrush BenimKartKenar = new(Color.FromRgb(0x3B, 0x82, 0xF6));

    public TalepListeSatiri(
        SatinalmaTalep talep,
        string? kullaniciUid = null,
        string? kullaniciAd = null,
        Func<Guid, Guid, bool>? stokAktarildiMi = null)
    {
        Talep = talep;
        BenimTalebim = SatinalmaTalepKuyrugu.KullanicininTalebi(talep, kullaniciUid, kullaniciAd);
        DurumEtiketi = SatinalmaTalepDurumEtiketiMasaustu.Olustur(talep, stokAktarildiMi);
        var (_, kenar, rozetArka, rozetYazi) = TalepDurumRenkleri.Fircalar(DurumEtiketi);

        if (BenimTalebim)
        {
            KartArkaPlan = BenimKartArka;
            KartKenar = BenimKartKenar;
        }
        else
        {
            KartKenar = kenar;
            KartArkaPlan = rozetArka;
        }

        RozetArkaPlan = rozetArka;
        RozetYazi = rozetYazi;
    }

    public SatinalmaTalep Talep { get; }
    public bool BenimTalebim { get; }

    public string TalepNo => Talep.TalepNo;
    public string Tarih => Talep.Tarih;
    public string TalepEden => Talep.TalepEden;
    public string KalemSayisiMetni => Talep.KalemSayisiMetni;
    public string DurumEtiketi { get; }
    public Brush KartArkaPlan { get; }
    public Brush KartKenar { get; }
    public Brush RozetArkaPlan { get; }
    public Brush RozetYazi { get; }
    public bool Duzenlenebilir => DuzenlenebilirDurumlar.Contains(Talep.Durum);

    public static string DurumEtiketiOlustur(SatinalmaTalep talep, Func<Guid, Guid, bool>? stokAktarildiMi = null) =>
        SatinalmaTalepDurumEtiketiMasaustu.Olustur(talep, stokAktarildiMi);
}

namespace SatinalmaPro.Shared.Models;



public static class KullaniciRolleri

{

    public const string Admin = "Admin";

    public const string Yonetim = "Yönetim";

    public const string Satinalma = "Satınalma";

    public const string Sef = "Şef";

    public const string Saha = "Saha";

    public const string Atolye = "Atölye";

    public const string Depo = "Depo";

    /// <summary>Eski rol — yeni kayıtlarda kullanılmaz; Normalize ile Şef'e dönüşür.</summary>

    public const string Santiye = "Şantiye";

    public const string Okuma = "Okuma";



    public static IReadOnlyList<string> Tum { get; } =

        [Admin, Yonetim, Satinalma, Sef, Saha, Atolye, Depo];



    public static string Normalize(string? rol)

    {

        if (string.IsNullOrWhiteSpace(rol))

            return Saha;



        var r = rol.Trim();



        if (r.Equals(Okuma, StringComparison.OrdinalIgnoreCase))

            return Saha;



        if (r.Equals(Santiye, StringComparison.OrdinalIgnoreCase)

            || r.Equals("Santiye", StringComparison.OrdinalIgnoreCase))

            return Sef;



        if (r.Equals(Admin, StringComparison.OrdinalIgnoreCase))

            return Admin;



        if (r.Equals(Sef, StringComparison.OrdinalIgnoreCase)

            || r.Equals("Sef", StringComparison.OrdinalIgnoreCase))

            return Sef;



        if (r.Equals(Atolye, StringComparison.OrdinalIgnoreCase)

            || r.Equals("Atolye", StringComparison.OrdinalIgnoreCase))

            return Atolye;



        return Tum.FirstOrDefault(x => x.Equals(r, StringComparison.OrdinalIgnoreCase)) ?? r;

    }



    public static bool AdminMi(string? rol) =>

        !string.IsNullOrWhiteSpace(rol) &&

        rol.Trim().Equals(Admin, StringComparison.OrdinalIgnoreCase);



    public static bool YazabilirMi(string? rol) =>

        Normalize(rol) is Admin or Yonetim or Satinalma or Depo or Sef or Saha or Atolye;



    /// <summary>Talep oluşturabilen roller.</summary>

    public static bool TalepOlusturabilir(string? rol) =>

        Normalize(rol) is Admin or Yonetim or Saha or Sef or Satinalma;



    /// <summary>Onay Bekleyen — teklif kuyruğu olmayan rollerde geniş liste (takip).</summary>
    public static bool OnayBekleyenGenisListe(string? rol) =>
        KendiTalepleriniTakipEder(rol) || SadeceTalepModu(rol);

    /// <summary>Şef/saha — teklif sekmesi yok; onay bekleyen geniş görünüm.</summary>
    public static bool KendiTalepleriniTakipEder(string? rol) =>
        Normalize(rol) is Saha or Sef;

    /// <summary>Yalnızca kendi taleplerini takip eder — teklif/onay yok.</summary>

    public static bool SadeceTalepModu(string? rol) =>

        Normalize(rol) is Saha;



    /// <summary>Şef — talep + onaylanan malzeme takibi.</summary>

    public static bool SefRolu(string? rol) =>

        Normalize(rol) is Sef;



    /// <summary>Masaüstü Satınalma modülü — Android RolNavigasyonu ile aynı.</summary>

    public static IReadOnlyList<string> VarsayilanSatinalmaSekmeler(string? rol) =>

        SatinalmaPro.Shared.Services.MasaustuRolHaritasi.SatinalmaSekmeleri(rol);



    /// <summary>Eski sekme adı (Talepler) ile uyumluluk.</summary>

    public static string SatinalmaSekmeNormalize(string sekmeAdi) =>

        sekmeAdi.Equals("Talepler", StringComparison.OrdinalIgnoreCase) ? "Taleplerim" : sekmeAdi;



    public static bool SatinalmaSekmesiGorebilir(string? rol, string sekmeAdi) =>

        SatinalmaPro.Shared.Services.MasaustuRolHaritasi.SatinalmaSekmesiGorebilir(rol, sekmeAdi);

}



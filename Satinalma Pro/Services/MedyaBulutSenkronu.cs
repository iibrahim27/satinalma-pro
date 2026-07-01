using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using SatinalmaPro.Models;



namespace SatinalmaPro.Services;



public static class MedyaBulutSenkronu

{

    private const string BelgeYolu = "veri/medya";



    private static readonly JsonSerializerOptions Json = new()

    {

        PropertyNamingPolicy = JsonNamingPolicy.CamelCase

    };



    public static async Task SenkronizeEtAsync(CancellationToken iptal = default)

    {

        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)

            return;



        var (bulutJson, _) = await OturumYoneticisi.Firestore.BelgeOkuAsync(BelgeYolu, iptal);

        var yerelPaket = YerelPaketOlustur();



        if (!string.IsNullOrWhiteSpace(bulutJson))

        {

            try

            {

                var paket = JsonSerializer.Deserialize<MedyaPaketi>(bulutJson, Json) ?? new MedyaPaketi();

                if (LogoVar(paket))
                {
                    await UiThreaddeCalistirAsync(() => Uygula(paket));
                    return;
                }

            }

            catch

            {

                // yerel yükleme dene

            }

        }



        if (LogoVar(yerelPaket))

        {

            var json = JsonSerializer.Serialize(yerelPaket, Json);

            await OturumYoneticisi.Firestore.BelgeJsonYazAsync(

                BelgeYolu, json, OturumYoneticisi.Auth?.Uid, iptal);

        }

    }



    public static async Task BulutaYukleAsync(CancellationToken iptal = default)

    {

        if (!OturumYoneticisi.GirisYapildi || OturumYoneticisi.Firestore is null)

            return;



        var paket = YerelPaketOlustur();

        if (!LogoVar(paket))

            return;



        var json = JsonSerializer.Serialize(paket, Json);

        await OturumYoneticisi.Firestore.BelgeJsonYazAsync(

            BelgeYolu, json, OturumYoneticisi.Auth?.Uid, iptal);

    }



    public static void Planla()

    {

        if (!OturumYoneticisi.GirisYapildi)

            return;



        _ = Task.Run(async () =>

        {

            try { await BulutaYukleAsync(); } catch { /* yoksay */ }

        });

    }



    private static MedyaPaketi YerelPaketOlustur()

    {

        var ayarlar = UygulamaAyarDeposu.Ayarlar;

        return new MedyaPaketi

        {

            FirmaLogoDosya = ayarlar.LogoDosyaYolu,

            AnasayfaLogoDosya = ayarlar.AnasayfaLogoDosyaYolu,

            FirmaLogoBase64 = DosyadanBase64(SatinalmaProLogoDeposu.TamYol(ayarlar.LogoDosyaYolu)),

            AnasayfaLogoBase64 = DosyadanBase64(SatinalmaProLogoDeposu.TamYol(ayarlar.AnasayfaLogoDosyaYolu))

        };

    }



    private static void Uygula(MedyaPaketi paket)

    {

        SatinalmaProLogoDeposu.Olustur();

        var degisti = false;



        if (!string.IsNullOrWhiteSpace(paket.FirmaLogoBase64))

        {

            var yol = Base64DenKaydet(paket.FirmaLogoBase64, paket.FirmaLogoDosya, "firma");

            if (!string.IsNullOrWhiteSpace(yol) && UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu != yol)

            {

                UygulamaAyarDeposu.Ayarlar.LogoDosyaYolu = yol;

                degisti = true;

            }

        }



        if (!string.IsNullOrWhiteSpace(paket.AnasayfaLogoBase64))

        {

            var yol = Base64DenKaydet(paket.AnasayfaLogoBase64, paket.AnasayfaLogoDosya, "anasayfa");

            if (!string.IsNullOrWhiteSpace(yol) && UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu != yol)

            {

                UygulamaAyarDeposu.Ayarlar.AnasayfaLogoDosyaYolu = yol;

                degisti = true;

            }

        }



        if (degisti)

            UygulamaAyarDeposu.Kaydet();

    }



    private static string Base64DenKaydet(string base64, string? dosyaAdi, string onEk)

    {

        try

        {

            var bytes = Convert.FromBase64String(base64);

            var uzanti = Path.GetExtension(dosyaAdi ?? "") is { Length: > 0 } ext ? ext : ".png";

            var hedefAd = string.IsNullOrWhiteSpace(dosyaAdi)

                ? $"{onEk}_{DateTime.Now:yyyyMMdd_HHmmss}{uzanti}"

                : Path.GetFileName(dosyaAdi);

            var hedef = Path.Combine(SatinalmaProLogoDeposu.LogolarKlasoru, hedefAd);

            File.WriteAllBytes(hedef, bytes);

            return SatinalmaProLogoDeposu.GoreliYol(hedef);

        }

        catch

        {

            return "";

        }

    }



    private static string? DosyadanBase64(string tamYol)

    {

        if (string.IsNullOrWhiteSpace(tamYol) || !File.Exists(tamYol))

            return null;



        return Convert.ToBase64String(File.ReadAllBytes(tamYol));

    }



    private static bool LogoVar(MedyaPaketi paket) =>
        !string.IsNullOrWhiteSpace(paket.FirmaLogoBase64) ||
        !string.IsNullOrWhiteSpace(paket.AnasayfaLogoBase64);

    private static Task UiThreaddeCalistirAsync(Action islem)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            islem();
            return Task.CompletedTask;
        }

        return dispatcher.InvokeAsync(islem).Task;
    }
}


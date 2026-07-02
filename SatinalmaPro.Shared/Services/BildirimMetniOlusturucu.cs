using SatinalmaPro.Shared.Models;

namespace SatinalmaPro.Shared.Services;

public static class BildirimMetniOlusturucu
{
    public static string Ozet(string? aciklama, IEnumerable<string>? malzemeler, int maxLen = 100)
    {
        if (!string.IsNullOrWhiteSpace(aciklama))
            return Kisalt(aciklama.Trim(), maxLen);

        var list = malzemeler?
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Take(3)
            .ToList();

        if (list is { Count: > 0 })
            return Kisalt(string.Join(", ", list), maxLen);

        return "";
    }

    public static string TalepSatiri(string talepNo, string talepEden, string? aciklama, IEnumerable<string>? malzemeler)
    {
        var ozet = Ozet(aciklama, malzemeler);
        var parcalar = new List<string>();
        if (!string.IsNullOrWhiteSpace(talepNo))
            parcalar.Add(talepNo);
        if (!string.IsNullOrWhiteSpace(talepEden))
            parcalar.Add(talepEden);
        if (!string.IsNullOrWhiteSpace(ozet))
            parcalar.Add(ozet);

        return parcalar.Count > 0 ? string.Join(" · ", parcalar) : talepNo;
    }

    public static (string Baslik, string Mesaj) Olustur(string tip, SatinalmaTalep talep, string? firmaAdi = null, string? ek = null)
    {
        var malzemeler = talep.Kalemler?.OrderBy(k => k.SiraNo).Select(k => k.Malzeme);
        return Olustur(tip, talep.TalepNo, talep.TalepEden, talep.TalepAciklamasi, malzemeler, firmaAdi, ek);
    }

    public static (string Baslik, string Mesaj) Olustur(
        string tip,
        string talepNo,
        string talepEden,
        string? aciklama,
        IEnumerable<string>? malzemeler,
        string? firmaAdi = null,
        string? ek = null)
    {
        var satir = TalepSatiri(talepNo, talepEden, aciklama, malzemeler);

        return tip switch
        {
            BildirimTipleri.YonetimeGonderildi => ($"Yeni talep: {talepNo}", satir),
            BildirimTipleri.TeklifIstendi => ($"Teklif istendi: {talepNo}", satir),
            BildirimTipleri.TeklifOnayda => ($"Teklif onayda: {talepNo}", satir),
            BildirimTipleri.TeklifDuzeltmeIstendi => ($"Teklif düzeltme: {talepNo}",
                string.IsNullOrWhiteSpace(ek) ? satir : TalepSatiri(talepNo, talepEden, ek, null)),
            BildirimTipleri.Reddedildi => ($"Talep reddedildi: {talepNo}",
                string.IsNullOrWhiteSpace(ek) ? satir : TalepSatiri(talepNo, talepEden, ek, null)),
            BildirimTipleri.Onaylandi when !string.IsNullOrWhiteSpace(firmaAdi) =>
                ($"Firma onaylandı: {talepNo}", $"Yönetim {firmaAdi} firmasına onay verdi."),
            BildirimTipleri.Onaylandi => ($"Talep onaylandı: {talepNo}", satir),
            BildirimTipleri.SiparisOlusturuldu => ($"Sipariş verildi: {talepNo}",
                string.IsNullOrWhiteSpace(ek) ? satir : $"{satir} · {ek}"),
            BildirimTipleri.MalKabulEdildi => ($"Mal kabul: {talepNo}",
                string.IsNullOrWhiteSpace(ek) ? satir : $"{satir} · {ek}"),
            _ => ($"Talep: {talepNo}", satir)
        };
    }

    private static string Kisalt(string metin, int maxLen)
    {
        if (metin.Length <= maxLen)
            return metin;

        return metin[..(maxLen - 1)].TrimEnd() + "…";
    }
}

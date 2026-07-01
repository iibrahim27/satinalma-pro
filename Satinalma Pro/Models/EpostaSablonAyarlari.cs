namespace SatinalmaPro.Models;

public class EpostaSablonAyarlari
{
    public string SifreSifirKonu { get; set; } = "Satınalma Pro — Şifre Sıfırlama";
    public string SifreSifirGovde { get; set; } =
        "Merhaba {adSoyad},\n\n" +
        "Satınalma Pro hesabınız ({eposta}) için şifre sıfırlama talebi oluşturuldu.\n\n" +
        "E-postanıza gelen bağlantıya tıklayarak yeni şifrenizi belirleyebilirsiniz.\n\n" +
        "Firma: {firmaAdi}\n\n" +
        "Bu işlemi siz yapmadıysanız yöneticinize bildirin.";
}

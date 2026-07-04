namespace SatinalmaPro.Mobile.Services;

public interface IApkKurulumServisi
{
    /// <summary>Kurulum izni yoksa ayarlara yönlendirir.</summary>
    bool KurulumIznineHazir();

    /// <summary>İndirilen APK dosyasını kurar.</summary>
    void Kur(string apkYol);
}

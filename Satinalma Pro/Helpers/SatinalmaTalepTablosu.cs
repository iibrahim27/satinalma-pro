using System.Collections.ObjectModel;
using SatinalmaPro.Models;
using SatinalmaPro.Services;

namespace SatinalmaPro.Helpers;

/// <summary>Satınalma talep tablosu — sayfalama yok, depodaki tüm kayıtlar.</summary>
public sealed class SatinalmaTalepTablosu
{
    public ObservableCollection<SatinalmaTalep> Satirlar { get; } = [];

    public int DepoToplam { get; private set; }

    public void Yenile(string durumFiltresi, string? arama, bool sadeceTalepModu)
    {
        DepoToplam = SatinalmaDepo.Talepler.Count;

        var sirali = SatinalmaDepo.Talepler
            .Where(t => SatinalmaTalepListesiServisi.Gorunur(t, durumFiltresi, arama, sadeceTalepModu))
            .OrderByDescending(t => ModulSayfalamaYardimcisi.TarihSira(t.Tarih))
            .ThenByDescending(t => t.TalepNo, StringComparer.OrdinalIgnoreCase)
            .ToList();

        Satirlar.Clear();
        foreach (var talep in sirali)
            Satirlar.Add(talep);
    }
}

using System.Windows.Controls;
using SatinalmaPro.Services;
using SatinalmaPro.Theme;

namespace SatinalmaPro.Controls.Dashboard;

public partial class ErpOpenRecordsPanel : UserControl
{
    public ErpOpenRecordsPanel() => InitializeComponent();

    public void Bagla(IReadOnlyList<AnaSayfaAcikKayit> kayitlar)
    {
        Liste.ItemsSource = kayitlar.Select(k => new SatirVm(k)).ToList();
        TxtAltBilgi.Text = $"Toplam {kayitlar.Count} kayıt";
    }

    private sealed class SatirVm
    {
        public SatirVm(AnaSayfaAcikKayit k)
        {
            No = k.No;
            Tarih = k.Tarih;
            Cari = k.Cari;
            Vade = k.Vade;
            Tutar = k.Tutar;
            Kalan = k.Kalan;
            Durum = k.Durum;
            DurumArkaplan = AppTheme.TintBrush(AppTheme.Parse(k.DurumRenkHex), 40);
            DurumOnplan = AppTheme.Brush(k.DurumRenkHex);
        }

        public string No { get; }
        public string Tarih { get; }
        public string Cari { get; }
        public string Vade { get; }
        public string Tutar { get; }
        public string Kalan { get; }
        public string Durum { get; }
        public System.Windows.Media.Brush DurumArkaplan { get; }
        public System.Windows.Media.Brush DurumOnplan { get; }
    }
}

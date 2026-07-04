using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SatinalmaPro.Models.SatinalmaMerkezi;
using SatinalmaPro.Services;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.ViewModels;

public sealed class SatinalmaMerkeziViewModel : INotifyPropertyChanged
{
    private string _aramaMetni = "";
    private string _aktifSekme = "Talepler";
    private string _aktifFiltre = "Tümü";
    private TalepSatirModel? _seciliTalep;
    private SiparisSatirModel? _seciliSiparis;
    private TalepDetayModel? _detay;
    private bool _karsilastirmaGoster;
    private bool _yukleniyor;
    private int _okunmamisBildirim;

    public SatinalmaMerkeziViewModel()
    {
        KpiKartlar = new ObservableCollection<KpiKartModel>();
        YapilacakIsler = new ObservableCollection<YapilacakIsModel>();
        SonHareketler = new ObservableCollection<SonHareketModel>();
        Talepler = new ObservableCollection<TalepSatirModel>();
        Siparisler = new ObservableCollection<SiparisSatirModel>();
        BeklenenSiparisler = new ObservableCollection<SiparisSatirModel>();
        DepoTakip = new ObservableCollection<DepoTakipSatirModel>();
        Teklifler = new ObservableCollection<TeklifSatirModel>();
        TedarikciPerformans = new ObservableCollection<TedarikciPerformansModel>();
        Iadeler = new ObservableCollection<IadeSatirModel>();
        Tamamlananlar = new ObservableCollection<TamamlananSatirModel>();
        Bildirimler = new ObservableCollection<BildirimModel>();
        Filtreler = ["Tümü", "Bekleyen", "Acil", "Normal", "Teklif Bekleyen", "Onaylanan", "Reddedilen", "Siparişe Dönüşen"];
        Sekmeler =
        [
            new SekmeModel("Talepler", true),
            new SekmeModel("Teklifler", TeklifGorebilir),
            new SekmeModel("Siparişler", true),
            new SekmeModel("Beklenen Siparişler", true),
            new SekmeModel("Teslimatlar", true),
            new SekmeModel("İadeler", true),
            new SekmeModel("Tamamlananlar", true)
        ];

        SekmeDegistirKomutu = new RelayCommand(p => { if (p is string s) AktifSekme = s; });
        TalepSecKomutu = new RelayCommand(p => { if (p is TalepSatirModel t) TalepSec(t); });
        SiparisSecKomutu = new RelayCommand(p => { if (p is SiparisSatirModel s) SiparisSec(s); });
        FiltreKomutu = new RelayCommand(p => { if (p is string f) AktifFiltre = f; TalepleriFiltrele(); });
        KpiKomutu = new RelayCommand(p => { if (p is string k) KpiFiltreUygula(k); });
        YenileKomutu = new RelayCommand(_ => Yukle());
        KarsilastirmaKomutu = new RelayCommand(_ => KarsilastirmaGoster = !KarsilastirmaGoster);
        BildirimKomutu = new RelayCommand(_ => BildirimPanelAc?.Invoke());
        OperasyonModuKomutu = new RelayCommand(_ => OperasyonModuIstendi?.Invoke(null, "talepler"));

        BildirimYoneticisi.BildirimlerDegisti += () =>
        {
            OkunmamisBildirim = BildirimYoneticisi.OkunmamisSayisi;
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? BildirimPanelAc;
    public event Action<Guid?, string>? OperasyonModuIstendi;

    public ObservableCollection<KpiKartModel> KpiKartlar { get; }
    public ObservableCollection<YapilacakIsModel> YapilacakIsler { get; }
    public ObservableCollection<SonHareketModel> SonHareketler { get; }
    public ObservableCollection<TalepSatirModel> Talepler { get; }
    public ObservableCollection<SiparisSatirModel> Siparisler { get; }
    public ObservableCollection<SiparisSatirModel> BeklenenSiparisler { get; }
    public ObservableCollection<DepoTakipSatirModel> DepoTakip { get; }
    public ObservableCollection<TeklifSatirModel> Teklifler { get; }
    public ObservableCollection<TedarikciPerformansModel> TedarikciPerformans { get; }
    public ObservableCollection<IadeSatirModel> Iadeler { get; }
    public ObservableCollection<TamamlananSatirModel> Tamamlananlar { get; }
    public ObservableCollection<BildirimModel> Bildirimler { get; }
    public ObservableCollection<string> Filtreler { get; }
    public ObservableCollection<SekmeModel> Sekmeler { get; }

    public ICommand SekmeDegistirKomutu { get; }
    public ICommand TalepSecKomutu { get; }
    public ICommand SiparisSecKomutu { get; }
    public ICommand FiltreKomutu { get; }
    public ICommand KpiKomutu { get; }
    public ICommand YenileKomutu { get; }
    public ICommand KarsilastirmaKomutu { get; }
    public ICommand BildirimKomutu { get; }
    public ICommand OperasyonModuKomutu { get; }

    public string AramaMetni
    {
        get => _aramaMetni;
        set { if (_aramaMetni == value) return; _aramaMetni = value; OnPropertyChanged(); TalepleriFiltrele(); }
    }

    public string AktifSekme
    {
        get => _aktifSekme;
        set
        {
            if (_aktifSekme == value) return;
            _aktifSekme = value;
            foreach (var s in Sekmeler) s.Aktif = s.Ad == value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SekmeTalepler));
            OnPropertyChanged(nameof(SekmeTeklifler));
            OnPropertyChanged(nameof(SekmeSiparisler));
            OnPropertyChanged(nameof(SekmeBeklenen));
            OnPropertyChanged(nameof(SekmeTeslimatlar));
            OnPropertyChanged(nameof(SekmeIadeler));
            OnPropertyChanged(nameof(SekmeTamamlanan));
        }
    }

    public string AktifFiltre
    {
        get => _aktifFiltre;
        set { if (_aktifFiltre == value) return; _aktifFiltre = value; OnPropertyChanged(); TalepleriFiltrele(); }
    }

    public TalepDetayModel? Detay
    {
        get => _detay;
        private set { _detay = value; OnPropertyChanged(); OnPropertyChanged(nameof(DetayBaslik)); OnPropertyChanged(nameof(DetayVar)); }
    }

    public bool KarsilastirmaGoster
    {
        get => _karsilastirmaGoster;
        set { _karsilastirmaGoster = value; OnPropertyChanged(); }
    }

    public bool Yukleniyor
    {
        get => _yukleniyor;
        set { _yukleniyor = value; OnPropertyChanged(); }
    }

    public int OkunmamisBildirim
    {
        get => _okunmamisBildirim;
        private set { _okunmamisBildirim = value; OnPropertyChanged(); }
    }

    public bool TeklifGorebilir => !KullaniciYetkileri.SatinalmaSadeceTalepModu();
    public bool YazmaYetkisi => KullaniciYetkileri.ModulYazabilir("Satınalma");
    public bool DetayVar => Detay is not null;
    public string DetayBaslik => Detay?.TalepNo ?? "Kayıt seçin";

    public Visibility SekmeTalepler => AktifSekme == "Talepler" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SekmeTeklifler => AktifSekme == "Teklifler" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SekmeSiparisler => AktifSekme == "Siparişler" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SekmeBeklenen => AktifSekme == "Beklenen Siparişler" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SekmeTeslimatlar => AktifSekme == "Teslimatlar" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SekmeIadeler => AktifSekme == "İadeler" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility SekmeTamamlanan => AktifSekme == "Tamamlananlar" ? Visibility.Visible : Visibility.Collapsed;

    private IReadOnlyList<TalepSatirModel> _tumTalepler = [];
    private int _yuklemeDevamEdiyor;

    public void Yukle()
    {
        if (Interlocked.CompareExchange(ref _yuklemeDevamEdiyor, 1, 0) != 0)
            return;

        Yukleniyor = true;
        _ = Task.Run(async () =>
        {
            try
            {
                await SatinalmaMerkeziVeriServisi.VerileriHazirlaAsync().ConfigureAwait(false);

                var kpi = SatinalmaMerkeziVeriServisi.KpiKartlari();
                var yapilacak = SatinalmaMerkeziVeriServisi.YapilacakIsler();
                var hareketler = SatinalmaMerkeziVeriServisi.SonHareketler();
                var tumTalepler = SatinalmaMerkeziVeriServisi.Talepler();
                var siparisler = SatinalmaMerkeziVeriServisi.Siparisler();
                var beklenen = SatinalmaMerkeziVeriServisi.BeklenenSiparisler();
                var depo = SatinalmaMerkeziVeriServisi.DepoTakip();
                var performans = SatinalmaMerkeziVeriServisi.TedarikciPerformans();
                var iadeler = SatinalmaMerkeziVeriServisi.Iadeler();
                var tamamlanan = SatinalmaMerkeziVeriServisi.Tamamlananlar();
                var bildirimler = SatinalmaMerkeziVeriServisi.Bildirimler();
                var okunmamis = BildirimYoneticisi.OkunmamisSayisi;

                var ui = Application.Current?.Dispatcher;
                if (ui is null)
                    return;

                await ui.InvokeAsync(() =>
                {
                    Doldur(KpiKartlar, kpi);
                    Doldur(YapilacakIsler, yapilacak);
                    Doldur(SonHareketler, hareketler);
                    _tumTalepler = tumTalepler;
                    TalepleriFiltrele();
                    Doldur(Siparisler, siparisler);
                    Doldur(BeklenenSiparisler, beklenen);
                    Doldur(DepoTakip, depo);
                    Doldur(TedarikciPerformans, performans);
                    Doldur(Iadeler, iadeler);
                    Doldur(Tamamlananlar, tamamlanan);
                    Doldur(Bildirimler, bildirimler);
                    OkunmamisBildirim = okunmamis;
                    if (Talepler.FirstOrDefault() is { } ilk)
                        TalepSec(ilk);
                    Yukleniyor = false;
                });
            }
            catch (Exception ex)
            {
                HataGunlugu.Kaydet(ex, "SatinalmaMerkeziViewModel.Yukle");
                var ui = Application.Current?.Dispatcher;
                if (ui is not null)
                    await ui.InvokeAsync(() => Yukleniyor = false);
            }
            finally
            {
                Interlocked.Exchange(ref _yuklemeDevamEdiyor, 0);
            }
        });
    }

    public void TalepSec(TalepSatirModel talep)
    {
        _seciliTalep = talep;
        Detay = SatinalmaMerkeziVeriServisi.TalepDetay(talep.Id);
        Teklifler.Clear();
        foreach (var t in Detay.Teklifler) Teklifler.Add(t);
    }

    public void SiparisSec(SiparisSatirModel siparis) => _seciliSiparis = siparis;

    public void BildirimdenAc(Guid? talepId, int adim = 0, string sekme = "talepler")
    {
        if (DeepLinkServisi.SatinalmaOperasyonGerektirir(sekme))
        {
            OperasyonModuIstendi?.Invoke(talepId, sekme);
            return;
        }

        var merkeziSekme = MasaustuRolHaritasi.RouteToSatinalmaSekme(sekme);
        AktifSekme = sekme switch
        {
            "teklifler" or "teklif-bekleyen" or "teklif-onay" or "teklif-giris" or "teklif-gir" => "Teklifler",
            "siparisler-bekleyen" => "Beklenen Siparişler",
            "siparisler" or "onaylanan-malzemeler" => "Siparişler",
            "teslimatlar" or "depo" => "Teslimatlar",
            "iadeler" => "İadeler",
            "tamamlananlar" => "Tamamlananlar",
            _ when merkeziSekme is "Teklif Girişi" or "Karşılaştırma" or "Teklif Onay" => "Teklifler",
            _ when merkeziSekme is "Alınan Malzemeler" => "Siparişler",
            _ => "Talepler"
        };
        if (talepId is null) return;
        var talep = _tumTalepler.FirstOrDefault(t => t.Id == talepId);
        if (talep is not null) TalepSec(talep);
    }

    private void TalepleriFiltrele()
    {
        Talepler.Clear();
        var q = _tumTalepler.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(AramaMetni))
        {
            var a = AramaMetni.Trim();
            q = q.Where(t => t.TalepNo.Contains(a, StringComparison.OrdinalIgnoreCase)
                || t.Santiye.Contains(a, StringComparison.OrdinalIgnoreCase)
                || t.TalepEden.Contains(a, StringComparison.OrdinalIgnoreCase));
        }
        if (AktifFiltre != "Tümü")
            q = q.Where(t => AktifFiltre switch
            {
                "Bekleyen" => t.Durum.Contains("Bekleyen", StringComparison.OrdinalIgnoreCase),
                "Acil" => t.Oncelik.Equals("Acil", StringComparison.OrdinalIgnoreCase),
                "Normal" => t.Oncelik.Equals("Normal", StringComparison.OrdinalIgnoreCase),
                "Teklif Bekleyen" => t.Durum.Contains("Teklif", StringComparison.OrdinalIgnoreCase),
                "Onaylanan" => t.Durum.Contains("Onay", StringComparison.OrdinalIgnoreCase),
                "Reddedilen" => t.Durum.Contains("Red", StringComparison.OrdinalIgnoreCase),
                "Siparişe Dönüşen" => t.Durum.Contains("Sipariş", StringComparison.OrdinalIgnoreCase),
                _ => true
            });
        foreach (var t in q) Talepler.Add(t);
    }

    private void KpiFiltreUygula(string anahtar)
    {
        AktifSekme = anahtar switch
        {
            "teklif" => "Teklifler",
            "siparis" or "bugun" => "Siparişler",
            "teslimat" or "kismi" => "Teslimatlar",
            "iade" => "İadeler",
            _ => "Talepler"
        };
        AktifFiltre = anahtar switch
        {
            "bekleyen" => "Bekleyen",
            "teklif" => "Teklif Bekleyen",
            "onay" => "Onaylanan",
            "kismi" => "Tümü",
            _ => "Tümü"
        };
        TalepleriFiltrele();
    }

    private static void Doldur<T>(ObservableCollection<T> hedef, IEnumerable<T> kaynak)
    {
        hedef.Clear();
        foreach (var o in kaynak) hedef.Add(o);
    }

    private void OnPropertyChanged([CallerMemberName] string? ad = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(ad));
}

public sealed class SekmeModel(string ad, bool gorunur) : INotifyPropertyChanged
{
    private bool _aktif;

    public string Ad { get; } = ad;
    public bool Gorunur { get; } = gorunur;

    public bool Aktif
    {
        get => _aktif;
        set { _aktif = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Aktif))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

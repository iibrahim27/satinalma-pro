using Microsoft.Maui.Controls.Shapes;
using SatinalmaPro.Mobile.Helpers;
using SatinalmaPro.Mobile.Services;
using SatinalmaPro.Shared.Helpers;
using SatinalmaPro.Shared.Models;
using SatinalmaPro.Shared.Services;

namespace SatinalmaPro.Mobile.Views;

[QueryProperty(nameof(TalepId), "id")]
public partial class TeklifOnayDetayPage : ContentPage
{
    private readonly OturumServisi _oturum;
    private string _talepId = "";
    private SatinalmaTalep? _talep;

    public string TalepId
    {
        get => _talepId;
        set
        {
            _talepId = value;
            _ = YukleAsync();
        }
    }

    public TeklifOnayDetayPage(OturumServisi oturum)
    {
        InitializeComponent();
        _oturum = oturum;
        RefreshView.Command = new Command(async () =>
        {
            await YukleAsync();
            RefreshView.IsRefreshing = false;
        });
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!await MobilSayfaKorumasi.StackErisimAsync(this, _oturum, "teklif-onay-detay"))
            return;
        if (!string.IsNullOrWhiteSpace(_talepId))
            await YukleAsync();
    }

    private async Task YukleAsync()
    {
        if (!Guid.TryParse(_talepId, out var id))
            return;

        await _oturum.VerileriYenileAsync();
        Icerik.Clear();

        var talep = _oturum.Depo.Talepler.FirstOrDefault(t => t.Id == id);
        _talep = talep;
        if (talep is null)
        {
            Icerik.Add(new Label { Text = "Talep bulunamadı.", TextColor = Colors.Gray });
            return;
        }

        talep.Kalemler ??= [];
        talep.Teklifler ??= [];
        talep.SatinalmaOnerisiMigrasyonu();

        var duzenlemeModu = SatinalmaOnayYetkisi.FirmaOnayiDuzenlenebilir(_oturum.Depo.AktifKullanici)
            && talep.HerhangiKalemOnayli
            && (talep.YonetimOnayKilitli || talep.Durum == SatinalmaTalepDurumlari.Onaylandi);
        var onayBekliyor = SatinalmaTalepYardimcisi.TeklifYonetimOnayiBekliyor(talep) && !duzenlemeModu;
        var onayVerebilir = SatinalmaOnayYetkisi.TeklifOnayVerebilir(_oturum.Depo.AktifKullanici);

        if (!onayBekliyor && !duzenlemeModu)
        {
            Icerik.Add(new Label { Text = "Bu talep için onay bekleyen teklif yok.", TextColor = Colors.Gray });
            return;
        }

        var baslik = string.IsNullOrWhiteSpace(talep.TalepAciklamasi)
            ? talep.TalepEden
            : talep.TalepAciklamasi;

        Icerik.Add(new Label
        {
            Text = $"{talep.TalepNo} — {baslik}",
            FontAttributes = FontAttributes.Bold,
            FontSize = 18,
            TextColor = TemaKaynaklari.BirincilMetin
        });

        if (!string.IsNullOrWhiteSpace(talep.TeklifDuzeltmeNotu))
        {
            Icerik.Add(new Border
            {
                Padding = 10,
                Margin = new Thickness(0, 0, 0, 8),
                BackgroundColor = Color.FromArgb("#FEF3C7"),
                Stroke = Color.FromArgb("#FCD34D"),
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Content = new Label
                {
                    Text = $"Yönetim düzeltme notu: {talep.TeklifDuzeltmeNotu}",
                    FontSize = 12,
                    TextColor = Color.FromArgb("#92400E")
                }
            });
        }

        Icerik.Add(KalemKutusu(talep));
        Icerik.Add(KarsilastirmaTablosu(talep));

        var onerilen = talep.OnerilenTeklif();

        Icerik.Add(new Label
        {
            Text = "Satınalma Önerisi",
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = TemaKaynaklari.BirincilMetin,
            Margin = new Thickness(0, 8, 0, 0)
        });
        Icerik.Add(new Label
        {
            Text = onerilen is null
                ? "Önerilen teklif belirlenemedi."
                : talep.SatinalmaOnerisiElleSecildi
                    ? $"Satınalma önerisi: {onerilen.FirmaAdi} — {onerilen.GenelToplam:N2} ₺ (KDV dahil, elle seçildi)"
                    : $"En uygun fiyat: {onerilen.FirmaAdi} — {onerilen.GenelToplam:N2} ₺ (KDV dahil, otomatik)",
            FontSize = 12,
            TextColor = TemaKaynaklari.IkincilMetin,
            Margin = new Thickness(0, 0, 0, 4)
        });

        Icerik.Add(KalemOnayPaneli(talep, onerilen, duzenlemeModu, onayVerebilir, onayBekliyor));

        if (onayBekliyor && SatinalmaOnayYetkisi.YonetimKararVerebilir(_oturum.Depo.AktifKullanici))
            Icerik.Add(YonetimKararPaneli(talep));

        var siraliTeklifler = talep.Teklifler
            .OrderByDescending(t => onerilen is not null && t.Id == onerilen.Id)
            .ThenBy(t => t.FirmaAdi)
            .ToList();

        foreach (var teklif in siraliTeklifler)
        {
            var t = talep;
            var tk = teklif;
            var oneriMi = onerilen is not null && teklif.Id == onerilen.Id;
            teklif.FiyatlariHesapla(t.Kalemler);
            var kdvHaric = teklif.Fiyatlar.Sum(f => f.ToplamTutar);

            var kartIcerik = new VerticalStackLayout { Spacing = 6 };
            var kart = new Border
            {
                Padding = 12,
                Margin = new Thickness(0, 4),
                BackgroundColor = oneriMi ? TemaKaynaklari.OneriArkaPlan : TemaKaynaklari.KartArkaPlan,
                Stroke = oneriMi ? TemaKaynaklari.OneriCerceve : TemaKaynaklari.KartCerceve,
                StrokeThickness = oneriMi ? 2.5 : 1,
                StrokeShape = new RoundRectangle { CornerRadius = 8 }
            };

            if (oneriMi)
            {
                kartIcerik.Add(new Label
                {
                    Text = "✓ Satınalma önerisi — en uygun teklif",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = TemaKaynaklari.OneriCerceve
                });
            }

            kartIcerik.Add(new Label
            {
                Text = teklif.FirmaAdi,
                FontAttributes = FontAttributes.Bold,
                TextColor = TemaKaynaklari.BirincilMetin
            });
            kartIcerik.Add(new Label
            {
                Text = $"Toplam (KDV hariç): {kdvHaric:N2} ₺ · KDV dahil: {teklif.GenelToplam:N2} ₺",
                TextColor = Colors.Gray,
                FontSize = 12
            });

            foreach (var kalem in t.Kalemler.OrderBy(k => k.SiraNo))
            {
                var fiyat = teklif.Fiyatlar.FirstOrDefault(f => f.KalemId == kalem.Id);
                var birimFiyat = fiyat?.BirimFiyat ?? 0;
                var pb = fiyat?.ParaBirimi ?? "TRY";
                var tlBirim = fiyat?.TlBirimFiyat(teklif.UsdKuru, teklif.EurKuru) ?? 0;
                var satirKdvHaric = fiyat?.ToplamTutar ?? 0;
                kartIcerik.Add(new Label
                {
                    Text = $"• {kalem.Malzeme} — {kalem.Miktar:N2} {kalem.Birim} · {birimFiyat:N2} {pb} (≈{tlBirim:N2} ₺) · KDV %{fiyat?.KdvOrani ?? 0:N0} · {satirKdvHaric:N2} ₺",
                    FontSize = 12,
                    TextColor = TemaKaynaklari.VurguMetin,
                    LineBreakMode = LineBreakMode.WordWrap
                });
            }

            kartIcerik.Add(new Label
            {
                Text = $"Vade: {teklif.VadeGunu} gün · Teslim: {teklif.TeslimSuresi}",
                FontSize = 11,
                TextColor = TemaKaynaklari.SolukMetin
            });

            var btn = new Button
            {
                Text = "Tüm kalemlere uygula",
                BackgroundColor = TemaKaynaklari.Marka,
                TextColor = Colors.White,
                FontSize = 12,
                HorizontalOptions = LayoutOptions.End
            };
            btn.Clicked += async (_, _) =>
            {
                try
                {
                    await _oturum.Satinalma.TumKalemlereTeklifAtaAsync(t, tk.Id);
                    await DisplayAlert("Atandı", $"{tk.FirmaAdi} tüm kalemlere atandı.", "Tamam");
                    await YukleAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Hata", ex.Message, "Tamam");
                }
            };
            kartIcerik.Add(btn);
            kart.Content = kartIcerik;
            Icerik.Add(kart);
        }
    }

    private View KalemOnayPaneli(SatinalmaTalep talep, SatinalmaTeklif? onerilen, bool duzenlemeModu, bool onayVerebilir, bool onayBekliyor)
    {
        var panel = new VerticalStackLayout { Spacing = 8, Margin = new Thickness(0, 8, 0, 12) };

        panel.Add(new Label
        {
            Text = duzenlemeModu ? "Onay Düzeltme" : "Kalem Bazlı Onay",
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = TemaKaynaklari.BirincilMetin
        });

        panel.Add(new Label
        {
            Text = duzenlemeModu
                ? "Firma atamalarını değiştirebilir, yeniden onaylayabilir veya tüm onayı geri alabilirsiniz."
                : "Her malzeme için firma seçin, ardından onayları kaydedin.",
            FontSize = 12,
            TextColor = TemaKaynaklari.IkincilMetin
        });

        if (!onayVerebilir)
        {
            panel.Add(new Label
            {
                Text = "Onay işlemi yalnızca Yönetim ve Satınalma rollerinde yapılabilir.",
                TextColor = Colors.OrangeRed,
                FontSize = 12
            });
            return panel;
        }

        var secenekler = new List<TeklifSecenegi>
        {
            new(null, "— Seçiniz —")
        };
        secenekler.AddRange(talep.Teklifler.OrderBy(t => t.FirmaAdi).Select(t => new TeklifSecenegi(t.Id, t.FirmaAdi)));

        foreach (var kalem in talep.Kalemler.OrderBy(k => k.SiraNo))
        {
            var k = kalem;
            var satir = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(160)
                },
                ColumnSpacing = 8,
                Margin = new Thickness(0, 2)
            };

            satir.Add(new Label
            {
                Text = $"{kalem.Malzeme}\n{kalem.Miktar:N2} {kalem.Birim}",
                FontSize = 12,
                VerticalOptions = LayoutOptions.Center,
                TextColor = TemaKaynaklari.VurguMetin
            }, 0, 0);

            var picker = new Picker
            {
                Title = "Firma seç",
                ItemDisplayBinding = new Binding(nameof(TeklifSecenegi.Ad)),
                ItemsSource = secenekler,
                VerticalOptions = LayoutOptions.Center
            };

            var seciliIdx = secenekler.FindIndex(s => s.Id == kalem.OnaylananTeklifId);
            if (seciliIdx >= 0)
                picker.SelectedIndex = seciliIdx;

            picker.SelectedIndexChanged += async (_, _) =>
            {
                if (picker.SelectedIndex < 0)
                    return;

                var secim = secenekler[picker.SelectedIndex];
                try
                {
                    await _oturum.Satinalma.KalemTeklifiAtaAsync(talep, k.Id, secim.Id);
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Hata", ex.Message, "Tamam");
                }
            };

            satir.Add(picker, 1, 0);
            panel.Add(satir);
        }

        if (onerilen is not null)
        {
            var oneriBtn = new Button
            {
                Text = "Önerilen teklifi tüm kalemlere uygula",
                Style = (Style)Application.Current!.Resources["BtnInfoDark"]
            };
            oneriBtn.Clicked += async (_, _) =>
            {
                try
                {
                    await _oturum.Satinalma.TumKalemlereTeklifAtaAsync(talep, onerilen.Id);
                    await DisplayAlert("Atandı", $"{onerilen.FirmaAdi} tüm kalemlere uygulandı.", "Tamam");
                    await YukleAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Hata", ex.Message, "Tamam");
                }
            };
            panel.Add(oneriBtn);
        }

        var kaydetBtn = new Button
        {
            Text = duzenlemeModu ? "Onayları Güncelle" : "Onayları Kaydet",
            Style = (Style)Application.Current!.Resources["BtnBrand"]
        };
        kaydetBtn.Clicked += async (_, _) =>
        {
            var onayli = talep.Kalemler.Count(k => k.OnaylananTeklifId != null);
            if (onayli == 0)
            {
                await DisplayAlert("Uyarı", "En az bir malzeme için firma seçin.", "Tamam");
                return;
            }

            var onay = await DisplayAlert(
                duzenlemeModu ? "Onayları Güncelle" : "Onayları Kaydet",
                duzenlemeModu
                    ? $"{onayli} kalem için firma onayı güncellenecek. Devam edilsin mi?"
                    : $"{onayli} kalem onaylanacak. Devam edilsin mi?",
                "Kaydet", "İptal");
            if (!onay)
                return;

            try
            {
                kaydetBtn.IsEnabled = false;
                await _oturum.Satinalma.KalemBazliOnaylaAsync(talep);
                _ = _oturum.Dinleyici.SenkronizeVeGosterAsync();
                await DisplayAlert(
                    duzenlemeModu ? "Güncellendi" : "Onaylandı",
                    duzenlemeModu ? $"{onayli} kalem onayı güncellendi." : $"{onayli} kalem onaylandı.",
                    "Tamam");
                if (duzenlemeModu)
                    await YukleAsync();
                else
                    await ShellGuvenli.GoToAsync("//gecmis-teklifli-onaylar");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", ex.Message, "Tamam");
            }
            finally
            {
                kaydetBtn.IsEnabled = true;
            }
        };
        panel.Add(kaydetBtn);

        if (duzenlemeModu)
        {
            var geriAlBtn = new Button
            {
                Text = "Onayı Geri Al",
                Style = (Style)Application.Current!.Resources["BtnDanger"]
            };
            geriAlBtn.Clicked += async (_, _) =>
            {
                var onay = await DisplayAlert(
                    "Onayı Geri Al",
                    "Tüm firma onayları geri alınacak. Devam edilsin mi?",
                    "Geri Al", "İptal");
                if (!onay)
                    return;

                try
                {
                    geriAlBtn.IsEnabled = false;
                    await _oturum.Satinalma.FirmaOnaylariniGeriAlAsync(talep);
                    await DisplayAlert("Geri alındı", "Onaylar geri alındı. Firmaları yeniden seçebilirsiniz.", "Tamam");
                    await YukleAsync();
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Hata", ex.Message, "Tamam");
                }
                finally
                {
                    geriAlBtn.IsEnabled = true;
                }
            };
            panel.Add(geriAlBtn);
        }

        return new Border
        {
            Padding = 12,
            BackgroundColor = TemaKaynaklari.KartArkaPlan,
            Stroke = TemaKaynaklari.KartCerceve,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = panel
        };
    }

    private View YonetimKararPaneli(SatinalmaTalep talep)
    {
        var panel = new VerticalStackLayout { Spacing = 8, Margin = new Thickness(0, 0, 0, 12) };
        panel.Add(new Label
        {
            Text = "Yönetim Kararı",
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            TextColor = TemaKaynaklari.BirincilMetin
        });

        var geriBtn = new Button
        {
            Text = "Satınalmaya Geri Gönder",
            Style = (Style)Application.Current!.Resources["BtnInfoDark"]
        };
        geriBtn.Clicked += async (_, _) =>
        {
            var not = await DisplayPromptAsync(
                "Satınalmaya Geri Gönder",
                "Düzeltme notu (isteğe bağlı):",
                "Geri Gönder",
                "İptal",
                maxLength: 500);
            if (not is null)
                return;

            try
            {
                geriBtn.IsEnabled = false;
                await _oturum.Satinalma.TeklifGeriGonderAsync(talep, not);
                _ = _oturum.Dinleyici.SenkronizeVeGosterAsync();
                await DisplayAlert("Gönderildi", $"{talep.TalepNo} satınalmaya düzeltme için geri gönderildi.", "Tamam");
                await ShellGuvenli.GoToAsync("//teklif-onay");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", ex.Message, "Tamam");
            }
            finally
            {
                geriBtn.IsEnabled = true;
            }
        };
        panel.Add(geriBtn);

        var redBtn = new Button
        {
            Text = "Reddet",
            Style = (Style)Application.Current!.Resources["BtnDanger"]
        };
        redBtn.Clicked += async (_, _) =>
        {
            var gerekce = await DisplayPromptAsync(
                "Teklif Red",
                "Red gerekçesini girin:",
                "Reddet",
                "İptal",
                maxLength: 500);
            if (string.IsNullOrWhiteSpace(gerekce))
            {
                if (gerekce is not null)
                    await DisplayAlert("Uyarı", "Red gerekçesi zorunludur.", "Tamam");
                return;
            }

            var onay = await DisplayAlert("Teklif Red", $"{talep.TalepNo} reddedilsin mi?", "Reddet", "İptal");
            if (!onay)
                return;

            try
            {
                redBtn.IsEnabled = false;
                await _oturum.Satinalma.TeklifReddetAsync(talep, gerekce);
                _ = _oturum.Dinleyici.SenkronizeVeGosterAsync();
                await DisplayAlert("Reddedildi", $"{talep.TalepNo} reddedildi.", "Tamam");
                await ShellGuvenli.GoToAsync("//teklif-onay");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", ex.Message, "Tamam");
            }
            finally
            {
                redBtn.IsEnabled = true;
            }
        };
        panel.Add(redBtn);

        return new Border
        {
            Padding = 12,
            BackgroundColor = TemaKaynaklari.KartArkaPlan,
            Stroke = TemaKaynaklari.KartCerceve,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = panel
        };
    }

    private sealed record TeklifSecenegi(Guid? Id, string Ad);

    private static View KarsilastirmaTablosu(SatinalmaTalep talep)
    {
        var onerilen = talep.OnerilenTeklif();
        var teklifler = talep.Teklifler.OrderBy(t => t.FirmaAdi).ToList();
        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(140) },
            RowDefinitions = { new RowDefinition(GridLength.Auto) },
            ColumnSpacing = 4,
            RowSpacing = 4,
            Margin = new Thickness(0, 0, 0, 8)
        };

        foreach (var _ in teklifler)
            grid.ColumnDefinitions.Add(new ColumnDefinition(110));

        grid.Add(Hucre("Malzeme", kalin: true), 0, 0);
        for (var c = 0; c < teklifler.Count; c++)
        {
            teklifler[c].FiyatlariHesapla(talep.Kalemler);
            var oneriMi = onerilen is not null && teklifler[c].Id == onerilen.Id;
            grid.Add(Hucre(teklifler[c].FirmaAdi, kalin: true, oneri: oneriMi), c + 1, 0);
        }

        var satir = 1;
        foreach (var kalem in talep.Kalemler.OrderBy(k => k.SiraNo))
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.Add(Hucre($"{kalem.Malzeme}\n{kalem.Miktar:N1} {kalem.Birim}"), 0, satir);
            for (var c = 0; c < teklifler.Count; c++)
            {
                var f = teklifler[c].Fiyatlar.FirstOrDefault(x => x.KalemId == kalem.Id);
                var metin = f is null
                    ? "—"
                    : $"{f.BirimFiyat:N2} {f.ParaBirimi}\n≈{f.TlBirimFiyat(teklifler[c].UsdKuru, teklifler[c].EurKuru):N2} ₺";
                var oneriMi = onerilen is not null && teklifler[c].Id == onerilen.Id;
                grid.Add(Hucre(metin, oneri: oneriMi), c + 1, satir);
            }

            satir++;
        }

        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        grid.Add(Hucre("TOPLAM", kalin: true), 0, satir);
        for (var c = 0; c < teklifler.Count; c++)
        {
            var oneriMi = onerilen is not null && teklifler[c].Id == onerilen.Id;
            grid.Add(Hucre($"{teklifler[c].GenelToplam:N2} ₺", kalin: true, oneri: oneriMi), c + 1, satir);
        }

        return new Border
        {
            Padding = 8,
            BackgroundColor = TemaKaynaklari.KartArkaPlan,
            Stroke = TemaKaynaklari.KartCerceve,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = new ScrollView
            {
                Orientation = ScrollOrientation.Horizontal,
                Content = grid
            }
        };
    }

    private static Label Hucre(string metin, bool kalin = false, bool oneri = false) => new()
    {
        Text = metin,
        FontSize = 10,
        FontAttributes = kalin ? FontAttributes.Bold : FontAttributes.None,
        TextColor = oneri ? TemaKaynaklari.OneriCerceve : TemaKaynaklari.VurguMetin,
        BackgroundColor = oneri ? TemaKaynaklari.OneriArkaPlan : Colors.Transparent,
        LineBreakMode = LineBreakMode.WordWrap,
        Padding = new Thickness(4)
    };

    private async void BelgePaylas_Clicked(object sender, EventArgs e)
    {
        if (_talep is null)
        {
            await DisplayAlert("Uyarı", "Talep henüz yüklenmedi.", "Tamam");
            return;
        }

        if ((_talep.Teklifler?.Count ?? 0) == 0)
        {
            await DisplayAlert("Uyarı", "Paylaşılacak teklif bulunamadı.", "Tamam");
            return;
        }

        try
        {
            IsBusy = true;
            var talep = _talep;
            await MobilBelgePaylasServisi.PdfOlusturVePaylasAsync(
                () => MobilPdfOlusturucu.TeklifKarsilastirmaPdf(talep),
                $"{talep.TalepNo}_teklif.pdf",
                "Teklif Karşılaştırması");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PDF paylaş: {ex}");
            await DisplayAlert("PDF Hatası", ex.Message, "Tamam");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static View KalemKutusu(SatinalmaTalep talep)
    {
        var panel = new VerticalStackLayout { Spacing = 4 };
        panel.Add(new Label
        {
            Text = "Talep Kalemleri",
            FontAttributes = FontAttributes.Bold,
            FontSize = 13,
            TextColor = TemaKaynaklari.IkincilMetin
        });

        foreach (var satir in talep.KalemSatirlari())
        {
            panel.Add(new Label
            {
                Text = $"• {satir}",
                FontSize = 12,
                TextColor = TemaKaynaklari.VurguMetin
            });
        }

        if (talep.Kalemler.Count == 0)
            panel.Add(new Label { Text = "Kalem bilgisi yok", FontSize = 12, TextColor = Colors.Gray });

        return new Border
        {
            Padding = 10,
            Margin = new Thickness(0, 0, 0, 8),
            BackgroundColor = TemaKaynaklari.KartArkaPlan,
            Stroke = TemaKaynaklari.KartCerceve,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Content = panel
        };
    }
}

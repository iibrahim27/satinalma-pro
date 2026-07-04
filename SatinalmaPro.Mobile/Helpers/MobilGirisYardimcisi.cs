#if ANDROID
using Android.Widget;
#endif
using Microsoft.Maui.Controls.Shapes;

namespace SatinalmaPro.Mobile.Helpers;

public static class MobilGirisYardimcisi
{
    public static Entry GirisOlustur(string placeholder, Keyboard? keyboard = null, string? text = null)
    {
        var entry = new Entry
        {
            Placeholder = placeholder,
            BackgroundColor = Colors.Transparent,
            Text = text ?? ""
        };

        if (keyboard is not null)
            entry.Keyboard = keyboard;

        AndroidGirisHazirlaInternal(entry);
        return entry;
    }

    public static View Cercevele(Entry entry) =>
        new Border
        {
            Stroke = TemaKaynaklari.KartCerceve,
            BackgroundColor = TemaKaynaklari.KartArkaPlan,
            StrokeThickness = 1,
            Padding = new Thickness(12, 6),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(8) },
            Content = entry
        };

    public static View CerceveliGiris(string placeholder, Keyboard? keyboard = null, string? text = null) =>
        Cercevele(GirisOlustur(placeholder, keyboard, text));

    public static void AndroidGirisHazirla(Entry entry) =>
        AndroidGirisHazirlaInternal(entry);

    private static void AndroidGirisHazirlaInternal(Entry entry)
    {
#if ANDROID
        entry.HandlerChanged += (_, _) =>
        {
            if (entry.Handler?.PlatformView is EditText edit)
            {
                edit.Focusable = true;
                edit.FocusableInTouchMode = true;
                edit.Clickable = true;
                edit.LongClickable = true;
            }
        };
#endif
    }
}

namespace SatinalmaPro.Services;



public static class VeriYukleyici

{

    public static void TumunuYukle()

    {

        UygulamaAyarDeposu.YenidenYukle();

        SatinalmaDepo.YenidenYukle();

        ModulVeriDeposu.YenidenYukle();

        FinansmanVeriDeposu.YenidenYukle();

    }

}


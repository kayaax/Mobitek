using System.ComponentModel.DataAnnotations;

namespace Grand.Plugin.Payments.AllBank.Models.Enums
{
    public enum BankNames
    {
        //NestPay
        [Display(Name = "AkBank A.Ş")]
        AkBank = 46,

        [Display(Name = "İş Bankası")]
        IsBankasi = 64,

        [Display(Name = "Halk Bankası")]
        HalkBank = 12,

        [Display(Name = "Ziraat Bankası")]
        ZiraatBankasi = 10,

        [Display(Name = "TEB")]
        TurkEkonomiBankasi = 32,

        [Display(Name = "ING Bank")]
        IngBank = 99,

        [Display(Name = "Türkiye Finans Katılım Bankası")]
        TurkiyeFinans = 206,

        [Display(Name = "Anadolubank")]
        AnadoluBank = 135,

        [Display(Name = "HSBC")]
        HSBC = 123,

        [Display(Name = "Şekerbank")]
        SekerBank = 59,

        //InterVPOS
        [Display(Name = "Denizbank")]
        DenizBank = 134,

        //PayFor
        [Display(Name = "Finansbank")]
        FinansBank = 111,

        //GVP
        [Display(Name = "Garanti Bankası")]
        Garanti = 62,

        //KuveytTurk
        [Display(Name = "Kuveyt Türk")]
        KuveytTurk = 205,

        //GET 7/24
        [Display(Name = "Vakıfbank")]
        VakifBank = 15,

        //Posnet
        [Display(Name = "Yapı Kredi Bankası")]
        Yapikredi = 67,
        [Display(Name = "Albaraka Türk")]
        Albaraka = 203,
        //Iyzico
        [Display(Name = "Iyzico")]
        Iyzico = 100,
        [Display(Name = "IPara")]
        IPara = 101 ,

        [Display(Name = "PayTr")]
        PayTr = 102
        
    }
}
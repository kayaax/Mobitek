using Grand.Core.ModelBinding;

namespace Grand.Plugin.Payments.AllBank.Models.Banks
{
    public class ParameterModel
    {
        public ParameterModel()
        {
            NestPayModel = new NestPayModel();
            DenizBankModel = new DenizBankModel();
            FinansBankModel = new FinansBankModel();
            GarantiBankModel = new GarantiBankModel();
            KuveytTurkModel = new KuveytTurkModel();
            VakıfBankModel = new VakıfBankModel();
            YapıKrediBankModel = new YapıKrediBankModel();
            IyzicoBankModel = new IyzicoBankModel();
            ParaPayModel = new ParaPayModel();
            PayTrModel = new PayTrModel();
        }

        public NestPayModel NestPayModel { get; set; }
        public DenizBankModel DenizBankModel { get; set; }
        public FinansBankModel FinansBankModel { get; set; }
        public GarantiBankModel GarantiBankModel { get; set; }
        public KuveytTurkModel KuveytTurkModel { get; set; }
        public VakıfBankModel VakıfBankModel { get; set; }
        public YapıKrediBankModel YapıKrediBankModel { get; set; }
        public IyzicoBankModel IyzicoBankModel { get; set; }
        public ParaPayModel ParaPayModel { get; set; }
        public PayTrModel PayTrModel { get; set; }
    }

    public class NestPayModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.ClientId")]
        public string ClientId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.UserName")]
        public string UserName { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.StoreKey")]
        public string StoreKey { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.StoreType")]
        public string StoreType { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.Password")]
        public string Password { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.GatewayUrl")]
        public string GatewayUrl { get; set; }        
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.VerifyUrl")]
        public string VerifyUrl { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.ImcKod")]
        public string ImcKod { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.iframe")]
        public bool IFrame  { get; set; }
    }

    public class DenizBankModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.ShopCode")]
        public string ShopCode { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.UserCode")]
        public string UserCode { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.UserPass")]
        public string UserPass { get; set; }       
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.StoreKey")]
        public string StoreKey { get; set; }       
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.GatewayUrl")]
        public string GatewayUrl { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.VerifyUrl")]
        public string VerifyUrl { get; set; }
    }
    public class FinansBankModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.MbrId")]
        public string MbrId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.MerchantId")]
        public string MerchantId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.MerchantPass")]
        public string MerchantPass { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.UserCode")]
        public string UserCode { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.UserPass")]
        public string UserPass { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.iframe")]
        public bool IFrame { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.VerifyUrl")]
        public string VerifyUrl { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.GatewayUrl")]
        public string GatewayUrl { get; set; }      

    }
    public class GarantiBankModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.TerminalId")]
        public string TerminalId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.TerminalUserId")]
        public string TerminalUserId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.TerminalMerchantId")]
        public string TerminalMerchantId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.StoreKey")]
        public string StoreKey { get; set; }        
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.TerminalProvUserId")]
        public string TerminalProvUserId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.TerminalProvPassword")]
        public string TerminalProvPassword { get; set; }       
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.CompanyName")]
        public string CompanyName { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.Email")]
        public string Email { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.iframe")]
        public bool IFrame { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.GatewayUrl")]
        public string GatewayUrl { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.VerifyUrl")]
        public string VerifyUrl { get; set; }
    }

    public class KuveytTurkModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.MerchantId")]
        public string MerchantId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.UserName")]
        public string UserName { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.Password")]
        public string Password { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.CustomerNumber")]
        public string CustomerNumber { get; set; }        
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.GatewayUrl")]
        public string GatewayUrl { get; set; }       
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.VerifyUrl")]
        public string VerifyUrl { get; set; }
    }

    public class VakıfBankModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.ClientId")]
        public string MerchantId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.Password")]
        public string Password { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.iframe")]
        public bool IFrame { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.TerminalNo")]
        public string TerminalNo { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.EnrollmentUrl")]
        public string EnrollmentUrl { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.VerifyUrl")]
        public string VerifyUrl { get; set; }
      
    }

    public class YapıKrediBankModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.MerchantId")]
        public string MerchantId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.TerminalId")]
        public string TerminalId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.PosNetId")]
        public string PosNetId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.GatewayUrl")]
        public string GatewayUrl { get; set; }       
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.VerifyUrl")]
        public string VerifyUrl { get; set; }
    }

    public class IyzicoBankModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.ApiKey")]
        public string ApiKey { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.SecretKey")]
        public string SecretKey { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.ApiUrl")]
        public string ApiUrl { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.iframe")]
        public bool IFrame { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.iframe")]
        public bool PopUp { get; set; }
    }

    public class ParaPayModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.PublicKey")]
        public string PublicKey { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.PrivateKey")]
        public string PrivateKey { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.iframe")]
        public bool IFrame { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.BaseUrl")]
        public string BaseUrl { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.ThreeDInquiryUrl")]
        public string ThreeDInquiryUrl { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.HashString")]
        public string HashString { get; set; }
    }

    public class PayTrModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.MerchantId")]
        public string MerchantId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.MerchantKey")]
        public string MerchantKey { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.MerchantSalt")]
        public string MerchantSalt { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.iframe")]
        public bool IFrame { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.GatewayUrl")]
        public string GatewayUrl { get; set; }
      
    }
}

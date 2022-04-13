using Grand.Core.ModelBinding;

namespace Grand.Plugin.Payments.GarantiPay.Models.Banks
{
    public class ParameterModel
    {
        public ParameterModel()
        {
            GarantiBankModel = new GarantiBankModel();
        }
        public GarantiBankModel GarantiBankModel { get; set; }
    }

    public class GarantiBankModel
    {
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.TerminalId")]
        public string TerminalId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.TerminalUserId")]
        public string TerminalUserId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.TerminalMerchantId")]
        public string TerminalMerchantId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.StoreKey")]
        public string StoreKey { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.TerminalProvUserId")]
        public string TerminalProvUserId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.TerminalProvPassword")]
        public string TerminalProvPassword { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.CompanyName")]
        public string CompanyName { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.Email")]
        public string Email { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.GatewayUrl")]
        public string GatewayUrl { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.VerifyUrl")]
        public string VerifyUrl { get; set; }

    }


}

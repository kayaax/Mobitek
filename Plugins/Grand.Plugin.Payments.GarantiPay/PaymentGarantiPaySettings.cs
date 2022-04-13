using Grand.Domain.Configuration;

namespace Grand.Plugin.Payments.GarantiPay
{

    public class PaymentGarantiPaySettings : ISettings
    {

      

        public string DescriptionText { get; set; }

        public bool IsInstallment { get; set; }

        public bool AdditionalFeePercentage { get; set; }

        public decimal AdditionalFee { get; set; }

        public bool TestMode { get; set; }

      

    }
}
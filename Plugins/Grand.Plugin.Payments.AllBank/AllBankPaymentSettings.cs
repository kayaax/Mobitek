using Grand.Domain.Configuration;

namespace Grand.Plugin.Payments.AllBank
{
    public class AllBankPaymentSettings : ISettings
    {
        public string DescriptionText { get; set; }
        public bool IsInstallment { get; set; }
        public bool AdditionalFeePercentage { get; set; }
        public decimal AdditionalFee { get; set; }
       public bool TestMode { get; set; }
       public bool InstallmentCategoryBased { get; set; }

    }
}
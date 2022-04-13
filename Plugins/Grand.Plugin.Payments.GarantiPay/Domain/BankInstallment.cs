using Grand.Domain;

namespace Grand.Plugin.Payments.GarantiPay.Domain
{
    public class BankInstallment:SubBaseEntity
    {
        public string OmniGarantiPayPosId { get; set; }
        public int NumberOfInstallment { get; set; }
        public decimal Percentage { get; set; }
    }
}
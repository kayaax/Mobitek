using Grand.Domain;

namespace Grand.Plugin.Payments.GarantiPay.Domain
{
    public partial class OmniGarantiPayCategory:BaseEntity
    {
        public string Name { get; set; }
        public string CategoryId { get; set; }
        public int MaxInstallment { get; set; }
    }
}

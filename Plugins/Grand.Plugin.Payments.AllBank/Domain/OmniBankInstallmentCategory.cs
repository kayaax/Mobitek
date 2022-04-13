using Grand.Domain;

namespace Grand.Plugin.Payments.AllBank.Domain
{
    public partial class OmniBankInstallmentCategory:BaseEntity
    {
        public string Name { get; set; }
        public string CategoryId { get; set; }
        public int MaxInstallment { get; set; }
    }
}

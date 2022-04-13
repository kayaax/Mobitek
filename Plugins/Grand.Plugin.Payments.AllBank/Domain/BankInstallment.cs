using Grand.Domain;

namespace Grand.Plugin.Payments.AllBank.Domain
{
    public  class BankInstallment : SubBaseEntity
    {
        public string BankPosId { get; set; }
        public int BankId { get; set; }
        public int NumberOfInstallment { get; set; }
        public decimal Percentage { get; set; }
    }
}

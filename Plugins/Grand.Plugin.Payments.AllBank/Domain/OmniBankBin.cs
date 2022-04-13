using Grand.Domain;

namespace Grand.Plugin.Payments.AllBank.Domain
{
    public  class OmniBankBin : BaseEntity
    {
        public string BinNumber { get; set; }
        public string CardType { get; set; }
        public string BankName { get; set; }
        public string CardAssociation { get; set; }
        public string CardFamilyName { get; set; }
        public string BankCode { get; set; }
        public bool BusinessCard { get; set; }
        public int? Force3Ds { get; set; }


    }
}

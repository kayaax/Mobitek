using Grand.Core.ModelBinding;
using Grand.Core.Models;

namespace Grand.Plugin.Payments.AllBank.Models.BinList
{
    public class OmniBankBinModel : BaseEntityModel
    {

        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankBin.Field.ID")]
        public override string Id { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankBin.Field.BinNumber")]
        public string BinNumber { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankBin.Field.CardType")]
        public string CardType { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankBin.Field.BankName")]
        public string BankName { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankBin.Field.CardAssociation")]
        public string CardAssociation { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankBin.Field.CardFamilyName")]
        public string CardFamilyName { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankBin.Field.BankCode")]
        public string BankCode { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankBin.Field.BusinessCard")]
        public bool BusinessCard { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankBin.Field.Force3Ds")]
        public int? Force3Ds { get; set; }

    }
}

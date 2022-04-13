using Grand.Core.ModelBinding;
using Grand.Core.Models;

namespace Grand.Plugin.Payments.GarantiPay.Models.BankInstallments
{
    public class OmniBankInstallmentCategoryModel : BaseEntityModel
    {
        
        public override string Id { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.BankInstallment.Field.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.BankInstallment.Field.CategoryId")]
        public string CategoryId { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.BankInstallment.Field.MaxInstallment")]
        public int MaxInstallment { get; set; }

     
    }

    
}
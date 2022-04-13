using System.Collections.Generic;
using Grand.Core.ModelBinding;
using Grand.Core.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Plugin.Payments.GarantiPay.Models.BankPos
{
    public partial class OmniBankPosModel : BaseEntityModel
    {
        public OmniBankPosModel()
        {
            
            AvailableBankTypeList = new List<SelectListItem>();
            ParameterModel = new Banks.ParameterModel();
            AddBankInstallmentModel = new BankInstallmentModel();
            BankInstallmentModels = new List<BankInstallmentModel>();
        }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.BankPos.Field.ID")]
        public override string Id { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.BankPos.Field.Name")]
        public string Name { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.BankPos.Field.IsActive")]
        public bool IsActive { get; set; }

        public Banks.ParameterModel ParameterModel { get; set; } 

        public IList<SelectListItem> AvailableBankTypeList { get; set; }
        public BankInstallmentModel AddBankInstallmentModel { get; set; }
        public IList<BankInstallmentModel> BankInstallmentModels { get; set; }
        public class BankInstallmentModel : BaseEntityModel
        {
            public override string Id { get; set; }
            [GrandResourceDisplayName("Plugins.Payment.GarantiPay.BankInstallment.Field.BankPosId")]
            public string OmniGarantiPayPosId { get; set; }
            public string BankName { get; set; }
            [GrandResourceDisplayName("Plugins.Payment.GarantiPay.BankInstallment.Field.NumberOfInstallment")]
            public int NumberOfInstallment { get; set; }
            [GrandResourceDisplayName("Plugins.Payment.GarantiPay.BankInstallment.Field.Percentage")]
            public decimal Percentage { get; set; }
           
        }
        
    }
    

    
}

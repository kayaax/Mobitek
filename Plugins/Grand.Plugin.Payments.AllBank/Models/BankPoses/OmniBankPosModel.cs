using Grand.Core.ModelBinding;
using Grand.Core.Models;
using Grand.Plugin.Payments.AllBank.Models.Banks;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Plugin.Payments.AllBank.Models.BankPoses
{
    public partial class OmniBankPosModel : BaseEntityModel
    {
        public OmniBankPosModel()
        {
            
            AvailableBankTypeList = new List<SelectListItem>();
            ParameterModel = new ParameterModel();
            AddBankInstallmentModel = new BankInstallmentModel();
            BankInstallmentModels = new List<BankInstallmentModel>();

        }

        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.ID")]
        public override string Id { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.BankTypeId")]
        public int BankTypeId { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.BankTypeName")]
        public string BankTypeName { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.Name")]
        public string Name { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.SystemName")]
        public string SystemName { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.Picture")]
        [UIHint("Picture")]
        public string PictureId { get; set; }
        public string PictureUrl { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.IsActive")]
        public bool IsActive { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.PrimaryBank")]
        public bool PrimaryBank { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.Primary")]
        public bool Primary { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.BankColor")]
        public string BankColor { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.BankImageId")]
        [UIHint("Picture")]
        public string BankImageId { get; set; }
        public string BankImageUrl { get; set; }
        public ParameterModel ParameterModel { get; set; }
       
        public IList<SelectListItem> AvailableBankTypeList { get; set; }
        public BankInstallmentModel AddBankInstallmentModel { get; set; }
        public IList<BankInstallmentModel> BankInstallmentModels { get; set; }
        public class BankInstallmentModel : BaseEntityModel
        {
            public BankInstallmentModel()
            {
                AvailableBankPos = new List<SelectListItem>();
                AvailableBanks = new List<SelectListItem>();
            }
            public override string Id { get; set; }
            [GrandResourceDisplayName("Plugins.Payment.AllBank.BankInstallment.Field.BankPosId")]
            public string BankPosId { get; set; }
            [GrandResourceDisplayName("Plugins.Payment.AllBank.BankInstallment.Field.BankName")]
            public int BankId { get; set; }
            public string BankName { get; set; }
            [GrandResourceDisplayName("Plugins.Payment.AllBank.BankInstallment.Field.NumberOfInstallment")]
            public int NumberOfInstallment { get; set; }
            [GrandResourceDisplayName("Plugins.Payment.AllBank.BankInstallment.Field.Percentage")]
            public decimal Percentage { get; set; }
            public IList<SelectListItem> AvailableBankPos { get; set; }
            public IList<SelectListItem> AvailableBanks { get; set; }
        }
        
    }
    

    
}

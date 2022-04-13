using System.Collections.Generic;
using Grand.Core.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Plugin.Payments.GarantiPay.Models.BankInstallments
{
    public class BankCategoryList:BaseEntityModel
    {
        public BankCategoryList()
        {
            Categories = new List<SelectListItem>();
        }
        public override string Id { get; set; }
        public string Name { get; set; }
        public string CategoryId { get; set; }
        public int MaxInstallment { get; set; }
        public OmniBankInstallmentCategoryModel BankInstallmentCategoryModel { get; set; }
        public IList<SelectListItem> Categories { get; set; }
    }
}
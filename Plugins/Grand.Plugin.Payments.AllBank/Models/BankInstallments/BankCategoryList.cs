using Grand.Core.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Plugin.Payments.AllBank.Models.BankInstallments
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
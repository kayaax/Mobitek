using Grand.Core.Models;
using Grand.Domain.Catalog;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;
using Grand.Core.ModelBinding;

namespace Grand.Plugin.Payments.AllBank.Models.BankInstallments
{
    public class OmniBankInstallmentCategoryModel : BaseEntityModel
    {
        
        public override string Id { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankInstallment.Field.Name")]
        public string Name { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankInstallment.Field.CategoryId")]
        public string CategoryId { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankInstallment.Field.MaxInstallment")]
        public int MaxInstallment { get; set; }

     
    }

    
}
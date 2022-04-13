using Grand.Core.ModelBinding;
using Grand.Core.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

using System.Collections.Generic;

namespace Grand.Plugin.Payments.AllBank.Models.BankOrder
{
    public class BankOrderListModel:BaseModel
    {
        public BankOrderListModel()
        {
            AvailableCustomers = new List<SelectListItem>();
           
        }
        [GrandResourceDisplayName("Admin.AllBank.PaymentBankOrder.List.Customer")]
       
        public string SearchCustomerId { get; set; }
        public IList<SelectListItem> AvailableCustomers { get; set; }
       
    }
}
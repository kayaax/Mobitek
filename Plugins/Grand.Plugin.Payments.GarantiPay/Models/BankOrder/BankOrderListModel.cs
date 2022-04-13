using System.Collections.Generic;
using Grand.Core.ModelBinding;
using Grand.Core.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Plugin.Payments.GarantiPay.Models.BankOrder
{
    public class BankOrderListModel:BaseModel
    {
        public BankOrderListModel()
        {
            AvailableCustomers = new List<SelectListItem>();
           
        }
        [GrandResourceDisplayName("Admin.GarantiPay.PaymentBankOrder.List.Customer")]
       
        public string SearchCustomerId { get; set; }
        public IList<SelectListItem> AvailableCustomers { get; set; }
       
    }
}
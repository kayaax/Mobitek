using Grand.Core.ModelBinding;
using Grand.Core.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Grand.Plugin.Payments.AllBank.Models.BankPoses
{
    public class OmniBankPosListModel:BaseModel
    {
        public OmniBankPosListModel()
        {
            AvailableBankList = new List<SelectListItem>();
            AvailableBankTypes = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.SearchBankId")]
        public string SearchBankId { get; set; }

        [GrandResourceDisplayName("Plugins.Payment.AllBank.BankPos.Field.SearchBankTypeId")]
        public int SearchBankTypeId { get; set; }
        public IList<SelectListItem> AvailableBankTypes { get; set; }
        public IList<SelectListItem> AvailableBankList { get; set; }

    }
}

using System.ComponentModel.DataAnnotations;

namespace Grand.Plugin.Payments.AllBank.Models.Enums
{

    public enum BankType
    {
       
        [Display(Name = "Banka")]
        Bank = 10,
        [Display(Name = "Ortak Ödeme")]
        Ortak = 20

    }
}

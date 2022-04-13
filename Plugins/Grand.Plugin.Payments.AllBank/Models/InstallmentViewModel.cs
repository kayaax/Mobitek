using Grand.Core;
using Grand.Core.Models;
using Grand.Domain.Orders;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Models.BankInstallments;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Grand.Plugin.Payments.AllBank.Models
{
    public class InstallmentViewModel : BaseModel
    {
        public InstallmentViewModel()
        {
            InstallmentRates = new List<InstallmentRate>();
            BankInstallmentCategories = new List<OmniBankInstallmentCategoryModel>();
        }
        public string Prefix { get; set; }
        public decimal TotalAmount { get; set; }
        public string BankOrderId { get; set; }
        public int BankId { get; set; }
        public string BankName { get; set; }
        public string BankLogo { get; set; }
        //credi-card yada banka kartı
        public string CardType { get; set; }
        public string CurrencyCode { get; set; }
        public int InstallmentCount { get; set; }
        public int CurrentInstallmentCount { get; set; }
        public bool InstallmentCategoryBased { get; set; }
        public string ErrorMessage { get; set; }
        public Guid PaymentInfoSession { get; set; }
        public string OrderNumber { get; set; }
        public OmniBankPos BankPos { get; set; }
        public OmniBankBin BankBin { get; set; }
        public string BankColor { get; set; }
        public string BankImageUrl { get; set; }
        public List<OmniBankInstallmentCategoryModel> BankInstallmentCategories { get; set; }
        public IList<ShoppingCartItem> ShoppingCartItems { get; set; }
        public List<InstallmentRate> InstallmentRates { get; set; }

        public void AddOrderNumber()
        {
            OrderNumber = CommonHelper.GenerateRandomDigitCode(10);
        }
        public void AddCashRate(decimal totalAmount)
        {
            InstallmentRates.Add(new InstallmentRate {
                Text = "Peşin",
                Installment = 1,
                Amount = totalAmount.ToString("0.##", new CultureInfo("en-EN")),
                AmountValue = totalAmount,
                TotalAmount = totalAmount.ToString("0.##", new CultureInfo("en-EN")),
                TotalAmountValue = totalAmount
            });
        }

        public class InstallmentRate
        {
            public string Text { get; set; }
            public int Installment { get; set; }
            public decimal Rate { get; set; }
            public string Amount { get; set; }
            public decimal AmountValue { get; set; }
            public string TotalAmount { get; set; }
            public decimal TotalAmountValue { get; set; }
        }
    }
}

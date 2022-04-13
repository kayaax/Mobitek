using System;
using System.Collections.Generic;
using System.Globalization;
using Grand.Core;
using Grand.Core.Models;
using Grand.Plugin.Payments.GarantiPay.Domain;
using Grand.Plugin.Payments.GarantiPay.Models.BankInstallments;
namespace Grand.Plugin.Payments.GarantiPay.Models
{
    public class InstallmentViewModel : BaseModel
    {
        public InstallmentViewModel()
        {
            InstallmentRates = new List<InstallmentRate>();
            BankInstallmentCategories = new List<OmniBankInstallmentCategoryModel>();
        }
        public decimal TotalAmount { get; set; }
        public string CurrencyCode { get; set; }
        public string ErrorMessage { get; set; }
        public Guid PaymentInfoSession { get; set; }
        public string OrderNumber { get; set; }
        public OmniGarantiPayPos BankPos { get; set; }
        public int NumberOfInstallments { get; set; }
        public bool Installment { get; set; }
        public decimal Total { get; set; }
        public string BankOrderId { get; set; }
        public string TestMode { get; set; }
        public List<InstallmentRate> InstallmentRates { get; set; }
        public List<OmniBankInstallmentCategoryModel> BankInstallmentCategories { get; set; }
        public void MarkGuid()
        {
            PaymentInfoSession = Guid.NewGuid();
        }
        public void AddOrderNumber()
        {
            OrderNumber = CommonHelper.GenerateRandomDigitCode(10);
        }
        

        public class InstallmentRate
        {
            
            public int Installment { get; set; }
            public decimal Rate { get; set; }
            public decimal AmountValue { get; set; }
            public decimal TotalAmountValue { get; set; }
        }


    }
}

using Grand.Core.ModelBinding;
using Grand.Core.Models;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Plugin.Payments.AllBank.Models
{
    public  class PaymentInfoModel:BaseModel
    {
        
        [GrandResourceDisplayName("Payment.CardholderName")]
        public string CardholderName { get; set; }

        [GrandResourceDisplayName("Payment.CardNumber")]
        public string CardNumber { get; set; }

        [GrandResourceDisplayName("Payment.ExpirationDate")]
        public string ExpireMonth { get; set; }

        [GrandResourceDisplayName("Payment.ExpirationDate")]
        public string ExpireYear { get; set; }
        
        [GrandResourceDisplayName("Payment.CardCode")]
        public string CardCode { get; set; }

        public int NumberOfInstallments { get; set; }
        public string DescriptionText { get; set; }
        public bool Installment { get; set; }
        [GrandResourceDisplayName("Payment.Total")]
        public decimal Total { get; set; }
        public string Errors { get; set; }
        public string BankCardTypeName { get; set; }
        public string BankOrderId { get; set; }
        public Guid PaymentInfoSession { get; set; }
        public string ExpirationDate { get; set; }
         public void MarkGuid()
         {
             PaymentInfoSession = Guid.NewGuid();
         }
    }
}
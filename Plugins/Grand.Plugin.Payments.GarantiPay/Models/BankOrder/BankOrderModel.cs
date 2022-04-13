using System;
using Grand.Core.Models;

namespace Grand.Plugin.Payments.GarantiPay.Models.BankOrder
{
    public class BankOrderModel: BaseEntityModel
    {
       
        public override string Id { get; set; }
        public string CustomerId { get; set; }
        public string CustomerEmail { get; set; }
        public string Hash { get; set; }
        public string Token { get; set; }
        public string OrderNumber { get; set; }
        public Guid OrderGuid { get; set; }
        public string TransactionNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public string UserIpAddress { get; set; }
        public string UserAgent { get; set; }
        public int Installment { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentAmount { get; set; }
        public string BankErrorMessage { get; set; }
        public DateTime? PaidDate { get; set; }
        public DateTime CreateDate { get; set; }
        public int StatusId { get; set; }
        public bool Deleted { get; set; }
        public string BankRequest { get; set; }
        public string BankResponse { get; set; }
        public string PaymentInfo { get; set; }
        public Guid PaymentInfoSession { get; set; }
        public Guid PaymentResultSession { get; set; }
    }
}
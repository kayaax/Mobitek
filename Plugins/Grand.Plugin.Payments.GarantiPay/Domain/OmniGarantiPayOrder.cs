using System;
using Grand.Domain;
using Grand.Domain.Payments;

namespace Grand.Plugin.Payments.GarantiPay.Domain
{
    public class OmniGarantiPayOrder:BaseEntity
    {
        public string CustomerId { get; set; }
        public string Hash { get; set; }
        public string Token { get; set; }
        public string OrderNumber { get; set; }
        public Guid OrderGuid { get; set; }
        public string TransactionNumber { get; set; }
        public string ReferenceNumber { get; set; }
        public string UserIpAddress { get; set; }
        public string UserAgent { get; set; }
        public int Installment { get; set; }
        public int ExtraInstallment { get; set; }
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
        public PaymentStatus Status {
            get => (PaymentStatus)StatusId;
            set => StatusId = (int)value;
        }
        public void MarkAsCreated()
        {
            CreateDate = DateTime.UtcNow;
            Status = PaymentStatus.Pending;
        }
        public void MarkAsPaid(DateTime paidDate)
        {
            PaidDate = paidDate;
            Status = PaymentStatus.Paid;
            BankErrorMessage = null;
        }
        public void MarkAsFailed(string bankErrorMessage, string bankResponse)
        {
            Status = PaymentStatus.Pending;
            BankErrorMessage = bankErrorMessage;
            BankResponse = bankResponse;
        }
    }
}
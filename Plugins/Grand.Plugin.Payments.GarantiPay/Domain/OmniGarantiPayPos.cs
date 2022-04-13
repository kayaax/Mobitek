using System.Collections.Generic;
using System;
using Grand.Domain;

namespace Grand.Plugin.Payments.GarantiPay.Domain
{
    public class OmniGarantiPayPos : BaseEntity
    {
        private ICollection<BankInstallment> _bankInstallments;
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<BankInstallment> BankInstallments
        {
            get => _bankInstallments ??= new List<BankInstallment>();
            protected set => _bankInstallments = value;
        }
       
    }
}
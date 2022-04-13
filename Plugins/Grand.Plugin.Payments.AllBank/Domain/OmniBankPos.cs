using Grand.Domain;
using Grand.Plugin.Payments.AllBank.Models.Enums;
using System;
using System.Collections.Generic;

namespace Grand.Plugin.Payments.AllBank.Domain
{

    public class OmniBankPos : BaseEntity
    {

        private ICollection<BankInstallment> _bankInstallments;
        public int BankTypeId { get; set; }
        public string Name { get; set; }
        public string SystemName { get; set; }
        public string PictureId { get; set; }
        public bool IsActive { get; set; }
        public bool PrimaryBank { get; set; }
        public bool Primary { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }

        public virtual ICollection<BankInstallment> BankInstallments {
            get => _bankInstallments ??= new List<BankInstallment>();
            protected set => _bankInstallments = value;
        }
        public BankType BankType {
            get => (BankType)BankTypeId;
            set => BankTypeId = (int)value;

        }

    }
}

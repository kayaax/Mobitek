using Grand.Core.ModelBinding;
using Grand.Core.Models;
using Grand.Plugin.Payments.AllBank.Models.BankPoses;
using Grand.Plugin.Payments.AllBank.Models.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Grand.Plugin.Payments.AllBank.Models
{
    public partial class ConfigurationModel : BaseModel
    {
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.DescriptionText")]
        [UIHint("RichEditor")]
        public string DescriptionText { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.IsInstallment")]
        public bool IsInstallment { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.TestMode")]
        public bool TestMode { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.AllBank.Configuration.InstallmentCategoryBased")]
        public bool InstallmentCategoryBased { get; set; }

    }
}

using System.ComponentModel.DataAnnotations;

using Grand.Core.ModelBinding;
using Grand.Core.Models;

namespace Grand.Plugin.Payments.GarantiPay.Models
{
    public class ConfigurationModel : BaseModel
    {
       
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.DescriptionText")]
        [UIHint("RichEditor")]
        public string DescriptionText { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.IsInstallment")]
        public bool IsInstallment { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }
        [GrandResourceDisplayName("Plugins.Payment.GarantiPay.Configuration.TestMode")]
        public bool TestMode { get; set; }
    }
}
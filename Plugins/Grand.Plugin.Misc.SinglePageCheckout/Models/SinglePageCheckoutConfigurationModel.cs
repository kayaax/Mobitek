using Grand.Core.ModelBinding;
using Grand.Core.Models;

namespace Grand.Plugin.Misc.SinglePageCheckout.Models
{
    public class SinglePageCheckoutConfigurationModel : BaseModel
    {
        [GrandResourceDisplayName("Plugins.Misc.SinglePageCheckout.Enabled")]
        public bool Enabled { get; set; }

    }
}

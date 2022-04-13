using Grand.Framework.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Plugin.Payments.GarantiPay.Components
{
    [ViewComponent(Name = PaymentGarantiPayDefaults.GARANTIPAY_VIEW_COMPONENT_NAME)]
    public class PaymentGarantiPayComponents : BaseViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.GarantiPay/Views/PaymentInfo.cshtml");
        }
    }
}
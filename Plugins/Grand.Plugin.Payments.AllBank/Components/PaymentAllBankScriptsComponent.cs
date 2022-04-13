using Grand.Framework.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Plugin.Payments.AllBank.Components
{
    [ViewComponent(Name = AllBankPaymentDefaults.PAYMENT_SCRIPT_VIEW_COMPONENT_NAME)]
    public class PaymentAllBankScriptsComponent:BaseViewComponent
    {
        public IViewComponentResult Invoke()
        {
            return View("~/Plugins/Payments.AllBank/Views/PaymentAllBankScripts.cshtml");
        }
    }
}
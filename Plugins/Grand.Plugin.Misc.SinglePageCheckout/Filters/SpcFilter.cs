using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Grand.Plugin.Misc.SinglePageCheckout.Filters
{
    public class SpcProvider : IFilterProvider
    {
        private readonly SinglePageCheckoutSettings _singlePageCheckoutSettings;
        public SpcProvider(SinglePageCheckoutSettings singlePageCheckoutSettings)
        {
            _singlePageCheckoutSettings = singlePageCheckoutSettings;
        }
        public void OnProvidersExecuted(FilterProviderContext context)
        {
            if (context.ActionContext.ActionDescriptor.RouteValues["controller"] == "Checkout" &&
                context.ActionContext.ActionDescriptor.RouteValues["action"] == "OnePageCheckout")
            {
                if(_singlePageCheckoutSettings.Enabled)
                    context.Results.Add(new FilterItem(new FilterDescriptor(new SpcFilter(), FilterScope.Global), new SpcFilter()));
            }
        }

        public void OnProvidersExecuting(FilterProviderContext context)
        {
            return;
        }

        public int Order => 2005;
    }

    public class SpcFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            context.Result = new RedirectToActionResult("OnePageCheckout", "SinglePageCheckout", new { });
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            return;
        }
    }
}
using Grand.Core.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Grand.Plugin.Misc.SinglePageCheckout
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapControllerRoute("CheckoutSinglePage",
                "singlepagecheckout",
                new { controller = "SinglePageCheckoutController", action = "OnePageCheckout" });

            routeBuilder.MapControllerRoute("CheckoutSinglePageConfiguration",
                "singlepagecheckoutconfiguration",
                new { controller = "SinglePageCheckoutConfigurationController", action = "Configure" });
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
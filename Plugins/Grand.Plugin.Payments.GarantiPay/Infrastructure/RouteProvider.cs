using Grand.Core.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Grand.Plugin.Payments.GarantiPay.Infrastructure
{
    public class RouteProvider:IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapControllerRoute("Plugin.Payments.GarantiPay.Success", "PaymentGarantiPay/success",
                new { controller = "PaymentGarantiPay", action = "Success" });
            routeBuilder.MapControllerRoute("Plugin.Payments.GarantiPay.Cancel", "PaymentGarantiPay/cancel",
                new { controller = "PaymentGarantiPay", action = "Cancel" });
            routeBuilder.MapControllerRoute("PaymentGarantiPayPos", "Admin/PaymentGarantiPayPos/List",
                new { controller = "PaymentGarantiPayPos", action = "List" });
            routeBuilder.MapControllerRoute("PaymentGarantiPayOrder", "Admin/PaymentGarantiPayOrder/List",
                new { controller = "PaymentGarantiPayOrder", action = "List" });

        }

        public int Priority => 0;
    }
}
using Grand.Core.Routing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Grand.Plugin.Payments.AllBank.Infrastructure
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder routeBuilder)
        {
            routeBuilder.MapControllerRoute("PaymentBankPos", "Admin/PaymentBankPos/List",
                new { controller = "PaymentBankPos", action = "List" }
            );
            routeBuilder.MapControllerRoute("PaymentBankBin", "Admin/PaymentBankBin/List",
                new { controller = "PaymentBankBin", action = "List" }
            );
            routeBuilder.MapControllerRoute("PaymentBank", "Admin/PaymentBank/List",
                new { controller = "PaymentBank", action = "List" });

            routeBuilder.MapControllerRoute("PaymentOrder", "Admin/PaymentBankOrder/List",
                new { controller = "PaymentBankOrder", action = "List" });

            routeBuilder.MapControllerRoute("PaymentHandler", "PaymentAllBank/ProcessPayment/{id}",
                new { controller = "PaymentAllBank", action = "ProcessPayment" }
            ); 
            routeBuilder.MapControllerRoute("PaymentHandlerPaytr", "PaymentAllBank/ProcessPaymentPaytr/{bankOrderId}",
                new { controller = "PaymentAllBank", action = "ProcessPaymentPaytr" }
            );
            routeBuilder.MapControllerRoute("PaymentHandlerGet", "PaymentAllBank/ProcessPaymentGet/{orderId}",
                new { controller = "PaymentAllBank", action = "ProcessPaymentGet" }
            );
            routeBuilder.MapControllerRoute("PaymentCallBack", "PaymentAllBank/CallBack/",
                new { controller = "PaymentAllBank", action = "CallBack" }
            );
            routeBuilder.MapControllerRoute("PaymentCart", "PaymentAllBank/ProcessPaymentCart/{message?}",
                new { controller = "PaymentAllBank", action = "ProcessPaymentCart" });
        }

        public int Priority {
            get {
                return 0;
            }
        }
    }
}

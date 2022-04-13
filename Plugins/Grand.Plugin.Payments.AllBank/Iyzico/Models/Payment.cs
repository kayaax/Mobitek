using Grand.Plugin.Payments.AllBank.Iyzico.Request;

namespace Grand.Plugin.Payments.AllBank.Iyzico.Models
{
    public class Payment : PaymentResource
    {
        public static Payment Create(CreatePaymentRequest request, Options options)
        {
            return RestHttpClient.Create().Post<Payment>(options.BaseUrl + "/payment/auth", GetHttpHeaders(request, options), request);
        }

        public static Payment Retrieve(RetrievePaymentRequest request, Options options)
        {
            return RestHttpClient.Create().Post<Payment>(options.BaseUrl + "/payment/detail", GetHttpHeaders(request, options), request);
        }
    }
}

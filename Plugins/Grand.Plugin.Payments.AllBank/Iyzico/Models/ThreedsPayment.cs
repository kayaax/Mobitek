using Grand.Plugin.Payments.AllBank.Iyzico.Request;

namespace Grand.Plugin.Payments.AllBank.Iyzico.Models
{
    public class ThreedsPayment : PaymentResource
    {
        public static ThreedsPayment Create(CreateThreedsPaymentRequest request, Options options)
        {
            return RestHttpClient.Create().Post<ThreedsPayment>(options.BaseUrl + "/payment/3dsecure/auth", GetHttpHeaders(request, options), request);
        }

        public static ThreedsPayment Retrieve(RetrievePaymentRequest request, Options options)
        {
            return RestHttpClient.Create().Post<ThreedsPayment>(options.BaseUrl + "/payment/detail", GetHttpHeaders(request, options), request);
        }
    }
}

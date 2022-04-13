using Grand.Plugin.Payments.AllBank.Models.Banks;
using Grand.Plugin.Payments.AllBank.Requests;
using Grand.Plugin.Payments.AllBank.Results;
using Grand.Plugin.Payments.AllBank.Services;
using Grand.Services.Catalog;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Grand.Plugin.Payments.AllBank.Models;
using Grand.Services.Orders;
using Newtonsoft.Json;

namespace Grand.Plugin.Payments.AllBank.Providers
{
    public class PaytrPaymentProvider : IPaymentProvider
    {
        private readonly IProductService _productService;
        private readonly IBankOrderService _bankOrderService;
        public PaytrPaymentProvider(IProductService productService,
            IBankOrderService bankOrderService)
        {
            _productService = productService;
            _bankOrderService = bankOrderService;
           
        }
        public async Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            var merchantId = request.BankParameters[nameof(PayTrModel.MerchantId)];
            var merchantKey = request.BankParameters[nameof(PayTrModel.MerchantKey)];
            var merchantSalt = request.BankParameters[nameof(PayTrModel.MerchantSalt)];
            var testMode = request.TestMode ? "1" : "0";
            var gatewayUrl = request.BankParameters[nameof(PayTrModel.GatewayUrl)];
            var currency = request.CurrencyIsoCode == "TRY" ? "TL" : request.CurrencyIsoCode;
            const string lang = "tr";
            var noInstallment = request.Installment == 1 ? "0" : request.Installment.ToString();
            var maxInstallment = request.Installment == 1 ? "0" : request.Installment.ToString();
            var installment = request.Installment == 1 ? "0" : request.Installment.ToString();
            const string paymentType = "card";

            List<object> basketList = new List<object>();
            foreach (var item in request.ShoppingCartItems)
            {
                var product = await _productService.GetProductById(item.ProductId);
                var b = new object[]
                {
                    product.Name, 
                    product.Price.ToString("0.00",new CultureInfo("en-EN")), 
                    item.Quantity
                };
                basketList.Add(b);

            }
            var basketJson = JsonConvert.SerializeObject(basketList,Formatting.Indented);
            Encoding encoding = new CustomEncoding();
            string userBasket = String.Concat(basketJson.Where(c => !Char.IsWhiteSpace(c)));
            string basketItems = WebUtility.HtmlEncode(userBasket);
            var totalAmount = Convert.ToDouble(request.TotalAmount).ToString("0.00",new CultureInfo("en-EN"));
            var ipAddress = GetPublicIp();
            try
            {
                string birlestir = string.Concat(merchantId, ipAddress, request.OrderNumber, request.Customer.Email, totalAmount, paymentType, installment, currency, testMode,"0", merchantSalt);
                HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(merchantKey));
                byte[] b = hmac.ComputeHash(encoding.GetBytes(birlestir));
                var paytrToken = Convert.ToBase64String(b);
                var parameters = new Dictionary<string, object>
                {
                    {"cc_owner", request.CardHolderName},
                    {"card_number", request.CardNumber},
                    {"expiry_month", AllBankHelper.EncodeExpireMonth(request.ExpireMonth)},
                    {"expiry_year", request.ExpireYear},
                    {"cvv", request.CvvCode},
                    {"merchant_id", merchantId},
                    {"user_ip", ipAddress},
                    {"merchant_oid", request.OrderNumber},
                    {"email", request.Customer.Email},
                    {"payment_type", paymentType},
                    {"payment_amount", totalAmount},
                    {"currency", currency},
                    {"test_mode", testMode},
                    {"non_3d", "0"},
                    {"merchant_ok_url",request.CallbackUrl},
                    {"merchant_fail_url", request.CallbackUrl},
                    {"user_name", request.Customer.BillingAddress.FirstName + " " + request.Customer.BillingAddress.LastName},
                    {"user_address", request.Customer.BillingAddress.Address1 + " " + request.Customer.BillingAddress.Address2},
                    {"user_phone", request.Customer.BillingAddress.PhoneNumber},
                    {"user_basket", basketItems},
                    {"debug_on", testMode},
                    {"no_installment",noInstallment},
                    {"max_installment",maxInstallment},
                    {"lang",lang},
                    {"paytr_token", paytrToken},
                    {"non3d_test_failed", "0"},
                    {"installment_count", installment},
                    {"card_type", request.CardFamily.ToLower()},
                };
                return await Task.FromResult(PaymentGatewayResult.Successed(parameters, gatewayUrl));
            }
            catch (Exception ex)
            {

                return await Task.FromResult(PaymentGatewayResult.Failed(ex.ToString()));
            }
        }

        public async Task<VerifyGatewayResult> VerifyGateway(VerifyGatewayRequest request, PaymentGatewayRequest gatewayRequest, IFormCollection form)
        {

            string status = form["status"];
            string total_amount = form["total_amount"];
            string hash = form["hash"];
            string merchant_oid = form["merchant_oid"];
            var bankOrder = await _bankOrderService.GetBankOrderOrderNumber(gatewayRequest.OrderNumber);
            var merchantKey = request.BankParameters[nameof(PayTrModel.MerchantKey)];
            var merchantSalt = request.BankParameters[nameof(PayTrModel.MerchantSalt)];
            string birlestir = string.Concat(merchant_oid, merchantSalt, status, total_amount);
            Encoding encoding = new CustomEncoding();
            HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(merchantKey));
            byte[] b = hmac.ComputeHash(encoding.GetBytes(birlestir));
            string token = Convert.ToBase64String(b);
            if (hash.ToString() != token)
            {
                return await Task.FromResult(VerifyGatewayResult.Failed("PAYTR notification failed: bad hash"));
            }
            if (status == "success")
            {
                var totalAmount = bankOrder.TotalAmount;
                return await Task.FromResult(VerifyGatewayResult.Successed(transactionId: merchant_oid,
                    referenceNumber: merchant_oid, gatewayRequest.Installment, amount: totalAmount.ToString("0.##", new CultureInfo("en-EN"))));
            }
            string failed_reason_msg = form["failed_reason_msg"];
            string failed_reason_code = form["failed_reason_code"];
            return await Task.FromResult(VerifyGatewayResult.Failed(failed_reason_msg, ErrorCodes[failed_reason_code]));
        }

        public Task<CancelPaymentResult> CancelRequest(CancelPaymentRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<RefundPaymentResult> RefundRequest(RefundPaymentRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<PaymentDetailResult> PaymentDetailRequest(PaymentDetailRequest request)
        {
            throw new System.NotImplementedException();
        }

        private static readonly IDictionary<string, string> ErrorCodes = new Dictionary<string, string>
        {
            {"0", "Kartın limiti / bakiyesi yetersiz"},
            {"1", "Müşteri, kimlik doğrulama adımında cep telefonu numarasını girmedi."},
            {"2", "Müşteri, cep telefonuna gelen şifreyi doğru girmedi."},
            {"3", "Müşterinin işlemi PayTR tarafından güvenlik kontrolünden geçemedi veya kontrol yapılamadı."},
            {"6", "Müşteri, kendisine tanınmış olan işlem süresinde (1. ADIM’da tanımlanan request_exp_date değeri) işlemini tamamlamadı."},
            {"8", "Müşterinin kullanmakta olduğu kart ile seçmiş olduğu taksitli ödeme yöntemi kullanılamaz."},
            {"9", "Müşterinin kullanmakta olduğu kart için mağazanızın işlem yetkisi bulunmuyor."},
            {"10", "Müşteri, yapmış olduğu işlemde 3D Secure ile ödeme yapmalıdır."},

        };

        private static string GetPublicIp()
        {
            var webClient = new WebClient();
            string dnsString = webClient.DownloadString("http://checkip.dyndns.org");
            dnsString = (new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")).Match(dnsString).Value;
            webClient.Dispose();
            return dnsString;
        }

        
    }
}

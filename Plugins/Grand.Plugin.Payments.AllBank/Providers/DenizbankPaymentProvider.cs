using Grand.Plugin.Payments.AllBank.Models.Banks;
using Grand.Plugin.Payments.AllBank.Requests;
using Grand.Plugin.Payments.AllBank.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;


namespace Grand.Plugin.Payments.AllBank.Providers
{
    public class DenizbankPaymentProvider : IPaymentProvider
    {
        private readonly HttpClient _client;

        public DenizbankPaymentProvider(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
        }
        public async Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                string shopCode = request.BankParameters[nameof(DenizBankModel.ShopCode)];
                string storeKey = request.BankParameters[nameof(DenizBankModel.StoreKey)];
                string txnType = "Auth";
                string secureType = "3DPay";
                string random = DateTime.Now.ToString();
                var parameters = new Dictionary<string, object>();
                parameters.Add("ShopCode", shopCode);
                parameters.Add("OrderId", request.OrderNumber); //sipariş numarası

                //işlem başarılı da olsa başarısız da olsa callback sayfasına yönlendirerek kendi tarafımızda işlem sonucunu kontrol ediyoruz
                parameters.Add("OkUrl", request.CallbackUrl); //başarılı dönüş adresi
                parameters.Add("FailUrl", request.CallbackUrl); //hatalı dönüş adresi
                parameters.Add("TxnType", txnType); //direk satış
                parameters.Add("Rnd", random); //rastgele bir sayı üretilmesi isteniyor

                parameters.Add("Currency", CurrencyCodes[request.CurrencyIsoCode]); //TL ISO code | EURO 978 | Dolar 840
                parameters.Add("Pan", request.CardNumber);
                parameters.Add("Expiry", $"{AllBankHelper.EncodeExpireMonth(request.ExpireMonth)}{request.ExpireYear}"); //kart bitiş ay-yıl birleşik
                parameters.Add("Cvv2", request.CvvCode); //kart güvenlik kodu
                parameters.Add("CartType",
                    CardAssociations[request.CardAssociation]); //kart tipi visa 1 | master 2 | amex 3
                parameters.Add("SecureType", secureType);
                parameters.Add("Lang", request.LanguageIsoCode.ToUpper()); //iki haneli dil iso kodu

                //kuruş ayrımı nokta olmalı!!!
                string totalAmount = request.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture.NumberFormat);
                parameters.Add("PurchAmount", totalAmount);

                string installment = request.Installment.ToString();
                if (request.Installment < 2 || request.ManufacturerCard) //üretici kart durumunda taksit boş olacak
                    installment = string.Empty; //0 veya 1 olması durumunda taksit bilgisini boş gönderiyoruz

                //üretici kartı taksit desteği
                if (request.ManufacturerCard && request.Installment > 1)
                {
                    string maturity = request.Installment.ToString();
                    parameters.Add("AgricultureTxnFlag", "T");
                    parameters.Add("PaymentFrequency", maturity);
                    parameters.Add("MaturityPeriod", maturity);
                }

                //normal taksit
                parameters.Add("InstallmentCount", installment); //taksit sayısı | 1 veya boş tek çekim olur
                parameters.Add("taksitsayisi", installment); //taksit sayısı | 1 veya boş tek çekim olur

                var hashBuilder = new StringBuilder();
                hashBuilder.Append(shopCode);
                hashBuilder.Append(request.OrderNumber);
                hashBuilder.Append(totalAmount);
                hashBuilder.Append(request.CallbackUrl);
                hashBuilder.Append(request.CallbackUrl);
                hashBuilder.Append(txnType);
                hashBuilder.Append(installment);
                hashBuilder.Append(random);
                //merchantpass
                hashBuilder.Append(storeKey);
                var hashData = GetSHA1(hashBuilder.ToString());
                parameters.Add("Hash", hashData); //hash data
                return await Task.FromResult(PaymentGatewayResult.Successed(parameters,
                    request.BankParameters[nameof(DenizBankModel.GatewayUrl)]));
            }
            catch (Exception ex)
            {

                return await Task.FromResult(PaymentGatewayResult.Failed(ex.ToString()));
            }
        }

        public async Task<VerifyGatewayResult> VerifyGateway(VerifyGatewayRequest request, PaymentGatewayRequest gatewayRequest, IFormCollection form)
        {
            if (form == null)
            {
                return await Task.FromResult(VerifyGatewayResult.Failed("Form verisi alınamadı."));
            }
            var mdStatus = form["mdStatus"];
            if (StringValues.IsNullOrEmpty(mdStatus))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed(form["mdErrorMsg"], form["ProcReturnCode"]));
            }
            var response = form["Response"];
            //mdstatus 1,2,3 veya 4 olursa 3D doğrulama geçildi anlamına geliyor
            if (!mdStatus.Equals("1") || !mdStatus.Equals("2") || !mdStatus.Equals("3") || !mdStatus.Equals("4"))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed($"{response} - {form["mdErrorMsg"]}",
                    form["ProcReturnCode"]));
            }
            if (StringValues.IsNullOrEmpty(response) || !response.Equals("Approved"))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed($"{response} - {form["ErrorMessage"]}",
                    form["ProcReturnCode"]));
            }

            var hashBuilder = new StringBuilder();
            hashBuilder.Append(request.BankParameters[nameof(DenizBankModel.ShopCode)]);
            hashBuilder.Append(form["Version"].FirstOrDefault());
            hashBuilder.Append(form["PurchAmount"].FirstOrDefault());
            hashBuilder.Append(form["Exponent"].FirstOrDefault());
            hashBuilder.Append(form["Currency"].FirstOrDefault());
            hashBuilder.Append(form["OkUrl"].FirstOrDefault());
            hashBuilder.Append(form["FailUrl"].FirstOrDefault());
            hashBuilder.Append(form["MD"].FirstOrDefault());
            hashBuilder.Append(form["OrderId"].FirstOrDefault());
            hashBuilder.Append(form["ProcReturnCode"].FirstOrDefault());
            hashBuilder.Append(form["Response"].FirstOrDefault());
            hashBuilder.Append(form["mdStatus"].FirstOrDefault());
            hashBuilder.Append(request.BankParameters[nameof(DenizBankModel.StoreKey)]);

            var hashData = GetSHA1(hashBuilder.ToString());
            if (!form["HASH"].Equals(hashData))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed("Güvenlik imza doğrulaması geçersiz."));
            }

            int.TryParse(form["taksitsayisi"], out int taksitSayisi);
            int.TryParse(form["EXTRA.ARTITAKSIT"], out int extraTaksitSayisi);

            return await Task.FromResult(VerifyGatewayResult.Successed(form["TransId"], form["AuthCode "],
                taksitSayisi, extraTaksitSayisi, response, form["ProcReturnCode"], amount: form["PurchAmount"]));
        }

        public async Task<CancelPaymentResult> CancelRequest(CancelPaymentRequest request)
        {
            string shopCode = request.BankParameters[nameof(DenizBankModel.ShopCode)];
            string userCode = request.BankParameters["cancelUserCode"];
            string userPass = request.BankParameters["cancelUserPass"];

            var formBuilder = new StringBuilder();
            formBuilder.AppendFormat("ShopCode={0}&", shopCode);
            formBuilder.AppendFormat("PurchAmount={0}&",
                request.TotalAmount.ToString("0.##", new CultureInfo("en-US")));
            formBuilder.AppendFormat("Currency={0}&", request.CurrencyIsoCode);
            formBuilder.Append("OrderId=&");
            formBuilder.Append("TxnType=Void&");
            formBuilder.AppendFormat("orgOrderId={0}&", request.OrderNumber);
            formBuilder.AppendFormat("UserCode={0}&", userCode);
            formBuilder.AppendFormat("UserPass={0}&", userPass);
            formBuilder.Append("SecureType=NonSecure&");
            formBuilder.AppendFormat("Lang={0}&", request.LanguageIsoCode.ToUpper());
            formBuilder.Append("MOTO=0");

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"],
                new StringContent(formBuilder.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded"));
            string responseContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseContent))
                return CancelPaymentResult.Failed("İptal işlemi başarısız.");

            responseContent = responseContent.Replace(";;", ";").Replace(";", "&");
            var responseParams = HttpUtility.ParseQueryString(responseContent);

            if (responseParams["ProcReturnCode"] != "00")
                return CancelPaymentResult.Failed(responseParams["ErrorMessage"]);

            return CancelPaymentResult.Successed(responseParams["TransId"], responseParams["TransId"]);
        }

        public async Task<RefundPaymentResult> RefundRequest(RefundPaymentRequest request)
        {
            string shopCode = request.BankParameters["shopCode"];
            string userCode = request.BankParameters["refundUserCode"];
            string userPass = request.BankParameters["refundUserPass"];

            var formBuilder = new StringBuilder();
            formBuilder.AppendFormat("ShopCode={0}&", shopCode);
            formBuilder.AppendFormat("PurchAmount={0}&",
                request.TotalAmount.ToString("0.##", new CultureInfo("en-US")));
            formBuilder.AppendFormat("Currency={0}&", request.CurrencyIsoCode);
            formBuilder.Append("OrderId=&");
            formBuilder.Append("TxnType=Refund&");
            formBuilder.AppendFormat("orgOrderId={0}&", request.OrderNumber);
            formBuilder.AppendFormat("UserCode={0}&", userCode);
            formBuilder.AppendFormat("UserPass={0}&", userPass);
            formBuilder.Append("SecureType=NonSecure&");
            formBuilder.AppendFormat("Lang={0}&", request.LanguageIsoCode.ToUpper());
            formBuilder.Append("MOTO=0");

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"],
                new StringContent(formBuilder.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded"));
            string responseContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseContent))
                return RefundPaymentResult.Failed("İade işlemi başarısız.");

            responseContent = responseContent.Replace(";;", ";").Replace(";", "&");
            var responseParams = HttpUtility.ParseQueryString(responseContent);

            if (responseParams["ProcReturnCode"] != "00")
                return RefundPaymentResult.Failed(responseParams["ErrorMessage"]);

            return RefundPaymentResult.Successed(responseParams["TransId"], responseParams["TransId"]);
        }

        public async Task<PaymentDetailResult> PaymentDetailRequest(PaymentDetailRequest request)
        {
            string shopCode = request.BankParameters["shopCode"];
            string userCode = request.BankParameters["userCode"];
            string userPass = request.BankParameters["userPass"];

            var formBuilder = new StringBuilder();
            formBuilder.AppendFormat("ShopCode={0}&", shopCode);
            formBuilder.AppendFormat("Currency={0}&", request.CurrencyIsoCode);
            formBuilder.Append("TxnType=StatusHistory&");
            formBuilder.AppendFormat("orgOrderId={0}&", request.OrderNumber);
            formBuilder.AppendFormat("UserCode={0}&", userCode);
            formBuilder.AppendFormat("UserPass={0}&", userPass);
            formBuilder.Append("SecureType=NonSecure&");
            formBuilder.AppendFormat("Lang={0}&", request.LanguageIsoCode.ToUpper());

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"],
                new StringContent(formBuilder.ToString(), Encoding.UTF8, "application/x-www-form-urlencoded"));
            string responseContent = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(responseContent))
                return PaymentDetailResult.FailedResult(errorMessage: "İade işlemi başarısız.");

            responseContent = responseContent.Replace(";;", ";").Replace(";", "&");
            var responseParams = HttpUtility.ParseQueryString(responseContent);

            if (responseParams["ProcReturnCode"] != "00")
                return PaymentDetailResult.FailedResult(errorMessage: responseParams["ErrorMessage"],
                    errorCode: responseParams["ErrorCode"]);

            return PaymentDetailResult.PaidResult(responseParams["TransId"], responseParams["TransId"]);
        }

        private string GetSHA1(string text)
        {
            var cryptoServiceProvider = new SHA1CryptoServiceProvider();
            var inputbytes = cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(text));
            var hashData = Convert.ToBase64String(inputbytes);

            return hashData;
        }

        private static readonly Dictionary<string, string> CurrencyCodes = new Dictionary<string, string>
        {
           { "TRY","949" },
            { "USD","840" },
            { "EUR","978" },
            { "GBP","826" }
        };

        private static readonly Dictionary<string, string> CardAssociations = new Dictionary<string, string>
        {
            {"VISA", "1"},
            {"MASTER_CARD", "2"},
            {"AMEX", "3"}
        };
    }
}

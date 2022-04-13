using Grand.Plugin.Payments.AllBank.Models.Banks;
using Grand.Plugin.Payments.AllBank.Requests;
using Grand.Plugin.Payments.AllBank.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Grand.Core;


namespace Grand.Plugin.Payments.AllBank.Providers
{
    public class FinansbankPaymentProvider : IPaymentProvider
    {
        private readonly HttpClient _client;

        public FinansbankPaymentProvider(IHttpClientFactory httpClientFactory)
        {

            _client = httpClientFactory.CreateClient();
        }
        public async Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                string mbrId = request.BankParameters[nameof(FinansBankModel.MbrId)];//Mağaza numarası
                string merchantId = request.BankParameters[nameof(FinansBankModel.MerchantId)];//Mağaza numarası
                string userCode = request.BankParameters[nameof(FinansBankModel.UserCode)];//
                string userPass = request.BankParameters[nameof(FinansBankModel.UserPass)];//Mağaza anahtarı
                string secureType = "3DPay";
                string merchantPass = request.BankParameters[nameof(FinansBankModel.MerchantPass)];
                string totalAmount = request.TotalAmount.ToString("##.00", new CultureInfo("en-US"));
                string orderNumber = request.OrderNumber;
                string random = CommonHelper.GenerateRandomDigitCode(10);

                var parameters = new Dictionary<string, object>
                {
                    {"MbrId", mbrId},
                    {"MerchantId", merchantId},
                    {"UserCode", userCode},
                    {"UserPass", userPass},
                    {"PurchAmount", totalAmount},
                    {"Currency", CurrencyCodes[request.CurrencyIsoCode]},
                    {"OrderId", orderNumber},
                    {"TxnType", "Auth"},
                    {"SecureType", secureType},
                    {"Pan", request.CardNumber},
                    {"Expiry", $"{AllBankHelper.EncodeExpireMonth(request.ExpireMonth)}{request.ExpireYear}"},
                    {"Cvv2", request.CvvCode},
                    {"Lang", request.LanguageIsoCode},
                    {"rnd", random},
                    {"OkUrl", request.CallbackUrl},
                    {"FailUrl", request.CallbackUrl}
                };
                //kuruş ayrımı nokta olmalı!!!
                //TL:949, USD:840, EUR:978
                //sipariş numarası
                //direk satış
                //NonSecure, 3Dpay, 3DModel, 3DHost
                //kart numarası
                //kart bitiş ay-yıl birleşik
                //kart güvenlik kodu
                //iki haneli dil iso kodu
                //işlem başarılı da olsa başarısız da olsa callback sayfasına yönlendirerek kendi tarafımızda işlem sonucunu kontrol ediyoruz
                //başarılı dönüş adresi
                //hatalı dönüş adresi

                string installment = request.Installment.ToString();
                if (request.Installment < 2)
                    installment = string.Empty;//0 veya 1 olması durumunda taksit bilgisini boş gönderiyoruz

                parameters.Add("InstallmentCount", installment);//taksit sayısı | 0, 1 veya boş tek çekim olur

                String str = mbrId + orderNumber + request.TotalAmount + request.CallbackUrl + request.CallbackUrl + "Auth" + request.Installment + random + merchantPass;
                System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1CryptoServiceProvider();
                byte[] bytes = Encoding.ASCII.GetBytes(str);
                byte[] hashingBytes = sha.ComputeHash(bytes);
                String hash = Convert.ToBase64String(hashingBytes);
                parameters.Add("hash", hash);


                return await Task.FromResult(PaymentGatewayResult.Successed(parameters, request.BankParameters[nameof(FinansBankModel.GatewayUrl)]));
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

                return await Task.FromResult(VerifyGatewayResult.Failed($"{response} - {form["mdErrorMsg"]}", form["ProcReturnCode"]));
            }

            if (StringValues.IsNullOrEmpty(response) || !response.Equals("Approved"))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed($"{response} - {form["ErrorMessage"]}", form["ProcReturnCode"]));
            }

            int.TryParse(form["taksitsayisi"], out int taksitSayisi);

            return await Task.FromResult(VerifyGatewayResult.Successed(form["TransId"], form["AuthCode"],
                taksitSayisi, 0, response, form["ProcReturnCode"], amount: form["PurchAmount"]));
        }

        public async Task<CancelPaymentResult> CancelRequest(CancelPaymentRequest request)
        {
            string mbrId = request.BankParameters["mbrId"];//Mağaza numarası
            string merchantId = request.BankParameters["merchantId"];//Mağaza numarası
            string userCode = request.BankParameters["userCode"];//
            string userPass = request.BankParameters["userPass"];//Mağaza anahtarı


            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
                                    <PayforIptal>
                                        <MbrId>{mbrId}</MbrId>
                                        <MerchantID>{merchantId}</MerchantID>
                                        <UserCode>{userCode}</UserCode>
                                        <UserPass>{userPass}</UserPass>
                                        <OrgOrderId>{request.OrderNumber}</OrgOrderId>
                                        <SecureType>NonSecure</SecureType>
                                        <TxnType>Void</TxnType>
                                        <Currency>{request.CurrencyIsoCode}</Currency>
                                        <Lang>{request.LanguageIsoCode.ToUpper()}</Lang>
                                    </PayforIptal>";

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
            string responseContent = await response.Content.ReadAsStringAsync();

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            //TODO Finansbank response
            //if (xmlDocument.SelectSingleNode("VposResponse/ResultCode") == null ||
            //    xmlDocument.SelectSingleNode("VposResponse/ResultCode").InnerText != "0000")
            //{
            //    string errorMessage = xmlDocument.SelectSingleNode("VposResponse/ResultDetail")?.InnerText ?? string.Empty;
            //    if (string.IsNullOrEmpty(errorMessage))
            //        errorMessage = "Bankadan hata mesajı alınamadı.";

            //    return CancelPaymentResult.Failed(errorMessage);
            //}

            var transactionId = xmlDocument.SelectSingleNode("VposResponse/TransactionId")?.InnerText;
            return CancelPaymentResult.Successed(transactionId, transactionId);
        }

        public async Task<RefundPaymentResult> RefundRequest(RefundPaymentRequest request)
        {
            string mbrId = request.BankParameters["mbrId"];//Mağaza numarası
            string merchantId = request.BankParameters["merchantId"];//Mağaza numarası
            string userCode = request.BankParameters["userCode"];//
            string userPass = request.BankParameters["userPass"];//Mağaza anahtarı
            string totalAmount = request.TotalAmount.ToString("0:##", new CultureInfo("en-US"));

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8"" standalone=""yes""?>
                                    <PayforIade>
                                        <MbrId>{mbrId}</MbrId>
                                        <MerchantID>{merchantId}</MerchantID>
                                        <UserCode>{userCode}</UserCode>
                                        <UserPass>{userPass}</UserPass>
                                        <OrgOrderId>{request.OrderNumber}</OrgOrderId>
                                        <SecureType>NonSecure</SecureType>
                                        <TxnType>Refund</TxnType>
                                        <PurchAmount>{totalAmount}</PurchAmount>
                                        <Currency>{request.CurrencyIsoCode}</Currency>
                                        <Lang>{request.LanguageIsoCode.ToUpper()}</Lang>
                                    </PayforIade>";

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
            string responseContent = await response.Content.ReadAsStringAsync();

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            //TODO Finansbank response
            //if (xmlDocument.SelectSingleNode("VposResponse/ResultCode") == null ||
            //    xmlDocument.SelectSingleNode("VposResponse/ResultCode").InnerText != "0000")
            //{
            //    string errorMessage = xmlDocument.SelectSingleNode("VposResponse/ResultDetail")?.InnerText ?? string.Empty;
            //    if (string.IsNullOrEmpty(errorMessage))
            //        errorMessage = "Bankadan hata mesajı alınamadı.";

            //    return RefundPaymentResult.Failed(errorMessage);
            //}

            var transactionId = xmlDocument.SelectSingleNode("VposResponse/TransactionId")?.InnerText;
            return RefundPaymentResult.Successed(transactionId, transactionId);
        }

        public Task<PaymentDetailResult> PaymentDetailRequest(PaymentDetailRequest request)
        {
            throw new NotImplementedException();
        }
        private static readonly Dictionary<string, string> CurrencyCodes = new Dictionary<string, string>
        {
            { "TRY","949" },
            { "USD","840" },
            { "EUR","978" },
            { "GBP","826" }
        };

    }
}

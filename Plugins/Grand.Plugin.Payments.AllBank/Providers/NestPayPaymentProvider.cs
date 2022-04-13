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
using System.Xml;


namespace Grand.Plugin.Payments.AllBank.Providers
{
    public class NestPayPaymentProvider : IPaymentProvider
    {
        private readonly HttpClient _client;
        public NestPayPaymentProvider(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
        }

        public async Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                string clientId = request.BankParameters[nameof(NestPayModel.ClientId)];
                string processType = "Auth";
                string storeKey = request.BankParameters[nameof(NestPayModel.StoreKey)];
                string storeType = request.BankParameters[nameof(NestPayModel.StoreType)];
                string random = DateTime.Now.ToString();

                var parameters = new Dictionary<string, object>();
                parameters.Add("clientid", clientId);
                parameters.Add("oid", request.OrderNumber);//sipariş numarası

                if (!request.CommonPaymentPage)
                {
                    parameters.Add("pan", request.CardNumber);
                    parameters.Add("cardHolderName", request.CardHolderName);
                    parameters.Add("Ecom_Payment_Card_ExpDate_Month", AllBankHelper.EncodeExpireMonth(request.ExpireMonth));//kart bitiş ay'ı
                    parameters.Add("Ecom_Payment_Card_ExpDate_Year", request.ExpireYear);//kart bitiş yıl'ı
                    parameters.Add("cv2", request.CvvCode);//kart güvenlik kodu
                    parameters.Add("cardType", CardAssociations[request.CardAssociation]);//kart tipi visa 1 | master 2 | amex 3
                }

                //işlem başarılı da olsa başarısız da olsa callback sayfasına yönlendirerek kendi tarafımızda işlem sonucunu kontrol ediyoruz
                parameters.Add("okUrl", request.CallbackUrl);//başarılı dönüş adresi
                parameters.Add("failUrl", request.CallbackUrl);//hatalı dönüş adresi
                parameters.Add("islemtipi", processType);//direk satış
                parameters.Add("rnd", random);//rastgele bir sayı üretilmesi isteniyor
                parameters.Add("currency", CurrencyCodes[request.CurrencyIsoCode]);//ISO code TL 949 | EURO 978 | Dolar 840
                parameters.Add("storetype", storeType);
                parameters.Add("lang", request.LanguageIsoCode);//iki haneli dil iso kodu

                //kuruş ayrımı nokta olmalı!!!
                string totalAmount = request.TotalAmount.ToString("0.##", new CultureInfo("en-US"));
                parameters.Add("amount", totalAmount);

                string installment = request.Installment.ToString();
                if (request.Installment < 2 || request.ManufacturerCard)//imece kart durumunda taksit boş olacak
                    installment = string.Empty;//0 veya 1 olması durumunda taksit bilgisini boş gönderiyoruz

                //üretici kartı taksit desteği
                if (request.ManufacturerCard && request.Installment > 1)
                {
                    string ertelemeDonemSayisi = request.Installment.ToString();
                    parameters.Add("IMCKOD", request.BankParameters[nameof(NestPayModel.ImcKod)]);
                    parameters.Add("FDONEM", ertelemeDonemSayisi);
                }

                //normal taksit
                parameters.Add("taksit", installment);//taksit sayısı | 1 veya boş tek çekim olur

                var hashBuilder = new StringBuilder();
                hashBuilder.Append(clientId);
                hashBuilder.Append(request.OrderNumber);
                hashBuilder.Append(totalAmount);
                hashBuilder.Append(request.CallbackUrl);
                hashBuilder.Append(request.CallbackUrl);
                hashBuilder.Append(processType);
                hashBuilder.Append(installment);
                hashBuilder.Append(random);
                hashBuilder.Append(storeKey);

                var hashData = GetSHA1(hashBuilder.ToString());
                parameters.Add("hash", hashData);//hash data

                return await Task.FromResult(PaymentGatewayResult.Successed(parameters, request.BankParameters[nameof(NestPayModel.GatewayUrl)]));
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
            var formResult = form.Keys.ToDictionary(key => key, value => form[value].ToString().Split(",", StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault());

            var mdStatus = formResult["mdStatus"];
            if (string.IsNullOrEmpty(mdStatus))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed(formResult["mdErrorMsg"], formResult["ProcReturnCode"]));
            }
            var response = formResult["Response"];
            //mdstatus 1,2,3 veya 4 olursa 3D doğrulama geçildi anlamına geliyor
            if (!mdStatusCodes.Contains(mdStatus))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed($"{response} - {formResult["mdErrorMsg"]}", formResult["ProcReturnCode"]));
            }

            if (string.IsNullOrEmpty(response) || !response.Equals("Approved"))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed($"{response} - {formResult["mdErrorMsg"]}", formResult["ProcReturnCode"]));
            }

            var parameters = formResult["HASHPARAMS"].Split(":", StringSplitOptions.RemoveEmptyEntries);
            var paramsval = "";
            foreach (var parameter in parameters)
            {
                if (formResult.ContainsKey(parameter))
                {
                    paramsval += formResult[parameter];
                }
            }

            var hashval = paramsval + request.BankParameters[nameof(NestPayModel.StoreKey)].Trim();
            var hash = GetSHA1(hashval);
            if (!formResult["HASH"].Equals(hash))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed("Güvenlik imza doğrulaması geçersiz."));

            }
            var extraInstallment = 0;
            //if (formResult.TryGetValue("EXTRA.HOSTMSG", out var extraTaksit) && !StringValues.IsNullOrEmpty(extraTaksit))
            //{
            //    extraInstallment = int.Parse(extraTaksit);
            //}

            int.TryParse(formResult["taksit"], out int installment);
            return await Task.FromResult(VerifyGatewayResult.Successed(formResult["TransId"], formResult["TransId"],
                installment, extraInstallment, response, formResult["ProcReturnCode"], amount: formResult["amount"]));
        }

        public async Task<CancelPaymentResult> CancelRequest(CancelPaymentRequest request)
        {
            string clientId = request.BankParameters["clientId"];
            string userName = request.BankParameters["cancelUsername"];
            string password = request.BankParameters["cancelUserPassword"];

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <CC5Request>
                                      <Name>{userName}</Name>
                                      <Password>{password}</Password>
                                      <ClientId>{clientId}</ClientId>
                                      <Type>Void</Type>
                                      <OrderId>{request.OrderNumber}</OrderId>
                                    </CC5Request>";

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
            string responseContent = await response.Content.ReadAsStringAsync();

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            if (xmlDocument.SelectSingleNode("CC5Response/Response") == null ||
                xmlDocument.SelectSingleNode("CC5Response/Response").InnerText != "Approved")
            {
                var errorMessage = xmlDocument.SelectSingleNode("CC5Response/ErrMsg")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return CancelPaymentResult.Failed(errorMessage);
            }

            if (xmlDocument.SelectSingleNode("CC5Response/ProcReturnCode") == null ||
                xmlDocument.SelectSingleNode("CC5Response/ProcReturnCode").InnerText != "00")
            {
                var errorMessage = xmlDocument.SelectSingleNode("CC5Response/ErrMsg")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return CancelPaymentResult.Failed(errorMessage);
            }

            var transactionId = xmlDocument.SelectSingleNode("CC5Response/TransId")?.InnerText ?? string.Empty;
            return CancelPaymentResult.Successed(transactionId, transactionId);
        }

        public async Task<RefundPaymentResult> RefundRequest(RefundPaymentRequest request)
        {
            string clientId = request.BankParameters["clientId"];
            string userName = request.BankParameters["refundUsername"];
            string password = request.BankParameters["refundUserPassword"];

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <CC5Request>
                                      <Name>{userName}</Name>
                                      <Password>{password}</Password>
                                      <ClientId>{clientId}</ClientId>
                                      <Type>Credit</Type>
                                      <OrderId>{request.OrderNumber}</OrderId>
                                    </CC5Request>";

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
            string responseContent = await response.Content.ReadAsStringAsync();

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            if (xmlDocument.SelectSingleNode("CC5Response/Response") == null ||
                xmlDocument.SelectSingleNode("CC5Response/Response").InnerText != "Approved")
            {
                var errorMessage = xmlDocument.SelectSingleNode("CC5Response/ErrMsg")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return RefundPaymentResult.Failed(errorMessage);
            }

            if (xmlDocument.SelectSingleNode("CC5Response/ProcReturnCode") == null ||
                xmlDocument.SelectSingleNode("CC5Response/ProcReturnCode").InnerText != "00")
            {
                var errorMessage = xmlDocument.SelectSingleNode("CC5Response/ErrMsg")?.InnerText ?? string.Empty;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesajı alınamadı.";

                return RefundPaymentResult.Failed(errorMessage);
            }

            var transactionId = xmlDocument.SelectSingleNode("CC5Response/TransId")?.InnerText ?? string.Empty;
            return RefundPaymentResult.Successed(transactionId, transactionId);
        }

        public async Task<PaymentDetailResult> PaymentDetailRequest(PaymentDetailRequest request)
        {
            string clientId = request.BankParameters["clientId"];
            string userName = request.BankParameters["userName"];
            string password = request.BankParameters["password"];

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                    <CC5Request>
                                        <Name>{userName}</Name>
                                        <Password>{password}</Password>
                                        <ClientId>{clientId}</ClientId>
                                        <OrderId>{request.OrderNumber}</OrderId>
                                        <Extra>
                                            <ORDERDETAIL>QUERY</ORDERDETAIL>
                                        </Extra>
                                    </CC5Request>";

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
            string responseContent = await response.Content.ReadAsStringAsync();

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            string finalStatus = xmlDocument.SelectSingleNode("CC5Response/Extra/ORDER_FINAL_STATUS")?.InnerText ?? string.Empty;
            string transactionId = xmlDocument.SelectSingleNode("CC5Response/Extra/TRX_1_TRAN_UID")?.InnerText;
            string referenceNumber = xmlDocument.SelectSingleNode("CC5Response/Extra/TRX_1_TRAN_UID")?.InnerText;
            string cardPrefix = xmlDocument.SelectSingleNode("CC5Response/Extra/TRX_1_CARDBIN")?.InnerText;
            int.TryParse(cardPrefix, out int cardPrefixValue);

            string installment = xmlDocument.SelectSingleNode("CC5Response/Extra/TRX_1_INSTALMENT")?.InnerText ?? "0";
            string bankMessage = xmlDocument.SelectSingleNode("CC5Response/Response")?.InnerText;
            string responseCode = xmlDocument.SelectSingleNode("CC5Response/ProcReturnCode")?.InnerText;

            if (finalStatus.Equals("SALE", StringComparison.OrdinalIgnoreCase))
            {
                int.TryParse(installment, out int installmentValue);
                return PaymentDetailResult.PaidResult(transactionId, referenceNumber, cardPrefixValue.ToString(), installmentValue, 0, bankMessage, responseCode);
            }
            else if (finalStatus.Equals("VOID", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentDetailResult.CanceledResult(transactionId, referenceNumber, bankMessage, responseCode);
            }
            else if (finalStatus.Equals("REFUND", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentDetailResult.RefundedResult(transactionId, referenceNumber, bankMessage, responseCode);
            }

            var errorMessage = xmlDocument.SelectSingleNode("CC5Response/ErrMsg")?.InnerText ?? string.Empty;
            if (string.IsNullOrEmpty(errorMessage))
                errorMessage = "Bankadan hata mesajı alınamadı.";

            return PaymentDetailResult.FailedResult(errorMessage: errorMessage);
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
            {"AMEX", "3"},
        };
        private string GetSHA1(string text)
        {
            var cryptoServiceProvider = new SHA1CryptoServiceProvider();
            var inputbytes = cryptoServiceProvider.ComputeHash(Encoding.UTF8.GetBytes(text));
            var hashData = Convert.ToBase64String(inputbytes);
            return hashData;
        }

        private static readonly string[] mdStatusCodes = new[] { "1", "2", "3", "4" };
    }
}

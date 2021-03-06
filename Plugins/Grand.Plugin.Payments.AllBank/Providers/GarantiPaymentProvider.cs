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
    public class GarantiPaymentProvider : IPaymentProvider
    {
        private readonly HttpClient _client;

        public GarantiPaymentProvider(IHttpClientFactory httpClientFactory)
        {

            _client = httpClientFactory.CreateClient();
        }

        public async Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {
                string terminalId = request.BankParameters[nameof(GarantiBankModel.TerminalId)];
                string terminalUserId = request.BankParameters[nameof(GarantiBankModel.TerminalUserId)];
                string terminalMerchantId = request.BankParameters[nameof(GarantiBankModel.TerminalMerchantId)];
                string terminalProvUserId = request.BankParameters[nameof(GarantiBankModel.TerminalProvUserId)];
                string terminalProvPassword = request.BankParameters[nameof(GarantiBankModel.TerminalProvPassword)];
                string storeKey = request.BankParameters[nameof(GarantiBankModel.StoreKey)];
                string mode = request.TestMode ? "TEST" : "PROD";
                string email = request.BankParameters[nameof(GarantiBankModel.Email)];
                string type = "sales";
                string secure3dSecurityLevel = "3D_FULL";

                var parameters = new Dictionary<string, object>();

                if (!request.CommonPaymentPage)
                {
                    parameters.Add("cardnumber", request.CardNumber);
                    parameters.Add("cardcvv2", request.CvvCode);//kart g??venlik kodu
                    parameters.Add("cardexpiredatemonth", AllBankHelper.EncodeExpireMonth(request.ExpireMonth));//kart biti?? ay'??
                    parameters.Add("cardexpiredateyear", request.ExpireYear);//kart biti?? y??l'??
                }

                parameters.Add("secure3dsecuritylevel", secure3dSecurityLevel);//SMS onayl?? ??deme modeli 3DPay olarak adland??r??l??yor.
                parameters.Add("mode", mode);
                parameters.Add("apiversion", "v0.01");
                parameters.Add("terminalprovuserid", terminalProvUserId);
                parameters.Add("terminaluserid", terminalUserId);
                parameters.Add("terminalmerchantid", terminalMerchantId);
                parameters.Add("terminalid", terminalId);
                parameters.Add("txntype", type);//direk sat????
                parameters.Add("txncurrencycode", CurrencyCodes[request.CurrencyIsoCode]);//TL ISO code | EURO 978 | Dolar 840
                parameters.Add("lang", request.LanguageIsoCode);
                parameters.Add("motoind", "N");
                parameters.Add("customeripaddress", request.CustomerIpAddress);
                parameters.Add("orderaddressname1", request.CardHolderName);
                parameters.Add("orderid", request.OrderNumber);//sipari?? numaras??
                parameters.Add("customeremailaddress", email);

                //i??lem ba??ar??l?? da olsa ba??ar??s??z da olsa callback sayfas??na y??nlendirerek kendi taraf??m??zda i??lem sonucunu kontrol ediyoruz
                parameters.Add("successurl", request.CallbackUrl);//ba??ar??l?? d??n???? adresi
                parameters.Add("errorurl", request.CallbackUrl);//hatal?? d??n???? adresi

                //garanti bankas??nda tutar bilgisinde nokta, virg??l gibi de??erler istenmiyor. 1.10 TL'lik i??lem 110 olarak g??nderilmeli. Yani tutar?? 100 ile ??arpabiliriz.
                string amount = (request.TotalAmount * 100m).ToString("0.##", new CultureInfo("en-US"));//virg??lden sonraki s??f??rlara gerek yok
                parameters.Add("txnamount", amount);

                string installment = request.Installment.ToString();
                if (request.Installment < 2)
                    installment = string.Empty;//0 veya 1 olmas?? durumunda taksit bilgisini bo?? g??nderiyoruz

                parameters.Add("txninstallmentcount", installment);//taksit say??s?? | bo?? tek ??ekim olur

                var hashBuilder = new StringBuilder();
                hashBuilder.Append(terminalId);
                hashBuilder.Append(request.OrderNumber);
                hashBuilder.Append(amount);
                hashBuilder.Append(request.CallbackUrl);
                hashBuilder.Append(request.CallbackUrl);
                hashBuilder.Append(type);
                hashBuilder.Append(installment);
                hashBuilder.Append(storeKey);

                //garanti taraf??ndan terminal numaras??n?? 9 haneye tamamlamak i??in ba????na s??f??r eklenmesi isteniyor.
                string _terminalid = string.Format("{0:000000000}", int.Parse(terminalId));

                //provizyon ??ifresi ve 9 haneli terminal numaras??n??n birle??imi ile bir hash olu??turuluyor
                string securityData = GetSHA1($"{terminalProvPassword}{_terminalid}");
                hashBuilder.Append(securityData);

                var hashData = GetSHA1(hashBuilder.ToString());
                parameters.Add("secure3dhash", hashData);

                return await Task.FromResult(PaymentGatewayResult.Successed(parameters, request.BankParameters[nameof(GarantiBankModel.GatewayUrl)]));
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
                return await Task.FromResult(VerifyGatewayResult.Failed("Form verisi al??namad??."));
            }

            var mdStatus = form["mdstatus"].ToString();
            if (string.IsNullOrEmpty(mdStatus))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed(form["mderrormessage"], form["procreturncode"]));
            }

            var response = form["response"];
            //mdstatus 1,2,3 veya 4 olursa 3D do??rulama ge??ildi anlam??na geliyor
            if (!mdStatusCodes.Contains(mdStatus))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed($"{response} - {form["mderrormessage"]}", form["procreturncode"]));
            }

            if (StringValues.IsNullOrEmpty(response) || !response.Equals("Approved"))
            {
                return await Task.FromResult(VerifyGatewayResult.Failed($"{response} - {form["errmsg"]}", form["procreturncode"]));
            }

            var hashBuilder = new StringBuilder();
            hashBuilder.Append(form["clientid"].FirstOrDefault());
            hashBuilder.Append(form["oid"].FirstOrDefault());
            hashBuilder.Append(form["authcode"].FirstOrDefault());
            hashBuilder.Append(form["procreturncode"].FirstOrDefault());
            hashBuilder.Append(form["response"].FirstOrDefault());
            hashBuilder.Append(form["mdstatus"].FirstOrDefault());
            hashBuilder.Append(form["cavv"].FirstOrDefault());
            hashBuilder.Append(form["eci"].FirstOrDefault());
            hashBuilder.Append(form["md"].FirstOrDefault());
            hashBuilder.Append(form["rnd"].FirstOrDefault());
            hashBuilder.Append(request.BankParameters[nameof(GarantiBankModel.StoreKey)]);

            var cryptoServiceProvider = new SHA1CryptoServiceProvider();
            var inputbytes = cryptoServiceProvider.ComputeHash(Encoding.GetEncoding("ISO-8859-9").GetBytes(hashBuilder.ToString()));
            var hashData = Convert.ToBase64String(inputbytes);

            if (!form["HASH"].Equals(hashData))
            {

                return await Task.FromResult(VerifyGatewayResult.Failed("G??venlik imza do??rulamas?? ge??ersiz."));
            }

            int.TryParse(form["txninstallmentcount"], out int installment);

            return await Task.FromResult(VerifyGatewayResult.Successed(form["transid"], form["hostrefnum"],
                installment, 0, response, form["procreturncode"], form["campaignchooselink"], amount: form["txnamount"]));
        }

        public async Task<CancelPaymentResult> CancelRequest(CancelPaymentRequest request)
        {
            string terminalUserId = request.BankParameters["terminalUserId"];
            string terminalId = request.BankParameters["terminalId"];
            string terminalMerchantId = request.BankParameters["terminalMerchantId"];
            string cancelUserId = request.BankParameters["cancelUserId"];
            string cancelUserPassword = request.BankParameters["cancelUserPassword"];
            string mode = request.TestMode ? "TEST" : "PROD";

            //garanti taraf??ndan terminal numaras??n?? 9 haneye tamamlamak i??in ba????na s??f??r eklenmesi isteniyor.
            string _terminalid = string.Format("{0:000000000}", int.Parse(terminalId));

            //garanti bankas??nda tutar bilgisinde nokta, virg??l gibi de??erler istenmiyor. 1.10 TL'lik i??lem 110 olarak g??nderilmeli. Yani tutar?? 100 ile ??arpabiliriz.
            string amount = (request.TotalAmount * 100m).ToString("0.##", new CultureInfo("en-US"));//virg??lden sonraki s??f??rlara gerek yok

            //provizyon ??ifresi ve 9 haneli terminal numaras??n??n birle??imi ile bir hash olu??turuluyor
            string securityData = GetSHA1($"{cancelUserPassword}{_terminalid}");

            //ilgili veriler birle??tirilip hash olu??turuluyor
            string hashstr = GetSHA1($"{request.OrderNumber}{terminalId}{amount}{securityData}");

            string installment = request.Installment.ToString();
            if (request.Installment < 2)
                installment = string.Empty;//0 veya 1 olmas?? durumunda taksit bilgisini bo?? g??nderiyoruz

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                        <GVPSRequest>
                                            <Mode>{mode}</Mode>
                                            <Version>v0.01</Version>
                                            <ChannelCode></ChannelCode>
                                            <Terminal>
                                                <ProvUserID>{cancelUserId}</ProvUserID>
                                                <HashData>{hashstr}</HashData>
                                                <UserID>{terminalUserId}</UserID>
                                                <ID>{terminalId}</ID>
                                                <MerchantID>{terminalMerchantId}</MerchantID>
                                            </Terminal>
                                            <Customer>
                                                <IPAddress>{request.CustomerIpAddress}</IPAddress>
                                                <EmailAddress></EmailAddress>
                                            </Customer>
                                            <Order>
                                                <OrderID>{request.OrderNumber}</OrderID>
                                                <GroupID></GroupID>
                                            </Order>
                                            <Transaction>
                                                <Type>void</Type>
                                                <InstallmentCnt>{installment}</InstallmentCnt>
                                                <Amount>{amount}</Amount>
                                                <CurrencyCode>{request.CurrencyIsoCode}</CurrencyCode>
                                                <CardholderPresentCode>0</CardholderPresentCode>
                                                <MotoInd>N</MotoInd>
                                                <OriginalRetrefNum>{request.ReferenceNumber}</OriginalRetrefNum>
                                            </Transaction>
                                        </GVPSRequest>";

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
            string responseContent = await response.Content.ReadAsStringAsync();

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            if (xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ReasonCode")?.InnerText != "00")
            {
                string errorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ErrorMsg")?.InnerText;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/SysErrMsg")?.InnerText;

                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesaj?? al??namad??.";

                return CancelPaymentResult.Failed(errorMessage);
            }

            string transactionId = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/RetrefNum")?.InnerText;
            return CancelPaymentResult.Successed(transactionId, transactionId);
        }

        public async Task<RefundPaymentResult> RefundRequest(RefundPaymentRequest request)
        {
            string terminalUserId = request.BankParameters["terminalUserId"];
            string terminalId = request.BankParameters["terminalId"];
            string terminalMerchantId = request.BankParameters["terminalMerchantId"];
            string refundUserId = request.BankParameters["refundUserId"];
            string refundUserPassword = request.BankParameters["refundUserPassword"];
            string mode = request.BankParameters["mode"] == "1" ? "PROD" : "TEST";

            //garanti terminal numaras??n?? 9 haneye tamamlamak i??in ba????na s??f??r eklenmesini istiyor.
            string _terminalid = string.Format("{0:000000000}", int.Parse(terminalId));

            //garanti bankas??nda tutar bilgisinde nokta, virg??l gibi de??erler istenmiyor. 1.10 TL'lik i??lem 110 olarak g??nderilmeli. Yani tutar?? 100 ile ??arpabiliriz.
            string amount = (request.TotalAmount * 100m).ToString("0.##", new CultureInfo("en-US"));//virg??lden sonraki s??f??rlara gerek yok

            //provizyon ??ifresi ve 9 haneli terminal numaras??n??n birle??imi ile bir hash olu??turuluyor
            string securityData = GetSHA1($"{refundUserPassword}{_terminalid}");

            //ilgili veriler birle??tirilip hash olu??turuluyor
            string hashstr = GetSHA1($"{request.OrderNumber}{terminalId}{amount}{securityData}");

            string installment = request.Installment.ToString();
            if (request.Installment < 2)
                installment = string.Empty;//0 veya 1 olmas?? durumunda taksit bilgisini bo?? g??nderiyoruz

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                        <GVPSRequest>
                                            <Mode>{mode}</Mode>
                                            <Version>v0.01</Version>
                                            <ChannelCode></ChannelCode>
                                            <Terminal>
                                                <ProvUserID>{refundUserId}</ProvUserID>
                                                <HashData>{hashstr}</HashData>
                                                <UserID>{terminalUserId}</UserID>
                                                <ID>{terminalId}</ID>
                                                <MerchantID>{terminalMerchantId}</MerchantID>
                                            </Terminal>
                                            <Customer>
                                                <IPAddress>{request.CustomerIpAddress}</IPAddress>
                                                <EmailAddress></EmailAddress>
                                            </Customer>
                                            <Order>
                                                <OrderID>{request.OrderNumber}</OrderID>
                                                <GroupID></GroupID>
                                            </Order>
                                            <Transaction>
                                                <Type>refund</Type>
                                                <InstallmentCnt>{installment}</InstallmentCnt>
                                                <Amount>{amount}</Amount>
                                                <CurrencyCode>{request.CurrencyIsoCode}</CurrencyCode>
                                                <CardholderPresentCode>0</CardholderPresentCode>
                                                <MotoInd>N</MotoInd>
                                                <OriginalRetrefNum>{request.ReferenceNumber}</OriginalRetrefNum>
                                            </Transaction>
                                        </GVPSRequest>";

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
            string responseContent = await response.Content.ReadAsStringAsync();

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            if (xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ReasonCode")?.InnerText != "00")
            {
                string errorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ErrorMsg")?.InnerText;
                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/SysErrMsg")?.InnerText;

                if (string.IsNullOrEmpty(errorMessage))
                    errorMessage = "Bankadan hata mesaj?? al??namad??.";

                return RefundPaymentResult.Failed(errorMessage);
            }

            string transactionId = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/RetrefNum")?.InnerText;
            return RefundPaymentResult.Successed(transactionId, transactionId);
        }

        public async Task<PaymentDetailResult> PaymentDetailRequest(PaymentDetailRequest request)
        {
            string terminalUserId = request.BankParameters["terminalUserId"];
            string terminalId = request.BankParameters["terminalId"];
            string terminalMerchantId = request.BankParameters["terminalMerchantId"];
            string terminalProvUserId = request.BankParameters["terminalProvUserId"];
            string terminalProvPassword = request.BankParameters["terminalProvPassword"];
            string mode = request.BankParameters["mode"] == "1" ? "PROD" : "TEST";

            //garanti terminal numaras??n?? 9 haneye tamamlamak i??in ba????na s??f??r eklenmesini istiyor.
            string _terminalid = string.Format("{0:000000000}", int.Parse(terminalId));

            //provizyon ??ifresi ve 9 haneli terminal numaras??n??n birle??imi ile bir hash olu??turuluyor
            string securityData = GetSHA1($"{terminalProvPassword}{_terminalid}");

            string amount = "100";//sabit 100 g??nderin dediler. Yani 1 TL.

            //ilgili veriler birle??tirilip hash olu??turuluyor
            string hashstr = GetSHA1($"{request.OrderNumber}{terminalId}{amount}{securityData}");

            string requestXml = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                                        <GVPSRequest>
                                           <Mode>{mode}</Mode>
                                           <Version>v0.01</Version>
                                           <ChannelCode />
                                           <Terminal>
                                              <ProvUserID>{terminalProvUserId}</ProvUserID>
                                              <HashData>{hashstr}</HashData>
                                              <UserID>{terminalUserId}</UserID>
                                              <ID>{terminalId}</ID>
                                              <MerchantID>{terminalMerchantId}</MerchantID>
                                           </Terminal>
                                           <Customer>
                                              <IPAddress>{request.CustomerIpAddress}</IPAddress>
                                              <EmailAddress></EmailAddress>
                                           </Customer>
                                           <Card>
                                              <Number />
                                              <ExpireDate />
                                              <CVV2 />
                                           </Card>
                                           <Order>
                                              <OrderID>{request.OrderNumber}</OrderID>
                                              <GroupID />
                                           </Order>
                                           <Transaction>
                                              <Type>orderinq</Type>
                                              <InstallmentCnt />
                                              <Amount>{amount}</Amount>
                                              <CurrencyCode>{request.CurrencyIsoCode}</CurrencyCode>
                                              <CardholderPresentCode>0</CardholderPresentCode>
                                              <MotoInd>N</MotoInd>
                                           </Transaction>
                                        </GVPSRequest>";

            var response = await _client.PostAsync(request.BankParameters["verifyUrl"], new StringContent(requestXml, Encoding.UTF8, "text/xml"));
            string responseContent = await response.Content.ReadAsStringAsync();

            var xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            string finalStatus = xmlDocument.SelectSingleNode("GVPSResponse/Order/OrderInqResult/Status")?.InnerText ?? string.Empty;
            string transactionId = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/RetrefNum")?.InnerText;
            string referenceNumber = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/RetrefNum")?.InnerText;
            string cardPrefix = xmlDocument.SelectSingleNode("GVPSResponse/Order/OrderInqResult/CardNumberMasked")?.InnerText;
            int.TryParse(cardPrefix, out int cardPrefixValue);

            string installment = xmlDocument.SelectSingleNode("GVPSResponse/Order/OrderInqResult/InstallmentCnt")?.InnerText ?? "0";
            string bankMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/Message")?.InnerText;
            string responseCode = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ReasonCode")?.InnerText;

            if (finalStatus.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentDetailResult.PaidResult(transactionId, referenceNumber, cardPrefixValue.ToString(), int.Parse(installment), 0, bankMessage, responseCode);
            }
            else if (finalStatus.Equals("VOID", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentDetailResult.CanceledResult(transactionId, referenceNumber, bankMessage, responseCode);
            }
            else if (finalStatus.Equals("REFUNDED", StringComparison.OrdinalIgnoreCase))
            {
                return PaymentDetailResult.RefundedResult(transactionId, referenceNumber, bankMessage, responseCode);
            }

            var bankErrorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/SysErrMsg")?.InnerText ?? string.Empty;
            var errorMessage = xmlDocument.SelectSingleNode("GVPSResponse/Transaction/Response/ErrorMsg")?.InnerText ?? string.Empty;
            if (string.IsNullOrEmpty(errorMessage))
                errorMessage = "Bankadan hata mesaj?? al??namad??.";

            return PaymentDetailResult.FailedResult(bankErrorMessage, responseCode, errorMessage);
        }
        private string GetSHA1(string text)
        {
            var provider = CodePagesEncodingProvider.Instance;
            Encoding.RegisterProvider(provider);

            var cryptoServiceProvider = new SHA1CryptoServiceProvider();
            var inputbytes = cryptoServiceProvider.ComputeHash(Encoding.GetEncoding("ISO-8859-9").GetBytes(text));

            var builder = new StringBuilder();
            for (int i = 0; i < inputbytes.Length; i++)
            {
                builder.Append(string.Format("{0,2:x}", inputbytes[i]).Replace(" ", "0"));
            }

            return builder.ToString().ToUpper();
        }
        private static readonly Dictionary<string, string> CurrencyCodes = new Dictionary<string, string>
        {
           { "TRY","949" },
            { "USD","840" },
            { "EUR","978" },
            { "GBP","826" }
        };
        private static readonly string[] mdStatusCodes = new[] { "1", "2", "3", "4" };
    }
}

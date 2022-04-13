using Grand.Domain.Customers;
using Grand.Plugin.Payments.AllBank.Iyzico;
using Grand.Plugin.Payments.AllBank.Iyzico.Models;
using Grand.Plugin.Payments.AllBank.Iyzico.Request;
using Grand.Plugin.Payments.AllBank.Models.Banks;
using Grand.Plugin.Payments.AllBank.Requests;
using Grand.Plugin.Payments.AllBank.Results;
using Grand.Services.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Grand.Plugin.Payments.AllBank.Providers
{
    public class IyzicoPaymentProvider : IPaymentProvider
    {

        public async Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            ThreedsInitialize payment = null;
            try
            {
                PaymentCard paymentCard = new PaymentCard {
                    CardHolderName = request.CardHolderName,
                    CardNumber = request.CardNumber,
                    ExpireMonth = AllBankHelper.EncodeExpireMonth(request.ExpireMonth),
                    ExpireYear = request.ExpireYear.ToString().Length == 2 ? $"20{request.ExpireYear}" : request.ExpireYear.ToString(),
                    Cvc = request.CvvCode,
                    RegisterCard = 0
                };
                Buyer buyer = new Buyer {
                    Id = request.Customer.Id,
                    Name = request.Customer.GetAttributeFromEntity<string>(SystemCustomerAttributeNames.FirstName) ??
                           request.Customer.BillingAddress.FirstName,
                    Surname = request.Customer.GetAttributeFromEntity<string>(SystemCustomerAttributeNames.LastName) ??
                              request.Customer.BillingAddress.LastName,
                    IdentityNumber = request.Customer.GetAttributeFromEntity<string>(SystemCustomerAttributeNames.VatNumber) ?? "111111111111",
                    Email = request.Customer.Email ?? request.Customer.BillingAddress.Email,
                    City = request.Customer.GetAttributeFromEntity<string>(SystemCustomerAttributeNames.City) ??
                           request.Customer.BillingAddress.City,
                    Country = request.Country.Name,
                    Ip = request.CustomerIpAddress,
                    RegistrationAddress = request.Customer.BillingAddress.Address1
                };
                Address billingAddress = new Address {
                    ContactName = request.Customer.BillingAddress.FirstName + " " +
                                  request.Customer.BillingAddress.LastName,
                    City = request.Customer.BillingAddress.City,
                    Country = request.Country.Name,
                    Description = request.Customer.BillingAddress.Address1,
                    ZipCode = request.Customer.BillingAddress.ZipPostalCode
                };
                Address shippingAddress = new Address {
                    ContactName = request.Customer.ShippingAddress.FirstName + " " +
                                  request.Customer.ShippingAddress.LastName,
                    City = request.Customer.ShippingAddress.City,
                    Country = request.Country.Name,
                    Description = request.Customer.ShippingAddress.Address1,
                    ZipCode = request.Customer.ShippingAddress.ZipPostalCode
                };
                CreatePaymentRequest createPaymentRequest = new CreatePaymentRequest {
                    ConversationId = request.OrderNumber,
                    Currency = request.CurrencyIsoCode,
                    Installment = request.Installment,
                    PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                    PaymentChannel = PaymentChannel.MOBILE_WEB.ToString(),
                    Locale = request.LanguageIsoCode,
                    BasketId = request.Order.OrderNumber.ToString(),
                    PaidPrice = request.TotalAmount.ToString("0.00",new CultureInfo("en-EN")),
                    PaymentCard = paymentCard,
                    Buyer = buyer,
                    ShippingAddress = shippingAddress,
                    BillingAddress = billingAddress,
                    BasketItems = request.BasketItems,
                    CallbackUrl = request.CallbackUrl.ToString(),
                    Price = request.IyzicoPrice.ToString("0.00",new CultureInfo("en-EN"))

                };
                var options = new Options {
                    ApiKey = request.BankParameters[nameof(IyzicoBankModel.ApiKey)],
                    SecretKey = request.BankParameters[nameof(IyzicoBankModel.SecretKey)],
                    BaseUrl = request.BankParameters[nameof(IyzicoBankModel.ApiUrl)]
                };
                payment = ThreedsInitialize.Create(createPaymentRequest, options);
                if (payment.Status == "success")
                {
                    return await Task.FromResult(PaymentGatewayResult.Successed(payment.HtmlContent, payment.Status));
                }

                return await Task.FromResult(PaymentGatewayResult.Failed(payment.ErrorMessage));
            }
            catch (Exception ex)
            {
                return await Task.FromResult(PaymentGatewayResult.Failed(payment?.ErrorMessage, ex.Message));
            }

        }

        public async Task<VerifyGatewayResult> VerifyGateway(VerifyGatewayRequest request, PaymentGatewayRequest gatewayRequest, IFormCollection form)
        {
            if (form == null)
            {
                return await Task.FromResult(VerifyGatewayResult.Failed("Form verisi alınamadı."));
            }

            if (form["status"] == "success")
            {
                if (form["mdStatus"] == "1")
                {
                    CreateThreedsPaymentRequest createThreedsPayment = new CreateThreedsPaymentRequest {
                        ConversationId = gatewayRequest.OrderNumber,
                        PaymentId = form["paymentId"],
                        ConversationData = form["conversationData"],
                        Locale = gatewayRequest.LanguageIsoCode
                    };
                    var options = new Options {
                        ApiKey = request.BankParameters[nameof(IyzicoBankModel.ApiKey)],
                        SecretKey = request.BankParameters[nameof(IyzicoBankModel.SecretKey)],
                        BaseUrl = request.BankParameters[nameof(IyzicoBankModel.ApiUrl)]
                    };
                    ThreedsPayment threedsPayment = ThreedsPayment.Create(createThreedsPayment, options);
                    if (threedsPayment.Status.Equals("failure"))
                    {
                        return await Task.FromResult(VerifyGatewayResult.Failed(ErrorCodes[threedsPayment.ErrorCode],
                            threedsPayment.ErrorCode));
                    }

                    return await Task.FromResult(VerifyGatewayResult.Successed(transactionId: threedsPayment.AuthCode,
                        referenceNumber: threedsPayment.PaymentId, installment: Convert.ToInt32(threedsPayment.Installment),
                        amount: threedsPayment.PaidPrice, paymentItems: threedsPayment.PaymentItems));
                }


                return await Task.FromResult(VerifyGatewayResult.Failed(MdStatusCodes[form["mdStatus"].ToString()]));

            }

            return await Task.FromResult(VerifyGatewayResult.Failed(MdStatusCodes[form["mdStatus"].ToString()]));


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
            {"10051", "Kart limiti yetersiz, yetersiz bakiye"},
            {"10005", "İşlem onaylanmadı"},
            {"10012", "Geçersiz işlem"},
            {"10041", "Kayıp kart, karta el koyunuz"},
            {"10043", "Çalıntı kart, karta el koyunuz"},
            {"10054", "Vadesi dolmuş kart"},
            {"10084", "CVC2 bilgisi hatalı"},
            {"10057", "Kart sahibi bu işlemi yapamaz"},
            {"10058", "Terminalin bu işlemi yapmaya yetkisi yok"},
            {"10034", "Dolandırıcılık şüphesi"},
            {"10093", "Kartınız e-ticaret işlemlerine kapalıdır. Bankanızı arayınız."},
            {"10201", "Kart, işleme izin vermedi"},
            {"10204", "Ödeme işlemi esnasında genel bir hata oluştu"},
            {"10206", "CVC uzunluğu geçersiz"},
            {"10207", "Bankanızdan onay alınız"},
            {"10208", "Üye işyeri kategori kodu hatalı"},
            {"10209", "Bloke statülü kart"},
            {"10210", "Hatalı CAVV bilgisi"},
            {"10211", "Hatalı ECI bilgisi"},
            {"10213", "BIN bulunamadı"},
            {"10214", "İletişim veya sistem hatası"},
            {"10215", "Geçersiz kart numarası"},
            {"10216", "Bankası bulunamadı"},
            {"10217", "Banka kartları sadece 3D Secure işleminde kullanılabilir"},
            {"10219", "Bankaya gönderilen istek zaman aşımına uğradı"},
            {"10222", "Terminal taksitli işleme kapalı"},
            {"10223", "Gün sonu yapılmalı"},
            {"10225", "Kısıtlı kart"},
            {"10226", "İzin verilen PIN giriş sayısı aşılmış"},
            {"10227", "Geçersiz PIN"},
            {"10228", "Banka veya terminal işlem yapamıyor"},
            {"10229", "Son kullanma tarihi geçersiz"},
            {"10232", "Geçersiz tutar"},
        };

        private static readonly IDictionary<string, string> MdStatusCodes = new Dictionary<string, string>
        {
            {"0", "3-D Secure imzası geçersiz veya doğrulama"},
            {"2", "Kart sahibi veya bankası sisteme kayıtlı değil"},
            {"3", "Kartın bankası sisteme kayıtlı değil"},
            {"4", "Doğrulama denemesi, kart sahibi sisteme daha sonra kayıt olmayı seçmiş"},
            {"5", "Doğrulama yapılamıyor"},
            {"6", "3-D Secure hatası"},
            {"7", "Sistem hatası"},
            {"8", "Bilinmeyen kart no"}
        };


    }
}

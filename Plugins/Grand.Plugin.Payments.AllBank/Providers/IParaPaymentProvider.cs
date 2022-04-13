using Grand.Domain.Customers;
using Grand.Plugin.Payments.AllBank.IParaCore;
using Grand.Plugin.Payments.AllBank.IParaCore.Entity;
using Grand.Plugin.Payments.AllBank.IParaCore.Request;
using Grand.Plugin.Payments.AllBank.IParaCore.Response;
using Grand.Plugin.Payments.AllBank.Models.Banks;
using Grand.Plugin.Payments.AllBank.Requests;
using Grand.Plugin.Payments.AllBank.Results;
using Grand.Services.Catalog;
using Grand.Services.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Grand.Plugin.Payments.AllBank.Providers
{
    public class ParaPaymentProvider : IPaymentProvider
    {
        private readonly IProductService _productService;
        public ParaPaymentProvider(IProductService productService)
        {
            _productService = productService;

        }
        public async Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request)
        {
            try
            {

                var paymentRequest = new ThreeDPaymentInitRequest {
                    Version = "1.0",
                    OrderId = request.OrderNumber,
                    Echo = request.Customer.Id.ToString(),
                    Mode = request.TestMode ? "T" : "P",
                    Amount = (request.TotalAmount*100).ToString("0.##", new CultureInfo("en-US")),
                    CardOwnerName = request.CardHolderName,
                    CardNumber = request.CardNumber,
                    CardExpireMonth = AllBankHelper.EncodeExpireMonth(request.ExpireMonth),
                    CardExpireYear = request.ExpireYear.ToString(),
                    Installment = request.Installment == 0 ? string.Empty : request.Installment.ToString(),
                    Cvc = request.CvvCode,
                    SuccessUrl = request.CallbackUrl.ToString(),
                    FailUrl = request.CallbackUrl.ToString(),
                    UserId = string.Empty,
                    CardId = string.Empty,
                    PurchaserName =
                        request.Customer.GetAttributeFromEntity<string>(SystemCustomerAttributeNames.FirstName) ??
                        request.Customer.BillingAddress.FirstName,
                    PurchaserSurname =
                        request.Customer.GetAttributeFromEntity<string>(SystemCustomerAttributeNames.LastName) ??
                        request.Customer.BillingAddress.LastName,
                    PurchaserEmail = request.Customer.Email ?? request.Customer.BillingAddress.Email
                };

                var setting = new Settings {

                    PublicKey = request.BankParameters[nameof(ParaPayModel.PublicKey)],
                    PrivateKey = request.BankParameters[nameof(ParaPayModel.PrivateKey)],
                    BaseUrl = request.BankParameters[nameof(ParaPayModel.BaseUrl)],
                    ThreeDInquiryUrl = request.BankParameters[nameof(ParaPayModel.ThreeDInquiryUrl)],
                    Version = "1.0",
                    Mode = request.TestMode ? "T" : "P",
                    HashString = string.Empty
                };


                var form = ThreeDPaymentInitRequest.Execute(paymentRequest, setting);
                return await Task.FromResult(PaymentGatewayResult.Successed(form));
            }
            catch (Exception ex)
            {

                return await Task.FromResult(PaymentGatewayResult.Failed(ex.Message));
            }
        }

        public async Task<VerifyGatewayResult> VerifyGateway(VerifyGatewayRequest request,
            PaymentGatewayRequest gatewayRequest, IFormCollection form)
        {

            var paymentResponse = new ThreeDPaymentInitResponse {
                OrderId = form["orderId"],
                Result = form["result"],
                Amount = form["amount"],
                Mode = form["mode"]
            };

            if (!string.IsNullOrEmpty(form["errorCode"]))
            {
                paymentResponse.ErrorCode = form["errorCode"];
            }

            if (!string.IsNullOrEmpty(form["errorMessage"]))
            {
                paymentResponse.ErrorMessage = form["errorMessage"];
            }

            if (!string.IsNullOrEmpty(form["transactionDate"]))
            {
                paymentResponse.TransactionDate = form["transactionDate"];
            }

            if (!string.IsNullOrEmpty(form["hash"]))
            {
                paymentResponse.Hash = form["hash"];
            }

            var setting = new Settings {

                PublicKey = request.BankParameters[nameof(ParaPayModel.PublicKey)],
                PrivateKey = request.BankParameters[nameof(ParaPayModel.PrivateKey)],
                BaseUrl = request.BankParameters[nameof(ParaPayModel.BaseUrl)],
                ThreeDInquiryUrl = request.BankParameters[nameof(ParaPayModel.ThreeDInquiryUrl)],
                Version = "1.0",
                Mode = gatewayRequest.TestMode ? "T" : "P",
                HashString = string.Empty
            };
            if (paymentResponse.Result == "1")
            {
                if (Helper.Validate3DReturn(paymentResponse, setting))
                {
                    var paymentRequest = new ThreeDPaymentCompleteRequest
                    {
                        OrderId = gatewayRequest.OrderNumber,
                        Echo = form["echo"],
                        Mode = gatewayRequest.TestMode ? "T" : "P",
                        Amount = (gatewayRequest.TotalAmount*100).ToString("0.##", new CultureInfo("en-US")),
                        CardOwnerName = gatewayRequest.CardHolderName,
                        CardNumber = gatewayRequest.CardNumber,
                        CardExpireMonth = AllBankHelper.EncodeExpireMonth(gatewayRequest.ExpireMonth),
                        CardExpireYear = gatewayRequest.ExpireYear.ToString(),
                        Installment = gatewayRequest.Installment == 0
                            ? string.Empty
                            : gatewayRequest.Installment.ToString(),
                        Cvc = gatewayRequest.CvvCode,
                        ThreeD = "true",
                        ThreeDSecureCode = form["threeDSecureCode"],
                        Purchaser = new Purchaser
                        {
                            BirthDate = gatewayRequest.Customer.GetAttributeFromEntity<string>(SystemCustomerAttributeNames
                                .DateOfBirth),
                            GsmPhone = gatewayRequest.Customer.BillingAddress.PhoneNumber,
                            IdentityNumber =
                                gatewayRequest.Customer.GetAttributeFromEntity<string>(SystemCustomerAttributeNames
                                    .VatNumber) ?? "11111111111",
                            InvoiceAddress = new PurchaserAddress
                            {
                                Name = gatewayRequest.Customer.BillingAddress.FirstName,
                                SurName = gatewayRequest.Customer.BillingAddress.LastName,
                                Address = gatewayRequest.Customer.BillingAddress.Address1,
                                ZipCode = gatewayRequest.Customer.BillingAddress.ZipPostalCode,
                                CityCode = string.Empty,
                                CountryCode = gatewayRequest.Country.TwoLetterIsoCode ?? "TR",
                                CompanyName =
                                    gatewayRequest.Customer.GetAttributeFromEntity<string>(SystemCustomerAttributeNames
                                        .Company),
                                PhoneNumber = gatewayRequest.Customer.BillingAddress.PhoneNumber
                            },
                            ShippingAddress = new PurchaserAddress
                            {
                                Name = gatewayRequest.Customer.ShippingAddress.FirstName,
                                SurName = gatewayRequest.Customer.ShippingAddress.LastName,
                                Address = gatewayRequest.Customer.ShippingAddress.Address1,
                                ZipCode = gatewayRequest.Customer.ShippingAddress.ZipPostalCode,
                                CityCode = string.Empty,
                                CountryCode = gatewayRequest.Country.TwoLetterIsoCode ?? "TR",
                                PhoneNumber = gatewayRequest.Customer.ShippingAddress.PhoneNumber
                            }
                        },
                        Products = new List<Product>()
                    };
                    foreach (var sci in gatewayRequest.ShoppingCartItems)
                    {
                        var product = await _productService.GetProductById(sci.ProductId);
                        var p = new Product
                        {
                            Title = product.Name,
                            Code = product.Id,
                            Price = product.Price.ToString("0.##", new CultureInfo("en-US")),
                            Quantity = sci.Quantity
                        };
                        paymentRequest.Products.Add(p);
                    }

                    var response = ThreeDPaymentCompleteRequest.Execute(paymentRequest, setting);
                    if (response.Result == "1")
                    {
                        return await Task.FromResult(VerifyGatewayResult.Successed(transactionId: response.Hash,
                            referenceNumber: response.Hash, amount: response.Amount));
                    }

                    return await Task.FromResult(VerifyGatewayResult.Failed(response.ErrorMessage, response.ErrorCode,
                        response.ResponseMessage));
                }
            }
            return await Task.FromResult(VerifyGatewayResult.Failed(paymentResponse.ErrorMessage,
                paymentResponse.ErrorCode,
                paymentResponse.ResponseMessage));
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
    }
}

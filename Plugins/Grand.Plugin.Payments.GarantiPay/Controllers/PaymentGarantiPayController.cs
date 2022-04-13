using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Grand.Core;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Framework.Controllers;
using Grand.Framework.Mvc.Filters;
using Grand.Plugin.Payments.AllBank.Models;
using Grand.Plugin.Payments.GarantiPay.Domain;
using Grand.Plugin.Payments.GarantiPay.Models;
using Grand.Plugin.Payments.GarantiPay.Models.Banks;
using Grand.Plugin.Payments.GarantiPay.Services;
using Grand.Services.Commands.Models.Orders;
using Grand.Services.Configuration;
using Grand.Services.Customers;
using Grand.Services.Localization;
using Grand.Services.Logging;
using Grand.Services.Messages;
using Grand.Services.Orders;
using Grand.Services.Payments;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Grand.Plugin.Payments.GarantiPay.Controllers
{
    public class PaymentGarantiPayController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGarantiPayOrderServices _garantiPayOrderServices;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly ILogger _logger;


        public PaymentGarantiPayController(
            ISettingService settingService,
            ILocalizationService localizationService,
            IOrderService orderService,
            ICustomerService customerService,
            IHttpContextAccessor httpContextAccessor,
            IGarantiPayOrderServices garantiPayOrderServices,
            IWorkflowMessageService workflowMessageService,
            IWorkContext workContext, IMediator mediator,
            ILogger logger)
        {
            _settingService = settingService;
            _localizationService = localizationService;
            _orderService = orderService;
            _customerService = customerService;
            _httpContextAccessor = httpContextAccessor;
            _garantiPayOrderServices = garantiPayOrderServices;
            _workflowMessageService = workflowMessageService;
            _workContext = workContext;
            _mediator = mediator;
            _logger = logger;
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


        [AuthorizeAdmin]
        [Area("Admin")]
        public IActionResult Configure()
        {
            var paymentGarantiPaySettings = _settingService.LoadSetting<PaymentGarantiPaySettings>();
            var model = new ConfigurationModel
            {
                AdditionalFee = paymentGarantiPaySettings.AdditionalFee,
                DescriptionText = paymentGarantiPaySettings.DescriptionText,
                IsInstallment = paymentGarantiPaySettings.IsInstallment,
                AdditionalFeePercentage = paymentGarantiPaySettings.AdditionalFeePercentage,
                TestMode = paymentGarantiPaySettings.TestMode


            };
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.GarantiPay/Views/Configure.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area("Admin")]
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public async Task<IActionResult> Configure(ConfigurationModel model, IFormCollection form)
        {
            if (!ModelState.IsValid)
                return Configure();
            var paymentGarantiPaySettings = _settingService.LoadSetting<PaymentGarantiPaySettings>();
            paymentGarantiPaySettings.AdditionalFee = model.AdditionalFee;
            paymentGarantiPaySettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            paymentGarantiPaySettings.DescriptionText = model.DescriptionText;
            paymentGarantiPaySettings.IsInstallment = model.IsInstallment;
            paymentGarantiPaySettings.TestMode = model.TestMode;
            await _settingService.SaveSetting(paymentGarantiPaySettings);
            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));
            return Configure();
        }
        [HttpPost]
        public async Task<IActionResult> Success(IFormCollection form)
        {
            var formDic = form.ToDictionary(key => key.Key, value => value.Value);
            string errorMessage = form["errmsg"];
            string strType = form["txntype"];
            string strAmount = form["txnamount"];
            string strInstallmentCount = form["txninstallmentcount"];
            string strOrderId = form["oid"];
            var posList = await _garantiPayOrderServices.GetGarantiPayPosList();
            var pos = posList.FirstOrDefault();
            var parameters = pos.GenericAttributes.ToDictionary(key => key.Key, value => value.Value);
            var bankOrder = await _garantiPayOrderServices.GetOmniGarantiPayNumber(strOrderId);
            string strTerminalId = form["clientid"];
            string _strTerminalId = "0" + strTerminalId;
            string strStoreKey = parameters[nameof(GarantiBankModel.StoreKey)];
            string strProvisionPassword = parameters[nameof(GarantiBankModel.TerminalProvPassword)];
            string strSuccessUrl = form["successurl"];
            string strErrorUrl = form["errorurl"];
            string authcode = form["authcode"];
            string procreturncode = form["procreturncode"];
            string securityData = GetSHA1(strProvisionPassword + _strTerminalId).ToUpper();
            string strHashData = form["secure3dhash"];
            string validateHashData = GetSHA1(strTerminalId + strOrderId + strAmount + strSuccessUrl + strErrorUrl + strType + strInstallmentCount + strStoreKey + securityData).ToUpper();
            if (strHashData == validateHashData)
            {
                if (procreturncode == "00")
                {
                    var responseModel = new ResponseModel
                    {
                        AuthCode = authcode,
                        HASH = strHashData,
                        ProcReturnCode = procreturncode,

                    };
                    var order = await _orderService.GetOrderByGuid(bankOrder.OrderGuid);
                    order.OrderTotal = PaymentGarantiPayHelper.ToDecimal(strAmount);
                    order.AuthorizationTransactionId = authcode;
                    order.PaymentStatus = PaymentStatus.Paid;
                    order.OrderStatus = OrderStatus.Processing;
                    order.PaidDateUtc = DateTime.UtcNow;
                    order.CustomValuesXml = null;
                    var processPaymentRequest = new ProcessPaymentRequest();
                    processPaymentRequest.CustomValues.Add("Taksit :", strInstallmentCount);
                    processPaymentRequest.CustomValues.Add("Toplam :", PaymentGarantiPayHelper.ToDecimal(strAmount).ToString("C"));
                    processPaymentRequest.CustomValues.Add("Ödeme :", "Başarılı");
                    order.CustomValuesXml = processPaymentRequest.SerializeCustomValues();
                    var orderNote = new OrderNote
                    {
                        DisplayToCustomer = false,
                        CreatedOnUtc = DateTime.UtcNow,
                        Note = $"Taksit Sayısı:{strInstallmentCount}",
                        OrderId = order.Id
                    };
                    await _orderService.InsertOrderNote(orderNote);
                    await _orderService.UpdateOrder(order);
                    await _workflowMessageService.SendOrderPaidCustomerNotification(order,
                        _workContext.WorkingLanguage.Id);
                    await _workflowMessageService.SendOrderPaidStoreOwnerNotification(order,
                        _workContext.WorkingLanguage.Id);
                    bankOrder.BankResponse = JsonConvert.SerializeObject(formDic);
                    bankOrder.MarkAsPaid(DateTime.Now);
                    bankOrder.PaymentResultSession = Guid.NewGuid();
                    bankOrder.StatusId = (int)PaymentStatus.Paid;
                    await _garantiPayOrderServices.UpdateOmniGarantiPayOrder(bankOrder);
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                }
                await CancelOrder(bankOrder);
                ErrorNotification(errorMessage);
                _httpContextAccessor.HttpContext.Session.Set("InstallmentViewModel", null);
                return RedirectToAction("ShippingMethod", "Checkout");
            }
            await CancelOrder(bankOrder);
            ErrorNotification("Mesaj Bankadan Gelmiyor Bu yüzden Site Yetkilisi ile temasa Geçin.");
            _httpContextAccessor.HttpContext.Session.Set("InstallmentViewModel", null);
            return RedirectToAction("ShippingMethod", "Checkout");

        }
        [HttpPost]
        public async Task<IActionResult> Cancel(IFormCollection form)
        {
            var formDic = form.ToDictionary(key => key.Key, value => value.Value);
            var message = formDic["mderrormessage"] + " Ref No: " +formDic["mdstatus"];
            if (message.Contains("0809"))
            {
                ErrorNotification("Bayi Kodu ve Kredi Kartı numaranız sisteme kayıtlı olmadığı için işleminiz gerçekleşmemiştir. Lütfen müşteri temsilciniz ile iletişime geçiniz");
            }
            var bankOrder = await _garantiPayOrderServices.GetOmniGarantiPayNumber(formDic["oid"]);
            bankOrder.BankErrorMessage = formDic["errmsg"];
            bankOrder.StatusId = (int)PaymentStatus.Pending;
            bankOrder.PaymentResultSession = Guid.NewGuid();
            bankOrder.MarkAsFailed(formDic["errmsg"], JsonConvert.SerializeObject(formDic));
            await _garantiPayOrderServices.UpdateOmniGarantiPayOrder(bankOrder);
            await CancelOrder(bankOrder);
            ErrorNotification(formDic["errmsg"]);
            return RedirectToAction("ShippingMethod", "Checkout");
        }

        private async Task CancelOrder(OmniGarantiPayOrder payment)
        {
            var order = await _orderService.GetOrderByGuid(payment.OrderGuid);
            var customer = await _customerService.GetCustomerById(order.CustomerId);
            await _mediator.Send(new ReOrderCommand() { Order = order });
            await _orderService.DeleteOrder(order).ConfigureAwait(false);
            await _workflowMessageService.SendOrderCancelledCustomerNotification(order, _workContext.WorkingLanguage.Id);
            await _workflowMessageService.SendOrderCancelledStoreOwnerNotification(order, _workContext.WorkingLanguage.Id);
            _logger.Information($"{order.OrderNumber} no'lu { customer.Email } ait siparişle ilgili hata oluştu yeniden ödeme için sepete eklendi", null, customer);
        }

    }
}
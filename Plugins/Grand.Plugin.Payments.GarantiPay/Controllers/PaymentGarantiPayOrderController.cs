using System.Linq;
using System.Threading.Tasks;
using Grand.Framework.Kendoui;
using Grand.Framework.Mvc.Filters;
using Grand.Framework.Security.Authorization;
using Grand.Plugin.Payments.GarantiPay.Models.BankOrder;
using Grand.Plugin.Payments.GarantiPay.Services;
using Grand.Services.Customers;
using Grand.Services.Security;
using Grand.Web.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Plugin.Payments.GarantiPay.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.Plugins)]
    public class PaymentGarantiPayOrderController : BaseAdminController
    {
        private readonly IGarantiPayOrderServices _bankOrderService;
        private readonly ICustomerService _customerService;


        public PaymentGarantiPayOrderController(IGarantiPayOrderServices bankOrderService,
            ICustomerService customerService)
        {
            _bankOrderService = bankOrderService;
            _customerService = customerService;
           
        }


        public IActionResult Index() => RedirectToAction("List");

        public  IActionResult List()
        {
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.GarantiPay/Views/PaymentGarantiPayOrder/List.cshtml");
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, BankOrderModel model)
        {
            var bankOrder = await _bankOrderService.GetOmniGarantiPayOrderPageList(model.CustomerId, command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = bankOrder.Select(b =>
                {
                    var orderModel = new BankOrderModel
                    {
                        OrderGuid = b.OrderGuid,
                        OrderNumber = b.OrderNumber,
                        CustomerId = b.CustomerId,
                        CustomerEmail = _customerService.GetCustomerById(b.CustomerId).Result.Email,
                        TransactionNumber = b.TransactionNumber,
                        ReferenceNumber = b.ReferenceNumber,
                        PaymentAmount = b.PaymentAmount,
                        PaidDate = b.PaidDate,
                        PaymentResultSession = b.PaymentResultSession,
                        Id = b.Id

                    };
                    return orderModel;

                }),
                Total = bankOrder.TotalCount
            };


            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var bankOrder = await _bankOrderService.GetOmniGarantiPayOrderId(id);
            var model = new BankOrderModel
            {
                OrderGuid = bankOrder.OrderGuid,
                CustomerId = bankOrder.CustomerId,
                TotalAmount = bankOrder.TotalAmount,
                Hash = bankOrder.Hash,
                Deleted = bankOrder.Deleted,
                BankErrorMessage = bankOrder.BankErrorMessage,
                BankRequest = bankOrder.BankRequest,
                BankResponse = bankOrder.BankResponse,
                CreateDate = bankOrder.CreateDate,
                Id = bankOrder.Id,
                Installment = bankOrder.Installment,
                OrderNumber = bankOrder.OrderNumber,
                PaidDate = bankOrder.PaidDate,
                PaymentAmount = bankOrder.PaymentAmount,
                PaymentInfo = bankOrder.PaymentInfo,
                PaymentInfoSession = bankOrder.PaymentInfoSession,
                PaymentResultSession = bankOrder.PaymentResultSession,
                ReferenceNumber = bankOrder.ReferenceNumber,
                StatusId = bankOrder.StatusId,
                Token = bankOrder.Token,
                TransactionNumber = bankOrder.TransactionNumber,
                UserAgent = bankOrder.UserAgent,
                UserIpAddress = bankOrder.UserIpAddress

            };
            model.CustomerEmail = _customerService.GetCustomerById(model.CustomerId).Result.Email;
            return View("~/Plugins/Payments.GarantiPay/Views/PaymentGarantiPayOrder/Edit.cshtml", model);
        }
    }
}
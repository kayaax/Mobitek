using Grand.Framework.Kendoui;
using Grand.Framework.Mvc.Filters;
using Grand.Framework.Security.Authorization;
using Grand.Services.Security;
using Grand.Web.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using Grand.Core;
using Grand.Plugin.Payments.AllBank.Models.BankOrder;
using Grand.Plugin.Payments.AllBank.Services;
using Grand.Services.Customers;
using Grand.Services.Orders;
using Grand.Services.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace Grand.Plugin.Payments.AllBank.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.Plugins)]
    public class PaymentBankOrderController : BaseAdminController
    {
        private readonly IBankOrderService _bankOrderService;
        private readonly IStoreContext _storeContext;
        private readonly IOrderService _orderService;
        private readonly ICustomerService _customerService;
        private readonly ILocalizationService _localizationService;


        public PaymentBankOrderController(IBankOrderService bankOrderService,
            IStoreContext storeContext,
            IOrderService orderService,
            ICustomerService customerService, ILocalizationService localizationService)
        {
            _bankOrderService = bankOrderService;
            _storeContext = storeContext;
            _orderService = orderService;
            _customerService = customerService;
            _localizationService = localizationService;
        }


        public IActionResult Index() => RedirectToAction("List");

        public async Task<IActionResult> List()
        {
            var model = new BankOrderListModel();
            model.AvailableCustomers.Insert(0,
                new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = " " });
            var bankOrderList = await _bankOrderService.GetBankOrderList();
            foreach (var order in bankOrderList)
            {
                model.AvailableCustomers.Add(new SelectListItem { Text = order.CustomerId, Value = order.Id });
            }
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankOrder/List.cshtml", model);
        }

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command, BankOrderModel model)
        {
            var bankOrder = await _bankOrderService.GetBankOrderPageList(model.CustomerId, command.Page - 1, command.PageSize);
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
            var bankOrder = await _bankOrderService.GetBankOrderId(id);
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
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankOrder/Edit.cshtml", model);
        }
    }
}
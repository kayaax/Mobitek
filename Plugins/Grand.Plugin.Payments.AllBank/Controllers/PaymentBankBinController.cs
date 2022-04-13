using Grand.Core;
using Grand.Framework.Controllers;
using Grand.Framework.Kendoui;
using Grand.Framework.Mvc.Filters;
using Grand.Framework.Security.Authorization;
using Grand.Plugin.Payments.AllBank.Models.BinList;
using Grand.Plugin.Payments.AllBank.Services;
using Grand.Services.Localization;
using Grand.Services.Logging;
using Grand.Services.Security;
using Grand.Web.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Plugin.Payments.AllBank.Controllers
{

    [AuthorizeAdmin]
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.Plugins)]
    public class PaymentBankBinController : BaseAdminController
    {
        private readonly IBankBinService _bankBinService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IWorkContext _workContext;

        public PaymentBankBinController(IBankBinService bankBinService,
            ILocalizationService localizationService,
            ILogger logger,
            ICustomerActivityService customerActivityService, IWorkContext workContext)
        {
            _bankBinService = bankBinService;
            _localizationService = localizationService;
            _logger = logger;
            _customerActivityService = customerActivityService;
            _workContext = workContext;
        }

        // ReSharper disable once Mvc.ViewNotResolved
        public IActionResult List() => View("~/Plugins/Payments.AllBank/Views/PaymentBankBin/List.cshtml");

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> List(DataSourceRequest command)
        {
            var bankBins = await _bankBinService.GetBankBinPageList("", command.Page - 1, command.PageSize);
            var model = new List<OmniBankBinModel>();

            foreach (var item in bankBins)
            {
                var bin = item.ToModel();
                model.Add(bin);
            }

            var gridModel = new DataSourceResult {
                Data = model,
                Total = bankBins.TotalCount
            };
            return Json(gridModel);
        }
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public  IActionResult Create()
        {
            var model = new OmniBankBinModel();
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankBin/Create.cshtml", model);

        }
        [PermissionAuthorizeAction(PermissionActionName.Create)]
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public async Task<IActionResult> Create(OmniBankBinModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var bankBin = model.ToEntity();
                try
                {
                    await _bankBinService.InsertBankBin(bankBin);
                    SuccessNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankBin.Added"));
                    await _customerActivityService.InsertActivity("AddNewBank", bankBin.Id, $"{bankBin.BinNumber} -Yeni BankaBin Eklendi",
                        _workContext.CurrentCustomer);
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, e, _workContext.CurrentCustomer);
                    ErrorNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankBin.Error"));
                    Console.WriteLine(e);
                    throw;
                }

                return continueEditing ? RedirectToAction("Edit", new { id = bankBin.Id }) : RedirectToAction("List");
            }
           

            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankBin/Create.cshtml", model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var bankBin = await _bankBinService.GetBankBinId(id);
            if (bankBin == null)
                return RedirectToAction("List");
            var model = bankBin.ToModel();
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankBin/Edit.cshtml", model);

        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(OmniBankBinModel model, bool continueEditing)
        {
            var bankBin = await _bankBinService.GetBankBinId(model.Id);
            if (ModelState.IsValid)
            {
                bankBin = model.ToEntity(); 
                try
                {
                    await _bankBinService.UpdateBankBin(bankBin);
                    SuccessNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankBin.Updated"));
                    await _customerActivityService.InsertActivity("UpdateBankBin", bankBin.Id, $"{bankBin.BinNumber} - Güncellendi",
                        _workContext.CurrentCustomer);
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, e, _workContext.CurrentCustomer);
                    ErrorNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankBin.Error"));
                   
                }

                return continueEditing ? RedirectToAction("Edit", new { id = bankBin.Id }) : RedirectToAction("List");

            }

            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankBin/Edit.cshtml", model);
        }


        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            var bankBin = await _bankBinService.GetBankBinId(id);
            if (bankBin == null) return RedirectToAction("List");
            if (ModelState.IsValid)
            {
                try
                {
                    await _bankBinService.DeleteBankBin(bankBin);
                    SuccessNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankBin.Deleted"));
                    await _customerActivityService.InsertActivity("DeleteBankBin", bankBin.Id, $"{bankBin.BinNumber}-Silindi",
                        _workContext.CurrentCustomer);
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, e, _workContext.CurrentCustomer);
                    ErrorNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankBin.Error"));
                   
                }

                return RedirectToAction("List");

            }
            ErrorNotification(ModelState);
            return RedirectToAction("Edit", new { id = bankBin.Id });

        }
    }
}

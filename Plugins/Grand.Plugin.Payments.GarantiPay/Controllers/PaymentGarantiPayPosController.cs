using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Core;
using Grand.Framework.Controllers;
using Grand.Framework.Kendoui;
using Grand.Framework.Mvc;
using Grand.Framework.Mvc.Filters;
using Grand.Framework.Security.Authorization;
using Grand.Plugin.Payments.GarantiPay.Domain;
using Grand.Plugin.Payments.GarantiPay.Models.BankPos;
using Grand.Plugin.Payments.GarantiPay.Models.Banks;
using Grand.Plugin.Payments.GarantiPay.Services;
using Grand.Services.Common;
using Grand.Services.Localization;
using Grand.Services.Logging;
using Grand.Services.Security;
using Grand.Web.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Plugin.Payments.GarantiPay.Controllers
{
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.Plugins)]
    public class PaymentGarantiPayPosController : BaseAdminController
    {
        #region Fields

        private readonly IGarantiPayOrderServices _bankPosService;
        private readonly ILocalizationService _localizationService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IWorkContext _workContext;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILogger _logger;

        #endregion

        #region Ctor

        public PaymentGarantiPayPosController(
            IGarantiPayOrderServices bankPosService,
            ILocalizationService localizationService,
            ILogger logger,
            ICustomerActivityService customerActivityService,
            IWorkContext workContext,
            IGenericAttributeService genericAttributeService
        )
        {
            _bankPosService = bankPosService;
            _localizationService = localizationService;
            _logger = logger;
            _customerActivityService = customerActivityService;
            _workContext = workContext;
            _genericAttributeService = genericAttributeService;
        }

        #endregion

        #region Method

        public IActionResult Index() => RedirectToAction("List");

        public  IActionResult List()
        {
            return View("~/Plugins/Payments.GarantiPay/Views/PaymentGarantiPayPos/List.cshtml");
        }

        #region BankPos
        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> GarantiPayPosList(DataSourceRequest command)
        {
            var bankPoses = await _bankPosService.GetGarantiPayPosPageList(pageIndex: command.Page - 1, pageSize: command.PageSize);
            var gridModel = new DataSourceResult {
                Data = bankPoses.Select(x =>
                {
                    var bModel = new OmniBankPosModel {
                        Id = x.Id,
                        Name = x.Name,
                        IsActive = x.IsActive
                    };
                    return bModel;
                }),
                Total = bankPoses.TotalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var bankPos = await _bankPosService.GetGarantiPayPosId(id);
            var model = bankPos.ToModel();
            var parameterModel = new ParameterModel {
                GarantiBankModel = new GarantiBankModel {
                    TerminalId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalId)),
                    TerminalMerchantId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalMerchantId)),
                    TerminalProvUserId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalProvUserId)),
                    TerminalUserId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalUserId)),
                    TerminalProvPassword = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalProvPassword)),
                    StoreKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.StoreKey)),
                    Email = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.Email)),
                    CompanyName = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.CompanyName)),
                    GatewayUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.GatewayUrl)),
                   VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.VerifyUrl)),

                },
               
            };
            model.ParameterModel = parameterModel;
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.GarantiPay/Views/PaymentGarantiPayPos/Edit.cshtml", model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(OmniBankPosModel model, bool continueEditing)
        {
            var bankPos = model.ToEntity();
            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.TerminalId))
                    await _genericAttributeService.SaveAttribute(bankPos, "TerminalId", model.ParameterModel.GarantiBankModel.TerminalId);
                if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.StoreKey))
                    await _genericAttributeService.SaveAttribute(bankPos, "StoreKey", model.ParameterModel.GarantiBankModel.StoreKey);
                if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.TerminalMerchantId))
                    await _genericAttributeService.SaveAttribute(bankPos, "TerminalMerchantId", model.ParameterModel.GarantiBankModel.TerminalMerchantId);
                if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.TerminalProvPassword))
                    await _genericAttributeService.SaveAttribute(bankPos, "TerminalProvPassword", model.ParameterModel.GarantiBankModel.TerminalProvPassword);
                if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.TerminalProvUserId))
                    await _genericAttributeService.SaveAttribute(bankPos, "TerminalProvUserId", model.ParameterModel.GarantiBankModel.TerminalProvUserId);
                if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.TerminalUserId))
                    await _genericAttributeService.SaveAttribute(bankPos, "TerminalUserId", model.ParameterModel.GarantiBankModel.TerminalUserId);
                if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.Email))
                    await _genericAttributeService.SaveAttribute(bankPos, "Email", model.ParameterModel.GarantiBankModel.Email);
                if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.CompanyName))
                    await _genericAttributeService.SaveAttribute(bankPos, "CompanyName", model.ParameterModel.GarantiBankModel.CompanyName);
                if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.GatewayUrl))
                    await _genericAttributeService.SaveAttribute(bankPos, "GatewayUrl", model.ParameterModel.GarantiBankModel.GatewayUrl);
                if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.VerifyUrl))
                    await _genericAttributeService.SaveAttribute(bankPos, "VerifyUrl", model.ParameterModel.GarantiBankModel.VerifyUrl);
               
                try
                {
                    await _bankPosService.UpdateGarantiPayPos(bankPos);
                    SuccessNotification(_localizationService.GetResource("Plugins.GarantiPay.Admin.BankPos.Updated"));
                    await _customerActivityService.InsertActivity("UpdateBankPos", bankPos.Id,
                        $"{bankPos.Name} - Güncellendi",
                        _workContext.CurrentCustomer);
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, e, _workContext.CurrentCustomer);
                    ErrorNotification(_localizationService.GetResource("Plugins.GarantiPay.Admin.BankPos.Error"));
                    Console.WriteLine(e);
                    throw;
                }

                return continueEditing ? RedirectToAction("Edit", new { id = bankPos.Id }) : RedirectToAction("List");
            }

            var parameterModel = new ParameterModel {

                GarantiBankModel = new GarantiBankModel
                {
                    TerminalId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalId)),
                    TerminalMerchantId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalMerchantId)),
                    TerminalProvUserId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalProvUserId)),
                    TerminalUserId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalUserId)),
                    TerminalProvPassword = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalProvPassword)),
                    StoreKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.StoreKey)),
                    Email = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.Email)),
                    CompanyName = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.CompanyName)),
                    GatewayUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.GatewayUrl)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.VerifyUrl)),

                }
            };
            model.ParameterModel = parameterModel;
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.GarantiPay/Views/PaymentGarantiPayPos/Edit.cshtml", model);
        }

        

        #endregion

        #region BankInstallment

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> BankInstallmentList(DataSourceRequest command, string bankPosId)
        {
            if (String.IsNullOrEmpty(bankPosId)) return null;
            var bankPos = await _bankPosService.GetGarantiPayPosId(bankPosId);
            var items = new List<OmniBankPosModel.BankInstallmentModel>();
            foreach (var item in bankPos.BankInstallments)
            {
                items.Add(new OmniBankPosModel.BankInstallmentModel {
                    Id = item.Id,
                    Percentage = item.Percentage,
                    OmniGarantiPayPosId = item.OmniGarantiPayPosId,
                    NumberOfInstallment = item.NumberOfInstallment
                   
                });
            }

            var gridModel = new DataSourceResult {
                Data = items,
                Total = items.Count
            };
            return Json(gridModel);
        }

        public async Task<IActionResult> BankPosInstallmentAdd(string bankPosId, int numberOfInstallment, decimal percentage)
        {
           
            if (bankPosId == null)
                throw new ArgumentNullException(nameof(bankPosId));
            var bankPos = await _bankPosService.GetGarantiPayPosId(bankPosId);
            try
            {
                var installment = new BankInstallment {
                    NumberOfInstallment = numberOfInstallment,
                    Percentage = percentage,
                   OmniGarantiPayPosId = bankPos.Id
                };
                await _bankPosService.InsertGarantiPayPosInstallment(installment);
                return new NullJsonResult();
            }
            catch (Exception ex)
            {
                return ErrorForKendoGridJson(ex.Message);
            }
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> BankPosInstallmentUpdate(OmniBankPosModel.BankInstallmentModel model)
        {
            var bankPos = await _bankPosService.GetGarantiPayPosId(model.OmniGarantiPayPosId);
            if (bankPos == null)
                throw new ArgumentNullException(nameof(model));

            var bankPosInstallment = bankPos.BankInstallments.FirstOrDefault(x => x.Id == model.Id);
            if (bankPosInstallment == null)
                ModelState.AddModelError("", "Taksit bulunamadı");
            if (ModelState.IsValid)
            {
                try
                {
                    if (bankPosInstallment != null)
                    {
                        bankPosInstallment.NumberOfInstallment = model.NumberOfInstallment;
                        bankPosInstallment.Percentage = model.Percentage;
                        await _bankPosService.UpdateGarantiPayPosInstallment(bankPosInstallment);
                    }

                    return new NullJsonResult();
                }
                catch (Exception exception)
                {
                    return ErrorForKendoGridJson(exception.Message);
                }
            }

            return ErrorForKendoGridJson(ModelState);
        }

        [HttpPost]
        public async Task<IActionResult> BankPosInstallmentDelete(OmniBankPosModel.BankInstallmentModel model)
        {
            var bankPos = await _bankPosService.GetGarantiPayPosId(model.OmniGarantiPayPosId);
            if (bankPos == null)
                throw new ArgumentNullException(nameof(model));
            var bankPosInstallment = bankPos.BankInstallments.FirstOrDefault(x => x.Id == model.Id);
            if (bankPosInstallment == null)
                ModelState.AddModelError("", "Taksit bulunamadı");
            if (ModelState.IsValid)
            {
                if (bankPosInstallment != null)
                {
                    bankPosInstallment.OmniGarantiPayPosId = model.OmniGarantiPayPosId;
                    await _bankPosService.DeleteGarantiPayPosInstallment(bankPosInstallment);
                }

                return new NullJsonResult();
            }

            return ErrorForKendoGridJson(ModelState);
        }

        #endregion


        #endregion
    }
}

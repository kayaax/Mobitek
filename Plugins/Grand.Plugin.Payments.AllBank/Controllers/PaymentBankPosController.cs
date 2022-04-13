using Grand.Core;
using Grand.Framework.Controllers;
using Grand.Framework.Extensions;
using Grand.Framework.Kendoui;
using Grand.Framework.Mvc;
using Grand.Framework.Mvc.Filters;
using Grand.Framework.Security.Authorization;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Models.BankPoses;
using Grand.Plugin.Payments.AllBank.Models.Banks;
using Grand.Plugin.Payments.AllBank.Models.Enums;
using Grand.Plugin.Payments.AllBank.Services;
using Grand.Services.Catalog;
using Grand.Services.Common;
using Grand.Services.Helpers;
using Grand.Services.Localization;
using Grand.Services.Logging;
using Grand.Services.Media;
using Grand.Services.Security;
using Grand.Web.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Plugin.Payments.AllBank.Controllers
{
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.Plugins)]
    public class PaymentBankPosController : BaseAdminController
    {
        #region Fields

        private readonly IBankPosService _bankPosService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IWorkContext _workContext;
        private readonly IPictureService _pictureService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly ICategoryService _categoryService;
        private readonly IGenericAttributeService _genericAttributeService;

        #endregion

        #region Ctor

        public PaymentBankPosController(IBankPosService bankPosService,
            ILocalizationService localizationService,
            ILogger logger,
            IPictureService pictureService,
            ICustomerActivityService customerActivityService,
            IWorkContext workContext,
            IDateTimeHelper dateTimeHelper,
            ICategoryService categoryService,
            IGenericAttributeService genericAttributeService
        )
        {
            _bankPosService = bankPosService;
            _localizationService = localizationService;
            _logger = logger;
            _pictureService = pictureService;
            _customerActivityService = customerActivityService;
            _workContext = workContext;
            _dateTimeHelper = dateTimeHelper;
            _categoryService = categoryService;
            _genericAttributeService = genericAttributeService;
        }

        #endregion

        #region Method

        public IActionResult Index() => RedirectToAction("List");

        public async Task<IActionResult> List()
        {
            var model = new OmniBankPosListModel();
            model.AvailableBankList.Insert(0,
                new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = " " });
            var bankList = await _bankPosService.GetBankPosList();
            foreach (var pos in bankList)
            {
                model.AvailableBankList.Add(new SelectListItem { Text = pos.Name, Value = pos.Id });
            }

            model.AvailableBankTypes.Insert(0,
                new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = " " });
            model.AvailableBankTypes = BankType.Bank.ToSelectList().ToList();

            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankPos/List.cshtml", model);
        }

        #region BankPos

        [PermissionAuthorizeAction(PermissionActionName.List)]
        [HttpPost]
        public async Task<IActionResult> BankPosList(DataSourceRequest command, OmniBankPosListModel model)
        {
            var bankPoses = await _bankPosService.GetBankPosPageList(model.SearchBankId,
                bankTypeId: model.SearchBankTypeId, pageIndex: command.Page - 1, pageSize: command.PageSize);
            var gridModel = new DataSourceResult {
                Data = bankPoses.Select(x =>
                {
                    var bModel = new OmniBankPosModel {
                        Id = x.Id,
                        Name = x.Name,
                        SystemName = x.SystemName,
                        IsActive = x.IsActive,
                        PrimaryBank = x.PrimaryBank,
                        Primary = x.Primary,
                        PictureUrl = _pictureService.GetPictureUrl(x.PictureId).Result,
                        BankTypeName = Enum.GetName(typeof(BankType), x.BankTypeId),
                    };
                    return bModel;
                }),
                Total = bankPoses.TotalCount
            };
            return Json(gridModel);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        public IActionResult Create()
        {
            var model = new OmniBankPosModel {
                AvailableBankTypeList = BankType.Bank.ToSelectList(_localizationService).ToList()
            };
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankPos/Create.cshtml", model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Create)]
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public async Task<IActionResult> Create(OmniBankPosModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var bankPos = model.ToEntity();
                if (!string.IsNullOrEmpty(model.BankColor))
                    await _genericAttributeService.SaveAttribute(bankPos, "BankColor", model.BankColor);
                if (!string.IsNullOrEmpty(model.BankImageId))
                    await _genericAttributeService.SaveAttribute(bankPos, "BankImageId", model.BankImageId);
                bankPos.CreatedOnUtc = _dateTimeHelper.ConvertToUserTime(DateTime.Now, DateTimeKind.Local);
                bankPos.UpdatedOnUtc = _dateTimeHelper.ConvertToUserTime(DateTime.Now, DateTimeKind.Local);
                try
                {
                    await _bankPosService.InsertBankPos(bankPos);
                    SuccessNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankPos.Added"));
                    await _customerActivityService.InsertActivity("AddNewBank", bankPos.Id,
                        $"{bankPos.Name} -Yeni BankaPos Eklendi",
                        _workContext.CurrentCustomer);
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, e, _workContext.CurrentCustomer);
                    ErrorNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankPos.Error"));
                }

                return continueEditing ? RedirectToAction("Edit", new { id = bankPos.Id }) : RedirectToAction("List");
            }

            model.AvailableBankTypeList = BankType.Bank.ToSelectList(_localizationService).ToList();
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankPos/Create.cshtml", model);
        }

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        public async Task<IActionResult> Edit(string id)
        {
            var bankPos = await _bankPosService.GetBankPosId(id);
            var model = bankPos.ToModel();
            var parameterModel = new ParameterModel {
                NestPayModel = new NestPayModel {
                    ClientId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.ClientId)),
                    Password = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.Password)),
                    UserName = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.UserName)),
                    StoreKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.StoreKey)),
                    StoreType = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.StoreType)),
                    GatewayUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.GatewayUrl)),                   
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.VerifyUrl)),
                    ImcKod = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.ImcKod)),

                },
                DenizBankModel = new DenizBankModel {
                    ShopCode = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(DenizBankModel.ShopCode)),
                    UserCode = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(DenizBankModel.UserCode)),
                    StoreKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(DenizBankModel.StoreKey)),
                    UserPass = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(DenizBankModel.UserPass)),
                    GatewayUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(DenizBankModel.GatewayUrl)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(DenizBankModel.VerifyUrl)),
                },
                FinansBankModel = new FinansBankModel {
                    MbrId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(FinansBankModel.MbrId)),
                    MerchantId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(FinansBankModel.MerchantId)),
                    MerchantPass = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(FinansBankModel.MerchantPass)),
                    UserCode = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(FinansBankModel.UserCode)),
                    UserPass = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(FinansBankModel.UserPass)),
                    GatewayUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(FinansBankModel.GatewayUrl)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(FinansBankModel.VerifyUrl)),
                },
                GarantiBankModel = new GarantiBankModel {
                    TerminalId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalId)),
                    TerminalUserId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalUserId)),
                    TerminalMerchantId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalMerchantId)),
                    TerminalProvPassword = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalProvPassword)),
                    TerminalProvUserId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.TerminalProvUserId)),
                    StoreKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.StoreKey)),
                    Email = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.Email)),
                    CompanyName = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.CompanyName)),
                    GatewayUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.GatewayUrl)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(GarantiBankModel.VerifyUrl)),
                },
                IyzicoBankModel = new IyzicoBankModel {
                    ApiKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(IyzicoBankModel.ApiKey)),
                    ApiUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(IyzicoBankModel.ApiUrl)),
                    SecretKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(IyzicoBankModel.SecretKey)),
                },
                KuveytTurkModel = new KuveytTurkModel {
                    MerchantId =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(KuveytTurkModel.MerchantId)),
                    Password = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(KuveytTurkModel.Password)),
                    CustomerNumber = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(KuveytTurkModel.CustomerNumber)),
                    GatewayUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(KuveytTurkModel.GatewayUrl)),
                    UserName = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(KuveytTurkModel.UserName)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(KuveytTurkModel.VerifyUrl)),


                },
                ParaPayModel = new ParaPayModel {
                    PublicKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(ParaPayModel.PublicKey)),
                    PrivateKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(ParaPayModel.PrivateKey)),
                    BaseUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(ParaPayModel.BaseUrl)),
                    ThreeDInquiryUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(ParaPayModel.ThreeDInquiryUrl)),
                    
                },
                PayTrModel = new PayTrModel {
                    GatewayUrl =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(PayTrModel.GatewayUrl)),
                    MerchantId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(PayTrModel.MerchantId)),
                    MerchantKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(PayTrModel.MerchantKey)),
                    MerchantSalt = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(PayTrModel.MerchantSalt)),
                },
                VakıfBankModel = new VakıfBankModel {
                    MerchantId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(VakıfBankModel.MerchantId)),
                    Password = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(VakıfBankModel.Password)),
                    EnrollmentUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(VakıfBankModel.EnrollmentUrl)),
                    TerminalNo = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(VakıfBankModel.TerminalNo)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(VakıfBankModel.VerifyUrl)),
                },
                YapıKrediBankModel = new YapıKrediBankModel {
                    MerchantId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(YapıKrediBankModel.MerchantId)),
                    GatewayUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(YapıKrediBankModel.GatewayUrl)),
                    TerminalId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(YapıKrediBankModel.TerminalId)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(YapıKrediBankModel.VerifyUrl)),
                    PosNetId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(YapıKrediBankModel.PosNetId)),
                },
            };
            model.ParameterModel = parameterModel;
            model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, "BankColor");
            model.BankImageId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, "BankImageId");
            if (!string.IsNullOrEmpty(model.BankImageId))
                model.BankImageUrl = await _pictureService.GetPictureUrl(model.BankImageId, 150);
            model.AvailableBankTypeList.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            var bankType = Enum.GetValues(typeof(BankType));
            foreach (var item in bankType)
            {
                model.AvailableBankTypeList.Add(new SelectListItem {
                    Text = item.ToString(),
                    Value = ((int)item).ToString()
                });
            }

            model.AddBankInstallmentModel.AvailableBanks.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });

            var bankList = Enum.GetValues(typeof(BankNames));
            foreach (var item in bankList)
            {
                model.AddBankInstallmentModel.AvailableBanks.Add(new SelectListItem {
                    Text = item.ToString(),
                    Value = ((int)item).ToString(),
                    Selected = bankPos.SystemName == item.ToString()
                });
            }

            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankPos/Edit.cshtml", model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [FormValueRequired("save", "save-continue")]
        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        public async Task<IActionResult> Edit(OmniBankPosModel model, bool continueEditing)
        {
            var bankPos = model.ToEntity();

            if (ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(model.BankColor))
                    await _genericAttributeService.SaveAttribute(bankPos, "BankColor", model.BankColor);
                if (!string.IsNullOrEmpty(model.BankImageId))
                    await _genericAttributeService.SaveAttribute(bankPos, "BankImageId", model.BankImageId);
                switch (model.SystemName)
                {
                    case "AkBank":
                    case "IsBankasi":
                    case "HalkBank":
                    case "ZiraatBankasi":
                    case "TurkEkonomiBankasi":
                    case "IngBank":
                    case "AnadoluBank":
                    case "HSBC":
                    case "TurkiyeFinans":
                    case "SekerBank":
                        if (!string.IsNullOrEmpty(model.ParameterModel.NestPayModel.ClientId))
                            await _genericAttributeService.SaveAttribute(bankPos, "ClientId", model.ParameterModel.NestPayModel.ClientId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.NestPayModel.Password))
                            await _genericAttributeService.SaveAttribute(bankPos, "Password", model.ParameterModel.NestPayModel.Password);
                        if (!string.IsNullOrEmpty(model.ParameterModel.NestPayModel.StoreKey))
                            await _genericAttributeService.SaveAttribute(bankPos, "StoreKey", model.ParameterModel.NestPayModel.StoreKey);
                        if (!string.IsNullOrEmpty(model.ParameterModel.NestPayModel.StoreType))
                            await _genericAttributeService.SaveAttribute(bankPos, "StoreType", model.ParameterModel.NestPayModel.StoreType);
                        if (!string.IsNullOrEmpty(model.ParameterModel.NestPayModel.UserName))
                            await _genericAttributeService.SaveAttribute(bankPos, "UserName", model.ParameterModel.NestPayModel.UserName);
                        if (!string.IsNullOrEmpty(model.ParameterModel.NestPayModel.GatewayUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "GatewayUrl", model.ParameterModel.NestPayModel.GatewayUrl);
                        if (!string.IsNullOrEmpty(model.ParameterModel.NestPayModel.VerifyUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "VerifyUrl", model.ParameterModel.NestPayModel.VerifyUrl);
                        if (!string.IsNullOrEmpty(model.ParameterModel.NestPayModel.ImcKod))
                            await _genericAttributeService.SaveAttribute(bankPos, "ImcKod", model.ParameterModel.NestPayModel.ImcKod);
                        break;
                    case "DenizBank":
                        if (!string.IsNullOrEmpty(model.ParameterModel.DenizBankModel.ShopCode))
                            await _genericAttributeService.SaveAttribute(bankPos, "ShopCode", model.ParameterModel.DenizBankModel.ShopCode);
                        if (!string.IsNullOrEmpty(model.ParameterModel.DenizBankModel.UserCode))
                            await _genericAttributeService.SaveAttribute(bankPos, "UserCode", model.ParameterModel.DenizBankModel.UserCode);
                        if (!string.IsNullOrEmpty(model.ParameterModel.DenizBankModel.StoreKey))
                            await _genericAttributeService.SaveAttribute(bankPos, "StoreKey", model.ParameterModel.DenizBankModel.StoreKey);
                        if (!string.IsNullOrEmpty(model.ParameterModel.DenizBankModel.UserPass))
                            await _genericAttributeService.SaveAttribute(bankPos, "UserPass", model.ParameterModel.DenizBankModel.UserPass);
                        if (!string.IsNullOrEmpty(model.ParameterModel.DenizBankModel.GatewayUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "GatewayUrl", model.ParameterModel.DenizBankModel.GatewayUrl);
                        if (!string.IsNullOrEmpty(model.ParameterModel.DenizBankModel.VerifyUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "VerifyUrl", model.ParameterModel.DenizBankModel.VerifyUrl);
                        break;
                    case "FinansBank":
                        if (!string.IsNullOrEmpty(model.ParameterModel.FinansBankModel.MbrId))
                            await _genericAttributeService.SaveAttribute(bankPos, "MbrId", model.ParameterModel.FinansBankModel.MbrId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.FinansBankModel.MerchantId))
                            await _genericAttributeService.SaveAttribute(bankPos, "MerchantId", model.ParameterModel.FinansBankModel.MerchantId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.FinansBankModel.UserCode))
                            await _genericAttributeService.SaveAttribute(bankPos, "UserCode", model.ParameterModel.FinansBankModel.UserCode);
                        if (!string.IsNullOrEmpty(model.ParameterModel.FinansBankModel.UserPass))
                            await _genericAttributeService.SaveAttribute(bankPos, "UserPass", model.ParameterModel.FinansBankModel.UserPass);
                        if (!string.IsNullOrEmpty(model.ParameterModel.FinansBankModel.MerchantPass))
                            await _genericAttributeService.SaveAttribute(bankPos, "MerchantPass", model.ParameterModel.FinansBankModel.MerchantPass);
                        if (!string.IsNullOrEmpty(model.ParameterModel.FinansBankModel.GatewayUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "GatewayUrl", model.ParameterModel.FinansBankModel.GatewayUrl);
                        if (!string.IsNullOrEmpty(model.ParameterModel.FinansBankModel.VerifyUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "VerifyUrl", model.ParameterModel.FinansBankModel.VerifyUrl);
                        break;
                    case "Garanti":
                        if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.TerminalId))
                            await _genericAttributeService.SaveAttribute(bankPos, "TerminalId", model.ParameterModel.GarantiBankModel.TerminalId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.StoreKey))
                            await _genericAttributeService.SaveAttribute(bankPos, "StoreKey", model.ParameterModel.GarantiBankModel.StoreKey);
                        if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.TerminalMerchantId))
                            await _genericAttributeService.SaveAttribute(bankPos, "TerminalMerchantId", model.ParameterModel.GarantiBankModel.TerminalMerchantId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.TerminalProvPassword))
                            await _genericAttributeService.SaveAttribute(bankPos, "TerminalProvPassword", model.ParameterModel.GarantiBankModel.TerminalProvPassword);
                        if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.TerminalUserId))
                            await _genericAttributeService.SaveAttribute(bankPos, "TerminalUserId", model.ParameterModel.GarantiBankModel.TerminalUserId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.TerminalProvUserId))
                            await _genericAttributeService.SaveAttribute(bankPos, "TerminalProvUserId", model.ParameterModel.GarantiBankModel.TerminalProvUserId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.Email))
                            await _genericAttributeService.SaveAttribute(bankPos, "Email", model.ParameterModel.GarantiBankModel.Email);
                        if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.CompanyName))
                            await _genericAttributeService.SaveAttribute(bankPos, "CompanyName", model.ParameterModel.GarantiBankModel.CompanyName);
                        if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.GatewayUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "GatewayUrl", model.ParameterModel.GarantiBankModel.GatewayUrl);
                        if (!string.IsNullOrEmpty(model.ParameterModel.GarantiBankModel.VerifyUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "VerifyUrl", model.ParameterModel.GarantiBankModel.VerifyUrl);


                        break;
                    case "KuveytTurk":
                        if (!string.IsNullOrEmpty(model.ParameterModel.KuveytTurkModel.MerchantId))
                            await _genericAttributeService.SaveAttribute(bankPos, "MerchantId", model.ParameterModel.KuveytTurkModel.MerchantId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.KuveytTurkModel.UserName))
                            await _genericAttributeService.SaveAttribute(bankPos, "UserName", model.ParameterModel.KuveytTurkModel.UserName);
                        if (!string.IsNullOrEmpty(model.ParameterModel.KuveytTurkModel.Password))
                            await _genericAttributeService.SaveAttribute(bankPos, "Password", model.ParameterModel.KuveytTurkModel.Password);
                        if (!string.IsNullOrEmpty(model.ParameterModel.KuveytTurkModel.GatewayUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "GatewayUrl", model.ParameterModel.KuveytTurkModel.GatewayUrl);

                        if (!string.IsNullOrEmpty(model.ParameterModel.KuveytTurkModel.CustomerNumber))
                                await _genericAttributeService.SaveAttribute(bankPos, "CustomerNumber", model.ParameterModel.KuveytTurkModel.CustomerNumber);
                       
                        if (!string.IsNullOrEmpty(model.ParameterModel.KuveytTurkModel.VerifyUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "VerifyUrl", model.ParameterModel.KuveytTurkModel.VerifyUrl);
                        break;
                    case "VakifBank":
                        if (!string.IsNullOrEmpty(model.ParameterModel.VakıfBankModel.MerchantId))
                            await _genericAttributeService.SaveAttribute(bankPos, "MerchantId", model.ParameterModel.VakıfBankModel.MerchantId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.VakıfBankModel.Password))
                            await _genericAttributeService.SaveAttribute(bankPos, "Password", model.ParameterModel.VakıfBankModel.Password);
                        if (!string.IsNullOrEmpty(model.ParameterModel.VakıfBankModel.TerminalNo))
                            await _genericAttributeService.SaveAttribute(bankPos, "TerminalNo", model.ParameterModel.VakıfBankModel.TerminalNo);
                        if (!string.IsNullOrEmpty(model.ParameterModel.VakıfBankModel.EnrollmentUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "EnrollmentUrl", model.ParameterModel.VakıfBankModel.EnrollmentUrl);
                        if (!string.IsNullOrEmpty(model.ParameterModel.VakıfBankModel.VerifyUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "VerifyUrl", model.ParameterModel.VakıfBankModel.VerifyUrl);
                        break;
                    case "Yapikredi":
                    case "Albaraka":
                        if (!string.IsNullOrEmpty(model.ParameterModel.YapıKrediBankModel.MerchantId))
                            await _genericAttributeService.SaveAttribute(bankPos, "MerchantId", model.ParameterModel.YapıKrediBankModel.MerchantId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.YapıKrediBankModel.TerminalId))
                            await _genericAttributeService.SaveAttribute(bankPos, "TerminalId", model.ParameterModel.YapıKrediBankModel.TerminalId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.YapıKrediBankModel.PosNetId))
                            await _genericAttributeService.SaveAttribute(bankPos, "PosNetId", model.ParameterModel.YapıKrediBankModel.PosNetId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.YapıKrediBankModel.GatewayUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "GatewayUrl", model.ParameterModel.YapıKrediBankModel.GatewayUrl);
                        if (!string.IsNullOrEmpty(model.ParameterModel.YapıKrediBankModel.VerifyUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "VerifyUrl", model.ParameterModel.YapıKrediBankModel.VerifyUrl);
                        break;
                    case "Iyzico":
                        if (!string.IsNullOrEmpty(model.ParameterModel.IyzicoBankModel.ApiKey))
                            await _genericAttributeService.SaveAttribute(bankPos, "ApiKey", model.ParameterModel.IyzicoBankModel.ApiKey);
                        if (!string.IsNullOrEmpty(model.ParameterModel.IyzicoBankModel.ApiUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "ApiUrl", model.ParameterModel.IyzicoBankModel.ApiUrl);
                        if (!string.IsNullOrEmpty(model.ParameterModel.IyzicoBankModel.SecretKey))
                            await _genericAttributeService.SaveAttribute(bankPos, "SecretKey", model.ParameterModel.IyzicoBankModel.SecretKey);
                        break;
                    case "IPara":
                        if (!string.IsNullOrEmpty(model.ParameterModel.ParaPayModel.PublicKey))
                            await _genericAttributeService.SaveAttribute(bankPos, "PublicKey", model.ParameterModel.ParaPayModel.PublicKey);
                        if (!string.IsNullOrEmpty(model.ParameterModel.ParaPayModel.PrivateKey))
                            await _genericAttributeService.SaveAttribute(bankPos, "PrivateKey", model.ParameterModel.ParaPayModel.PrivateKey);
                        if (!string.IsNullOrEmpty(model.ParameterModel.ParaPayModel.ThreeDInquiryUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "ThreeDInquiryUrl", model.ParameterModel.ParaPayModel.ThreeDInquiryUrl);
                        if (!string.IsNullOrEmpty(model.ParameterModel.ParaPayModel.BaseUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "BaseUrl", model.ParameterModel.ParaPayModel.BaseUrl);
                     
                        break;
                    case "PayTr":
                        if (!string.IsNullOrEmpty(model.ParameterModel.PayTrModel.MerchantId))
                            await _genericAttributeService.SaveAttribute(bankPos, "MerchantId", model.ParameterModel.PayTrModel.MerchantId);
                        if (!string.IsNullOrEmpty(model.ParameterModel.PayTrModel.MerchantKey))
                            await _genericAttributeService.SaveAttribute(bankPos, "MerchantKey", model.ParameterModel.PayTrModel.MerchantKey);
                        if (!string.IsNullOrEmpty(model.ParameterModel.PayTrModel.MerchantSalt))
                            await _genericAttributeService.SaveAttribute(bankPos, "MerchantSalt", model.ParameterModel.PayTrModel.MerchantSalt);
                        if (!string.IsNullOrEmpty(model.ParameterModel.PayTrModel.GatewayUrl))
                            await _genericAttributeService.SaveAttribute(bankPos, "GatewayUrl", model.ParameterModel.PayTrModel.GatewayUrl);
                        break;
                }
                try
                {
                    await _bankPosService.UpdateBankPos(bankPos);
                    SuccessNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankPos.Updated"));
                    await _customerActivityService.InsertActivity("UpdateBankPos", bankPos.Id,
                        $"{bankPos.Name} - Gücellendi",
                        _workContext.CurrentCustomer);
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, e, _workContext.CurrentCustomer);
                    ErrorNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankPos.Error"));
                    Console.WriteLine(e);
                    throw;
                }

                return continueEditing ? RedirectToAction("Edit", new { id = bankPos.Id }) : RedirectToAction("List");
            }

            var parameterModel = new ParameterModel {
                NestPayModel = new NestPayModel {
                    ClientId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.ClientId)),
                    Password = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.Password)),
                    UserName = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.UserName)),
                    StoreKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.StoreKey)),
                    StoreType = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.StoreType)),
                    GatewayUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.GatewayUrl)),                   
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos, nameof(NestPayModel.VerifyUrl)),
                },
                DenizBankModel = new DenizBankModel {
                    ShopCode = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(DenizBankModel.ShopCode)),
                    UserCode = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(DenizBankModel.UserCode)),
                    StoreKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(DenizBankModel.StoreKey)),
                    UserPass = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(DenizBankModel.UserPass)),
                    GatewayUrl =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(DenizBankModel.GatewayUrl)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(DenizBankModel.VerifyUrl)),
                },
                FinansBankModel = new FinansBankModel {
                    MbrId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(FinansBankModel.MbrId)),
                    MerchantId =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(FinansBankModel.MerchantId)),
                    MerchantPass =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(FinansBankModel.MerchantPass)),
                    UserCode = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(FinansBankModel.UserCode)),
                    UserPass = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(FinansBankModel.UserPass)),
                    GatewayUrl =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(FinansBankModel.GatewayUrl)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(FinansBankModel.VerifyUrl)),
                },
                GarantiBankModel = new GarantiBankModel {
                    TerminalId =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(GarantiBankModel.TerminalId)),
                    TerminalUserId =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(GarantiBankModel.TerminalUserId)),
                    TerminalMerchantId =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(GarantiBankModel.TerminalMerchantId)),
                    TerminalProvPassword =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(GarantiBankModel.TerminalProvPassword)),
                    TerminalProvUserId =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(GarantiBankModel.TerminalProvUserId)),
                    StoreKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(GarantiBankModel.StoreKey)),
                    Email = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(GarantiBankModel.Email)),
                    CompanyName =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(GarantiBankModel.CompanyName)),
                    GatewayUrl =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(GarantiBankModel.GatewayUrl)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(GarantiBankModel.VerifyUrl)),
                },
                IyzicoBankModel = new IyzicoBankModel {
                    ApiKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(IyzicoBankModel.ApiKey)),
                    ApiUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(IyzicoBankModel.ApiUrl)),
                    SecretKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(IyzicoBankModel.SecretKey)),
                },
                KuveytTurkModel = new KuveytTurkModel {
                    MerchantId =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(KuveytTurkModel.MerchantId)),
                    Password = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(KuveytTurkModel.Password)),
                    CustomerNumber =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(KuveytTurkModel.CustomerNumber)),
                    GatewayUrl =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(KuveytTurkModel.GatewayUrl)),
                    UserName = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(KuveytTurkModel.UserName)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(KuveytTurkModel.VerifyUrl)),
                },
                ParaPayModel = new ParaPayModel {
                    PublicKey = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(ParaPayModel.PublicKey)),
                    PrivateKey =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(ParaPayModel.PrivateKey)),
                    BaseUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(ParaPayModel.BaseUrl)),
                    ThreeDInquiryUrl =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(ParaPayModel.ThreeDInquiryUrl)),
                    HashString =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(ParaPayModel.HashString)),
                },
                PayTrModel = new PayTrModel {
                    GatewayUrl =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(PayTrModel.GatewayUrl)),
                    MerchantId =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(PayTrModel.MerchantId)),
                    MerchantKey =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(PayTrModel.MerchantKey)),
                    MerchantSalt =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(PayTrModel.MerchantSalt)),
                },
                VakıfBankModel = new VakıfBankModel {
                    MerchantId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(VakıfBankModel.MerchantId)),
                    Password = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(VakıfBankModel.Password)),
                    EnrollmentUrl =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(VakıfBankModel.EnrollmentUrl)),
                    TerminalNo =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(VakıfBankModel.TerminalNo)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(VakıfBankModel.VerifyUrl)),
                },
                YapıKrediBankModel = new YapıKrediBankModel {
                    MerchantId =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(YapıKrediBankModel.MerchantId)),
                    GatewayUrl =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(YapıKrediBankModel.GatewayUrl)),
                    TerminalId =
                        await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                            nameof(YapıKrediBankModel.TerminalId)),
                    VerifyUrl = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(YapıKrediBankModel.VerifyUrl)),
                    PosNetId = await _genericAttributeService.GetAttributesForEntity<string>(bankPos,
                        nameof(YapıKrediBankModel.PosNetId)),
                },
            };
            model.ParameterModel = parameterModel;
            model.AvailableBankTypeList.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            var bankType = Enum.GetValues(typeof(BankType));
            foreach (var item in bankType)
            {
                model.AvailableBankTypeList.Add(new SelectListItem {
                    Text = item.ToString(),
                    Value = ((int)item).ToString()
                });
            }
            model.AddBankInstallmentModel.AvailableBanks.Insert(0, new SelectListItem { Text = _localizationService.GetResource("Admin.Common.All"), Value = "0" });
            var bankList = Enum.GetValues(typeof(BankNames));
            foreach (var item in bankList)
            {
                model.AddBankInstallmentModel.AvailableBankPos.Add(new SelectListItem {
                    Text = item.ToString(),
                    Value = ((int)item).ToString(),
                    Selected = bankPos.SystemName == item.ToString()
                });
            }
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentBankPos/Edit.cshtml", model);
        }

        [HttpPost]
        [PermissionAuthorizeAction(PermissionActionName.Delete)]
        public async Task<IActionResult> Delete(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                var bankPos = await _bankPosService.GetBankPosId(id);
                try
                {
                    await _bankPosService.DeleteBankPos(bankPos);
                    SuccessNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankPos.Deleted"));
                    await _customerActivityService.InsertActivity("DeleteBankPos", bankPos.Id,
                        $"{bankPos.Name} - Silindi",
                        _workContext.CurrentCustomer);
                }
                catch (Exception e)
                {
                    _logger.Error(e.Message, e, _workContext.CurrentCustomer);
                    ErrorNotification(_localizationService.GetResource("Plugins.AllBank.Admin.BankPos.Error"));
                    Console.WriteLine(e);
                    throw;
                }
            }

            return RedirectToAction("List");
        }

        #endregion

        #region BankInstallment

        [PermissionAuthorizeAction(PermissionActionName.Preview)]
        [HttpPost]
        public async Task<IActionResult> BankInstallmentList(DataSourceRequest command, string bankPosId)
        {
            if (String.IsNullOrEmpty(bankPosId)) return null;
            var bankPos = await _bankPosService.GetBankPosId(bankPosId);
            var items = new List<OmniBankPosModel.BankInstallmentModel>();
            foreach (var item in bankPos.BankInstallments)
            {
                items.Add(new OmniBankPosModel.BankInstallmentModel {
                    Id = item.Id,
                    Percentage = item.Percentage,
                    BankId = item.BankId,
                    BankPosId = item.BankPosId,
                    NumberOfInstallment = item.NumberOfInstallment,
                    BankName = Enum.GetName(typeof(BankNames), item.BankId)
                });
            }

            var gridModel = new DataSourceResult {
                Data = items,
                Total = items.Count
            };
            return Json(gridModel);
        }

        public async Task<IActionResult> BankPosInstallmentAdd(string bankPosId, int numberOfInstallment, decimal percentage, int bankId)
        {
            var bankPos = await _bankPosService.GetBankPosId(bankPosId);
            if (bankPos == null)
                throw new ArgumentNullException("bankPosId");
            try
            {
                var installment = new BankInstallment {
                    NumberOfInstallment = numberOfInstallment,
                    Percentage = percentage,
                    BankId = bankId,
                    BankPosId = bankPos.Id
                };
                await _bankPosService.InsertBankPosInstallment(installment);
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
            var bankPos = await _bankPosService.GetBankPosId(model.BankPosId);
            if (bankPos == null)
                throw new ArgumentNullException("bankPosId");

            var bankPosInstallment = bankPos.BankInstallments.FirstOrDefault(x => x.Id == model.Id);
            if (bankPosInstallment == null)
                ModelState.AddModelError("", "Taksit bulunamadı");
            if (ModelState.IsValid)
            {
                try
                {
                    bankPosInstallment.NumberOfInstallment = model.NumberOfInstallment;
                    bankPosInstallment.Percentage = model.Percentage;
                    await _bankPosService.UpdateBankPosInstallment(bankPosInstallment);
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
            var bankPos = await _bankPosService.GetBankPosId(model.BankPosId);
            if (bankPos == null)
                throw new ArgumentNullException("bankPosId");
            var bankPosInstallment = bankPos.BankInstallments.FirstOrDefault(x => x.Id == model.Id);
            if (bankPosInstallment == null)
                ModelState.AddModelError("", "Taksit bulunamadı");
            if (ModelState.IsValid)
            {
                bankPosInstallment.BankPosId = model.BankPosId;
                await _bankPosService.DeleteBankPosInstallment(bankPosInstallment);
                return new NullJsonResult();
            }

            return ErrorForKendoGridJson(ModelState);
        }

        #endregion


        #endregion
    }
}

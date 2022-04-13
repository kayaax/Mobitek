using Grand.Core;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Framework.Controllers;
using Grand.Framework.Extensions;
using Grand.Framework.Mvc.Filters;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Iyzico;
using Grand.Plugin.Payments.AllBank.Iyzico.Models;
using Grand.Plugin.Payments.AllBank.Iyzico.Request;
using Grand.Plugin.Payments.AllBank.Models;
using Grand.Plugin.Payments.AllBank.Models.Enums;
using Grand.Plugin.Payments.AllBank.Requests;
using Grand.Plugin.Payments.AllBank.Results;
using Grand.Plugin.Payments.AllBank.Services;
using Grand.Services.Catalog;
using Grand.Services.Commands.Models.Orders;
using Grand.Services.Common;
using Grand.Services.Configuration;
using Grand.Services.Helpers;
using Grand.Services.Localization;
using Grand.Services.Media;
using Grand.Services.Orders;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grand.Domain.Logging;
using Grand.Domain.Payments;
using Grand.Services.Customers;
using Grand.Services.Discounts;
using Grand.Services.Logging;
using Grand.Services.Messages;



namespace Grand.Plugin.Payments.AllBank.Controllers
{
    public class PaymentAllBankController : BasePaymentController
    {
        #region Fields

        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly AllBankPaymentSettings _allBankPaymentSettings;
        private readonly IBankPosService _bankPosService;
        private readonly IBankBinService _bankBinService;
        private readonly IPictureService _pictureService;
        private readonly IWorkContext _workContext;
        private readonly IBankOrderService _bankOrderService;
        private readonly IDateTimeHelper _dateTimeHelper;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IMediator _mediator;
        private readonly IPaymentProviderFactory _paymentProviderFactory;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IWorkflowMessageService _workflowMessageService;
        private readonly ILogger _logger;
        private readonly ICustomerService _customerService;

        #endregion

        #region Ctor

        public PaymentAllBankController(ISettingService settingService,
            ILocalizationService localizationService,
            AllBankPaymentSettings allBankPaymentSettings,
            IBankPosService bankPosService,
            IBankBinService bankBinService,
            IPictureService pictureService,
            IWorkContext workContext,
            IBankOrderService bankOrderService,
            IDateTimeHelper dateTimeHelper,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IMediator mediator,
            IPaymentProviderFactory paymentProviderFactory,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IProductService productService,
            ICategoryService categoryService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IHttpContextAccessor httpContextAccessor,
            IGenericAttributeService genericAttributeService,
            IWorkflowMessageService workflowMessageService,
            ICustomerService customerService,
            ILogger logger)
        {
            _settingService = settingService;
            _localizationService = localizationService;
            _allBankPaymentSettings = allBankPaymentSettings;
            _bankPosService = bankPosService;
            _bankBinService = bankBinService;
            _pictureService = pictureService;
            _workContext = workContext;
            _bankOrderService = bankOrderService;
            _dateTimeHelper = dateTimeHelper;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _mediator = mediator;
            _paymentProviderFactory = paymentProviderFactory;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _productService = productService;
            _categoryService = categoryService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _httpContextAccessor = httpContextAccessor;
            _genericAttributeService = genericAttributeService;
            _workflowMessageService = workflowMessageService;
            _customerService = customerService;
            _logger = logger;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task<OmniBankOrder> AddBankOrder(InstallmentViewModel model)
        {
            var bankOrder = new OmniBankOrder
            {
                PaymentInfo = JsonConvert.SerializeObject(model),
                CustomerId = _workContext.CurrentCustomer.Id
            };
            bankOrder.MarkAsCreated();
            bankOrder.PaymentInfoSession = model.PaymentInfoSession;
            await _bankOrderService.InsertBankOrder(bankOrder);
            return bankOrder;
        }

        /// <summary>
        /// </summary>
        /// <param name="model"></param>
        /// <param name="installment"></param>
        private void AddBankinstallment(InstallmentViewModel model, BankInstallment installment)
        {
            var installmentAmount = model.TotalAmount;
            var installmentTotalAmount = installmentAmount;
            if (installment.Percentage > 0)
            {
                installmentTotalAmount = Math.Round(model.TotalAmount + ((model.TotalAmount * installment.Percentage) / 100),
                        2, MidpointRounding.AwayFromZero);
            }
            installmentAmount = Math.Round(installmentTotalAmount / installment.NumberOfInstallment, 2, MidpointRounding.AwayFromZero);
            model.InstallmentRates.Add(new InstallmentViewModel.InstallmentRate
            {
                Text = $"{installment.NumberOfInstallment.ToString()} Taksit",
                Installment = installment.NumberOfInstallment,
                Rate = installment.Percentage,
                Amount = installmentAmount.ToString("N2"),
                AmountValue = installmentAmount,
                TotalAmount = installmentTotalAmount.ToString("N2"),
                TotalAmountValue = installmentTotalAmount
            });
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Options IyzicoOptions()
        {
            var options = new Options
            {
                ApiKey = "sandbox-VQM1Anw3zsWfx5g1YeypO9095gKQ83Ea",
                SecretKey = "sandbox-pUj1y0QNA4cmiDprNDEuXc3iHxaRFuYa",
                BaseUrl = "https://sandbox-api.iyzipay.com"
            };
            return options;
        }

        /// <summary>
        /// </summary>
        /// <param name="payment"></param>
        /// <param name="verifyResult"></param>
        private async Task CancelOrder(OmniBankOrder payment, VerifyGatewayResult verifyResult)
        {

            var order = await _orderService.GetOrderByGuid(payment.OrderGuid);
            var customer = await _customerService.GetCustomerById(order.CustomerId);
            payment.MarkAsFailed(verifyResult.ErrorMessage, $"{verifyResult.ErrorMessage} - {verifyResult.ErrorCode}");
            await _mediator.Send(new ReOrderCommand() { Order = await _orderService.GetOrderByGuid(payment.OrderGuid) });
            await _orderService.DeleteOrder(await _orderService.GetOrderByGuid(payment.OrderGuid)).ConfigureAwait(false);
            await _workflowMessageService.SendOrderCancelledCustomerNotification(order, _workContext.WorkingLanguage.Id);
            await _workflowMessageService.SendOrderCancelledStoreOwnerNotification(order, _workContext.WorkingLanguage.Id);
            _logger.Information($"{order.OrderNumber} no'lu { customer.Email } ait siparişle ilgili hata oluştu yeniden ödeme için sepete eklendi", null, customer);
        }

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area("Admin")]
        public IActionResult Configure()
        {
            var model = new ConfigurationModel
            {
                AdditionalFee = _allBankPaymentSettings.AdditionalFee,
                DescriptionText = _allBankPaymentSettings.DescriptionText,
                IsInstallment = _allBankPaymentSettings.IsInstallment,
                AdditionalFeePercentage = _allBankPaymentSettings.AdditionalFeePercentage,
                TestMode = _allBankPaymentSettings.TestMode,
                InstallmentCategoryBased = _allBankPaymentSettings.InstallmentCategoryBased
            };
            // ReSharper disable once Mvc.ViewNotResolved
            return View("~/Plugins/Payments.AllBank/Views/PaymentConfigure/Configure.cshtml", model);
        }

        [AuthorizeAdmin]
        [Area("Admin")]
        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        public async Task<IActionResult> Configure(ConfigurationModel model, bool continueEditing)
        {
            if (!ModelState.IsValid)
                return Configure();
            _allBankPaymentSettings.AdditionalFee = model.AdditionalFee;
            _allBankPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            _allBankPaymentSettings.DescriptionText = model.DescriptionText;
            _allBankPaymentSettings.IsInstallment = model.IsInstallment;
            _allBankPaymentSettings.TestMode = model.TestMode;
            _allBankPaymentSettings.InstallmentCategoryBased = model.InstallmentCategoryBased;
            await _settingService.SaveSetting(_allBankPaymentSettings);
            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));
            return Configure();
        }

        /// <summary>
        /// Taksit bilgileri
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> GetInstallments([FromBody] InstallmentViewModel model)
        {
            var cart = _shoppingCartService.GetShoppingCart(_storeContext.CurrentStore.Id, ShoppingCartType.ShoppingCart);
            if (!cart.Any())
            {
                model.ErrorMessage = _localizationService.GetResource("shoppingcart.cartisempty");
                return Ok(model);
            }
            var cartTotal = (await _orderTotalCalculationService.GetShoppingCartTotal(cart)).shoppingCartTotal;
            if (cartTotal.HasValue)
            {
                //peşin ödeme eklendi
                model.AddCashRate(cartTotal.Value);
                model.AddOrderNumber();
                //total eklendi
                model.TotalAmount = cartTotal.Value;
                model.ShoppingCartItems = cart;
            }

            //kategori bazlı taksitlendirme var mı?
            model.InstallmentCategoryBased = _allBankPaymentSettings.InstallmentCategoryBased;
            model.CurrencyCode = _workContext.WorkingCurrency.CurrencyCode == "TRY"
                ? "TL"
                : _workContext.WorkingCurrency.CurrencyCode;
            //Gelen bin numarası sistemde kayıtlı mı?
            var bankBin = await _bankBinService.GetBankBin(model.Prefix);
            //tek çekim default bank var mı?
            IPagedList<OmniBankPos> bankPosList = await _bankPosService.GetBankPosPageList(showHidden: true);
            var bankPosListImg = await _bankPosService.GetBankPosList();
            OmniBankPos primaryBankPos;
            string bankImgId = string.Empty;
            string pictureId;
            //bank order tablosuna bakılıyor session 
            var paymentInfoModel = _httpContextAccessor.HttpContext.Session.Get<PaymentInfoModel>("PaymentInfoModel");
            model.PaymentInfoSession = paymentInfoModel.PaymentInfoSession;
            OmniBankOrder bankOrder = await _bankOrderService.GetBankOrderPaymentSession(paymentInfoModel.PaymentInfoSession);
            //bin tanımlı değilse Iyzico demo hesabı ile bin sorgulaması yapılarak tabloya ekleniyor
            if (bankBin == null)
            {
                //burası tamamen bin almak için kullanılmıştır rakamlar farazi
                RetrieveInstallmentInfoRequest request = new RetrieveInstallmentInfoRequest
                {
                    Locale = "tr",
                    ConversationId = "123456789",
                    BinNumber = model.Prefix,
                    Price = "100"
                };
                InstallmentInfo installmentInfo = InstallmentInfo.Retrieve(request, IyzicoOptions());

                if (installmentInfo.Status == "success")
                {
                    var bin = new OmniBankBin
                    {
                        BinNumber = model.Prefix,
                        BankName = installmentInfo.InstallmentDetails[0].BankName,
                        CardAssociation = installmentInfo.InstallmentDetails[0].CardAssociation,
                        CardFamilyName = installmentInfo.InstallmentDetails[0].CardFamilyName,
                        BankCode = installmentInfo.InstallmentDetails[0].BankCode.ToString(),
                        BusinessCard = installmentInfo.InstallmentDetails[0].Commercial,
                        Force3Ds = installmentInfo.InstallmentDetails[0].Force3Ds,
                        CardType = installmentInfo.InstallmentDetails[0].CardType
                    };
                    await _bankBinService.InsertBankBin(bin);
                    bankBin = bin;
                }
            }

            //iyzico sorgulamasına ragmen bin yoksa peşin olarak tahsilat yapılacak
            if (bankBin == null)
            {
                //tekçekim için kullanılacak banka bulunur.
                primaryBankPos = bankPosList.FirstOrDefault(b => b.Primary);
                if (primaryBankPos != null)
                {
                    model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                    model.BankLogo = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(primaryBankPos.PictureId), 150);
                    model.BankPos = primaryBankPos;
                    model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(primaryBankPos, "BankColor");
                    bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(primaryBankPos, "BankImageId");
                    if (bankImgId != null)
                    {
                        model.BankImageUrl = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId), 150);
                    }
                }
                else
                {
                    primaryBankPos = bankPosList.FirstOrDefault(b => b.Primary && !b.PrimaryBank);
                    if (primaryBankPos != null)
                    {
                        model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                        model.BankLogo =
                            await _pictureService.GetPictureUrl(
                                await _pictureService.GetPictureById(primaryBankPos.PictureId), 150);
                        model.BankPos = primaryBankPos;
                        model.BankColor =
                            await _genericAttributeService.GetAttributesForEntity<string>(primaryBankPos, "BankColor");
                        bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(primaryBankPos,
                            "BankImageId");
                    }

                    if (bankImgId != null)
                    {
                        model.BankImageUrl = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId), 150);
                    }
                }
                //bank order tablosuna ekleniyor
                if (bankOrder == null)
                {
                    bankOrder = await AddBankOrder(model);
                    bankOrder.PaymentAmount = model.TotalAmount.ToString("0.00", new CultureInfo("en-EN").NumberFormat);
                    await _bankOrderService.UpdateBankOrder(bankOrder);
                }
                model.BankOrderId = bankOrder.Id;
                return Ok(model);
            }
            model.BankBin = bankBin;
            var bankCode = Convert.ToInt32(bankBin.BankCode);
            var systemName = Enum.GetName(typeof(BankNames), bankCode);
            //sistem genelinde taksit yoksa ...
            if (!_allBankPaymentSettings.IsInstallment)
            {
                //tek çekim ise ve anlaşmalı banka varsa burası çalışır.
                primaryBankPos = bankPosList.FirstOrDefault(b => b.Primary && !b.PrimaryBank);
                switch (primaryBankPos.BankTypeId)
                {
                    case (int)BankType.Ortak:
                        {
                            pictureId = bankPosList.FirstOrDefault(x => x.SystemName == systemName)?.PictureId;
                            model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                            model.BankLogo = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(pictureId), 150);
                            model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(primaryBankPos, "BankColor");
                            bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(bankPosListImg.FirstOrDefault(x => x.SystemName == systemName), "BankImageId");
                            if (bankImgId != null)
                            {
                                model.BankImageUrl = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId), 150);
                            }
                            break;
                        }
                    case (int)BankType.Bank:
                        {
                            model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                            model.BankLogo = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(primaryBankPos.PictureId), 150);
                            model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(bankPosListImg.FirstOrDefault(x => x.SystemName == systemName),
                                                  "BankColor");
                            bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(bankPosListImg.FirstOrDefault(x => x.SystemName == systemName),
                                            "BankImageId");
                            if (bankImgId != null)
                            {
                                model.BankImageUrl = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId), 150);
                            }
                            break;
                        }
                    default:
                        {
                            primaryBankPos = bankPosList.FirstOrDefault(b => b.Primary && b.PrimaryBank);
                            pictureId = bankPosList.FirstOrDefault(x => x.SystemName == systemName)?.PictureId;
                            model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                            model.BankLogo = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(pictureId), 150);
                            model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(bankPosListImg.FirstOrDefault(x => x.SystemName == systemName),
                                                  "BankColor");
                            bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(bankPosListImg.FirstOrDefault(x => x.SystemName == systemName),
                                            "BankImageId");
                            if (bankImgId != null)
                            {
                                model.BankImageUrl = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId), 150);
                            }

                            break;
                        }
                }

                //banka tipine bakılıyor iyzico veya diğer oratk ödmeler mi yoksa banka posumu kullanılıyor
                model.BankPos = primaryBankPos;
                //bank order tablosuna ekleniyor
                if (bankOrder == null)
                {
                    bankOrder = await AddBankOrder(model);
                    bankOrder.PaymentAmount = model.TotalAmount.ToString("0.00", new CultureInfo("en-EN").NumberFormat);
                    await _bankOrderService.UpdateBankOrder(bankOrder);
                }
                model.BankOrderId = bankOrder.Id;
                return Ok(model);
            }
            //site bazında varsayılan pos bulunuyor

            primaryBankPos = (bankPosList.FirstOrDefault(x => x.SystemName == Enum.GetName(typeof(BankNames), bankCode) && x.PrimaryBank) ??
                              bankPosList.FirstOrDefault(x => x.SystemName == Enum.GetName(typeof(BankNames), bankCode))) ?? bankPosList.FirstOrDefault(x => x.PrimaryBank);
            //banka tipine bakılıyor iyzico veya diğer ortak ödmeler mi yoksa banka posumu kullanılıyor
            if (primaryBankPos == null) return Ok(model);
            {
                model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                List<BankInstallment> bankInstallments;
                var categoryMaxInstallCount = 0;
                List<Category> listCategory = new List<Category>();
                var categoryInstallmentList = await _bankPosService.GetBankInstallmentCategoryList();
                if (primaryBankPos.BankTypeId == (int)BankType.Ortak)
                {
                    var bank = Enum.Parse(typeof(BankNames), primaryBankPos.SystemName);
                    if (bankBin.CardType.Contains("CREDIT_CARD"))
                    {
                        bankInstallments = primaryBankPos.BankInstallments
                            .Where(x => x.BankId == Convert.ToInt32(bankBin.BankCode)).ToList();
                        if (model.InstallmentCategoryBased)
                        {
                            foreach (var cartItem in cart)
                            {
                                var product = await _productService.GetProductById(cartItem.ProductId);
                                var categoryProduct = product.ProductCategories.FirstOrDefault();
                                if (categoryProduct == null) continue;
                                var category = await _categoryService.GetCategoryById(categoryProduct.CategoryId);
                                listCategory.Add(category);
                            }

                            foreach (var category in listCategory)
                            {
                                foreach (var bankInstallmentCategory in categoryInstallmentList)
                                {
                                    if (category.Id != bankInstallmentCategory.CategoryId) continue;
                                    if (bankInstallmentCategory.MaxInstallment == 0)
                                        break;
                                    categoryMaxInstallCount = bankInstallmentCategory.MaxInstallment;
                                }
                            }

                            if (bankInstallments.Count > 0 && categoryMaxInstallCount != 0 ||
                                categoryMaxInstallCount != 1)
                            {
                                foreach (var installment in bankInstallments.TakeWhile(installment =>
                                    installment.NumberOfInstallment != categoryMaxInstallCount))
                                {
                                    AddBankinstallment(model, installment);
                                }
                            }

                            pictureId = bankPosList
                                .FirstOrDefault(x => x.SystemName == systemName)?.PictureId;
                            model.BankId = bankInstallments[0].BankId;
                            model.BankPos = primaryBankPos;
                            model.BankLogo =
                                await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(pictureId),
                                    150);
                            model.BankName = bank.ToString();
                            model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                            bankOrder ??= await AddBankOrder(model);
                            bankOrder.PaymentAmount = model.TotalAmount.ToString("0.00", new CultureInfo("en-EN").NumberFormat);
                            await _bankOrderService.UpdateBankOrder(bankOrder);
                            model.BankOrderId = bankOrder.Id;
                            model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(
                                bankPosListImg.FirstOrDefault(x => x.SystemName == systemName), "BankColor");
                            bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(
                                bankPosListImg.FirstOrDefault(x => x.SystemName == systemName), "BankImageId");
                            if (bankImgId != null)
                            {
                                model.BankImageUrl =
                                    await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId),
                                        150);
                            }
                            return Ok(model);
                        }

                        model.InstallmentCount = bankInstallments.Count;
                        if (bankInstallments.Count > 0)
                        {
                            foreach (BankInstallment installment in bankInstallments)
                            {
                                AddBankinstallment(model, installment);
                            }

                            pictureId = bankPosList
                                .FirstOrDefault(x => x.SystemName == systemName)?.PictureId;
                            model.BankId = bankInstallments[0].BankId;
                            model.BankPos = primaryBankPos;
                            model.BankLogo = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(pictureId), 150);
                            model.BankName = bank.ToString();
                            model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                            model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(
                                bankPosListImg.FirstOrDefault(x =>
                                    x.SystemName == systemName), "BankColor");
                            bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(
                                bankPosListImg.FirstOrDefault(x => x.SystemName == systemName), "BankImageId");
                            if (bankImgId != null)
                            {
                                model.BankImageUrl = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId),
                                                         150);
                            }
                            bankOrder ??= await AddBankOrder(model);
                            bankOrder.PaymentAmount = model.TotalAmount.ToString("0.00", new CultureInfo("en-EN").NumberFormat);
                            await _bankOrderService.UpdateBankOrder(bankOrder);
                            model.BankOrderId = bankOrder.Id;
                            return Ok(model);
                        }
                    }

                    model.BankPos = primaryBankPos;
                    model.BankLogo = await _pictureService.GetPictureUrl(
                        (await _pictureService.GetPictureById(primaryBankPos.PictureId)), 150);
                    model.BankName = bank.ToString();
                    model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                    model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(
                        bankPosListImg.FirstOrDefault(x => x.SystemName == Enum.GetName(typeof(BankNames), bankCode)),
                        "BankColor");
                    bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(
                        bankPosListImg.FirstOrDefault(x => x.SystemName == Enum.GetName(typeof(BankNames), bankCode)),
                        "BankImageId");
                    if (bankImgId != null)
                    {
                        model.BankImageUrl =
                            await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId), 150);
                    }
                    bankOrder ??= await AddBankOrder(model);
                    bankOrder.PaymentAmount = model.TotalAmount.ToString("0.00", new CultureInfo("en-EN").NumberFormat);
                    await _bankOrderService.UpdateBankOrder(bankOrder);
                    model.BankOrderId = bankOrder.Id;
                    return Ok(model);
                }
                else
                {
                    var bank = Enum.Parse(typeof(BankNames), primaryBankPos.SystemName);
                    if (bankBin.CardType.Contains("CREDIT_CARD") || primaryBankPos.SystemName == Enum.GetName(typeof(BankNames), (int)bank))
                    {
                        bankInstallments = primaryBankPos.BankInstallments.ToList();
                        if (model.InstallmentCategoryBased)
                        {
                            foreach (var cartItem in cart)
                            {
                                var product = await _productService.GetProductById(cartItem.ProductId);
                                var categoryProduct = product.ProductCategories.FirstOrDefault();
                                if (categoryProduct == null) continue;
                                var category = await _categoryService.GetCategoryById(categoryProduct.CategoryId);
                                listCategory.Add(category);
                            }

                            foreach (var category in listCategory)
                            {
                                foreach (var bankInstallmentCategory in categoryInstallmentList)
                                {
                                    if (category.Id != bankInstallmentCategory.CategoryId) continue;
                                    if (bankInstallmentCategory.MaxInstallment == 0)
                                        break;
                                    categoryMaxInstallCount = bankInstallmentCategory.MaxInstallment;
                                }
                            }

                            if (bankInstallments.Count > 0 && categoryMaxInstallCount != 0 || categoryMaxInstallCount != 1)
                            {
                                foreach (var installment in bankInstallments.TakeWhile(installment =>
                                    installment.NumberOfInstallment != categoryMaxInstallCount))
                                {
                                    AddBankinstallment(model, installment);
                                }
                            }

                            model.InstallmentCount = categoryMaxInstallCount;
                            model.BankId = bankInstallments[0].BankId;
                            model.BankPos = primaryBankPos;
                            model.BankLogo =
                                await _pictureService.GetPictureUrl((await _pictureService.GetPictureById(primaryBankPos.PictureId)), 150);
                            model.BankName = bank.ToString();
                            model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                            model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(bankPosListImg.FirstOrDefault(x => x.SystemName == systemName), "BankColor");
                            bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(bankPosListImg.FirstOrDefault(x => x.SystemName == systemName), "BankImageId");
                            if (bankImgId != null)
                            {
                                model.BankImageUrl = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId), 150);
                            }
                            bankOrder ??= await AddBankOrder(model);
                            bankOrder.PaymentAmount = model.TotalAmount.ToString("0.00", new CultureInfo("en-EN").NumberFormat);
                            await _bankOrderService.UpdateBankOrder(bankOrder);
                            model.BankOrderId = bankOrder.Id;
                            return Ok(model);
                        }

                        pictureId = bankPosList.FirstOrDefault(x => x.SystemName == systemName)?.PictureId;
                        if (bankInstallments.Count > 0)
                        {
                            foreach (BankInstallment installment in bankInstallments)
                            {
                                AddBankinstallment(model, installment);
                            }

                            model.BankId = bankInstallments[0].BankId;
                            model.BankPos = primaryBankPos;
                            model.BankLogo = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(pictureId), 150);
                            model.BankName = bank.ToString();
                            model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                            model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(primaryBankPos, "BankColor");
                            bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(primaryBankPos, "BankImageId");
                            if (bankImgId != null)
                            {
                                model.BankImageUrl = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId), 150);
                            }
                            bankOrder ??= await AddBankOrder(model);
                            bankOrder.PaymentAmount = model.TotalAmount.ToString("0.00", new CultureInfo("en-EN").NumberFormat);
                            await _bankOrderService.UpdateBankOrder(bankOrder);
                            model.BankOrderId = bankOrder.Id;
                            return Ok(model);
                        }
                    }
                    model.BankPos = primaryBankPos;
                    model.BankLogo = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(primaryBankPos.PictureId), 150);
                    model.BankName = bank.ToString();
                    model.CardType = Enum.GetName(typeof(BankType), primaryBankPos.BankTypeId);
                    model.BankColor = await _genericAttributeService.GetAttributesForEntity<string>(bankPosListImg.FirstOrDefault(x => x.SystemName == primaryBankPos.SystemName),
                                          "BankColor");
                    bankImgId = await _genericAttributeService.GetAttributesForEntity<string>(bankPosListImg.FirstOrDefault(x => x.SystemName == primaryBankPos.SystemName),
                        "BankImageId");
                    if (bankImgId != null)
                    {
                        model.BankImageUrl = await _pictureService.GetPictureUrl(await _pictureService.GetPictureById(bankImgId), 150);
                    }
                    bankOrder ??= await AddBankOrder(model);
                    bankOrder.PaymentAmount = model.TotalAmount.ToString("0.00", new CultureInfo("en-EN").NumberFormat);
                    await _bankOrderService.UpdateBankOrder(bankOrder);
                    model.BankOrderId = bankOrder.Id;
                    return Ok(model);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> CallBack(IFormCollection form)
        {
            string merchantoid = form["merchant_oid"];
            var formDic = form.ToDictionary(key => key.Key, value => value.Value);
            await _logger.InsertLog(LogLevel.Information, "PaytrDönüş", merchantoid);
            var bankOrder = await _bankOrderService.GetBankOrderOrderNumber(merchantoid);
            bankOrder.BankResponse = JsonConvert.SerializeObject(formDic);
            await _logger.InsertLog(LogLevel.Information, "PaytrDönüş", bankOrder.BankResponse);
            await _bankOrderService.UpdateBankOrder(bankOrder);
            PaymentGatewayRequest bankRequest = JsonConvert.DeserializeObject<PaymentGatewayRequest>(bankOrder.BankRequest);
            //create provider
            IPaymentProvider provider = _paymentProviderFactory.Create(bankRequest.BankName);
            VerifyGatewayRequest verifyRequest = new VerifyGatewayRequest
            {
                BankName = bankRequest.BankName,
                BankParameters = bankRequest.BankParameters
            };

            VerifyGatewayResult verifyResult = await provider.VerifyGateway(verifyRequest, bankRequest, form);
            verifyResult.OrderNumber = bankRequest.OrderNumber;
            bankOrder.BankResponse = JsonConvert.SerializeObject(new
            {
                verifyResult,
                parameters = form.Keys.ToDictionary(key => key, value => form[value].ToString())
            });
            var order = await _orderService.GetOrderByGuid(bankOrder.OrderGuid);
            await _logger.InsertLog(LogLevel.Information, "PaytrDönüş", verifyResult.Success.ToString());
            if (verifyResult.Installment > 1)
            {
                bankOrder.Installment = verifyResult.Installment;
            }
            if (verifyResult.Success)
            {
               
                bankOrder.MarkAsPaid(_dateTimeHelper.ConvertToUserTime(DateTime.UtcNow,DateTimeKind.Local));
                bankOrder.TransactionNumber = formDic["merchant_oid"];
                bankOrder.ReferenceNumber = formDic["merchant_oid"];
                bankOrder.PaymentResultSession = Guid.NewGuid();
                order.AuthorizationTransactionCode = bankOrder.ReferenceNumber;
                order.AuthorizationTransactionId = bankOrder.TransactionNumber;
                order.OrderTotal = bankOrder.TotalAmount;
                order.PaymentStatusId = (int)PaymentStatus.Paid;
                order.PaidDateUtc = DateTime.Now;
                order.OrderStatusId = (int)OrderStatus.Processing;
                await _orderService.InsertOrderNote(new OrderNote
                {
                    Note = "Order has been marked as paid",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,

                });
                await _orderService.UpdateOrder(order);
                await _bankOrderService.UpdateBankOrder(bankOrder);
                await _workflowMessageService.SendOrderPaidCustomerNotification(order, _workContext.WorkingLanguage.Id);
                await _workflowMessageService.SendOrderPaidStoreOwnerNotification(order, _workContext.WorkingLanguage.Id);
            }
            else
            {
                await CancelOrder(bankOrder, verifyResult);
                await _bankOrderService.UpdateBankOrder(bankOrder);
            }
            return Content("OK");
        }
        [HttpPost]
        public async Task<IActionResult> ProcessPayment([FromRoute(Name = "id")] Guid paymentInfoSession, IFormCollection form)
        {
            // bankorder tablosuna bakılyor.
            var bankOrder = await _bankOrderService.GetBankOrderPaymentSession(paymentInfoSession);
            //banka parametreleri alınıyor.
            PaymentGatewayRequest bankRequest = JsonConvert.DeserializeObject<PaymentGatewayRequest>(bankOrder.BankRequest);
            if (bankRequest == null)
            {
                VerifyGatewayResult failModel =
                    VerifyGatewayResult.Failed(_localizationService.GetResource("AllBank.Payment.Information.Invalid"));
                await CancelOrder(bankOrder, failModel);
                return RedirectToRoute("cart");
            }

            //create provider
            IPaymentProvider provider = _paymentProviderFactory.Create(bankRequest.BankName);
            VerifyGatewayRequest verifyRequest = new VerifyGatewayRequest
            {
                BankName = bankRequest.BankName,
                BankParameters = bankRequest.BankParameters
            };

            VerifyGatewayResult verifyResult = await provider.VerifyGateway(verifyRequest, bankRequest, form);
            verifyResult.OrderNumber = bankRequest.OrderNumber;

            //save bank response
            switch (bankRequest.BankName)
            {
                case BankNames.AkBank:
                case BankNames.IsBankasi:
                case BankNames.HalkBank:
                case BankNames.ZiraatBankasi:
                case BankNames.TurkEkonomiBankasi:
                case BankNames.IngBank:
                case BankNames.AnadoluBank:
                case BankNames.HSBC:
                case BankNames.TurkiyeFinans:
                case BankNames.SekerBank:
                    bankOrder.BankResponse = JsonConvert.SerializeObject(new
                    {
                        verifyResult,
                        parameters = form.Keys.ToDictionary(key => key, value => form[value].ToString().Split(",", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
                    });
                    break;
                default:
                    bankOrder.BankResponse = JsonConvert.SerializeObject(new
                    {
                        verifyResult,
                        parameters = form.Keys.ToDictionary(key => key, value => form[value].ToString())
                    });
                    break;
            }
            var order = await _orderService.GetOrderByGuid(bankOrder.OrderGuid);

            if (verifyResult.Installment > 1)
            {
                bankOrder.Installment = verifyResult.Installment;
            }

            if (verifyResult.ExtraInstallment > 1)
            {
                bankOrder.ExtraInstallment = verifyResult.ExtraInstallment;
            }

            if (verifyResult.Success)
            {
                bankOrder.TransactionNumber = verifyResult.TransactionId;
                bankOrder.ReferenceNumber = verifyResult.ReferenceNumber;
                bankOrder.PaymentAmount = verifyResult.Amount;
                //mark as paid
                bankOrder.MarkAsPaid(_dateTimeHelper.ConvertToUserTime(DateTime.UtcNow, DateTimeKind.Local));
                bankOrder.PaymentResultSession = Guid.NewGuid();
                order.AuthorizationTransactionCode = bankOrder.ReferenceNumber;
                order.AuthorizationTransactionId = bankOrder.TransactionNumber;
                order.OrderTotal = bankOrder.TotalAmount;
                order.PaymentStatusId = (int)PaymentStatus.Paid;
                order.PaidDateUtc = DateTime.Now;
                order.OrderStatusId = (int)OrderStatus.Processing;
                await _orderService.InsertOrderNote(new OrderNote
                {
                    Note = "Order has been marked as paid",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow,
                    OrderId = order.Id,

                });
                await _orderService.UpdateOrder(order);
                await _bankOrderService.UpdateBankOrder(bankOrder);
                await _workflowMessageService.SendOrderPaidCustomerNotification(order, _workContext.WorkingLanguage.Id);
                await _workflowMessageService.SendOrderPaidStoreOwnerNotification(order, _workContext.WorkingLanguage.Id);
                return RedirectToAction("ProcessPaymentGet", new { order.Id });
            }

            //hata oluştu ise siparişi iptal edip tekrar sepete atıyoruz
            await CancelOrder(bankOrder, verifyResult);
            await _bankOrderService.UpdateBankOrder(bankOrder);
            return RedirectToAction("ProcessPaymentCart", new { message = verifyResult.ErrorMessage });
        }

        public async Task<IActionResult> ProcessPaymentPaytr(string bankOrderId)
        {
            await _logger.InsertLog(LogLevel.Information, "ProcessPaymentPaytr Metodu", bankOrderId);
            var bankOrder = await _bankOrderService.GetBankOrderId(bankOrderId);
            var order = await _orderService.GetOrderByGuid(bankOrder.OrderGuid);
            Thread.Sleep(5000);
            return RedirectToAction("ProcessPaymentGet", new { order.Id });

        }
        public async Task<IActionResult> ProcessPaymentGet(string orderId)
        {
            await _logger.InsertLog(LogLevel.Information, "ProcessPaymentGet Metodu", orderId);
            var order = await _orderService.GetOrderById(orderId);
            _httpContextAccessor.HttpContext.Session.Set<PaymentInfoModel>("PaymentInfoModel", null);
            return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
        }

        public IActionResult ProcessPaymentCart(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                ErrorNotification("Bir Hata Oluştu Yeniden Deneyin");
            }
            else
            {
                ErrorNotification(message);
            }
            _httpContextAccessor.HttpContext.Session.Set<PaymentInfoModel>("PaymentInfoModel", null);

            return RedirectToAction("Cart", "ShoppingCart");
        }

        #endregion
    }
}

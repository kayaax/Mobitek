using Grand.Core;
using Grand.Core.Plugins;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Framework.Extensions;
using Grand.Framework.Menu;
using Grand.Plugin.Payments.AllBank.Iyzico.Models;
using Grand.Plugin.Payments.AllBank.Models;
using Grand.Plugin.Payments.AllBank.Models.Enums;
using Grand.Plugin.Payments.AllBank.Requests;
using Grand.Plugin.Payments.AllBank.Results;
using Grand.Plugin.Payments.AllBank.Services;
using Grand.Plugin.Payments.AllBank.Validators;
using Grand.Services.Catalog;
using Grand.Services.Cms;
using Grand.Services.Commands.Models.Orders;
using Grand.Services.Configuration;
using Grand.Services.Directory;
using Grand.Services.Localization;
using Grand.Services.Logging;
using Grand.Services.Orders;
using Grand.Services.Payments;
using Grand.Services.Tax;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RefundPaymentResult = Grand.Services.Payments.RefundPaymentResult;


namespace Grand.Plugin.Payments.AllBank
{
    public class AllBankPaymentProcessor : BasePlugin, IPaymentMethod, IWidgetPlugin, IAdminMenuPlugin
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly AllBankPaymentSettings _allBankPaymentSettings;
        private readonly IWebHelper _webHelper;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly ILanguageService _languageService;
        private readonly IInstallDataService _installDataService;
        private readonly IBankBinService _bankBinService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBankOrderService _bankOrderService;
        private readonly ICountryService _countryService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly ITaxService _taxService;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IWorkContext _workContext;
        private readonly IPaymentProviderFactory _paymentProviderFactory;
        private readonly IMediator _mediator;
        private readonly IOrderService _orderService;
        private readonly ILogger _logger;
        private readonly OrderSettings _orderSettings;


        #endregion

        #region Ctor

        public AllBankPaymentProcessor(ILocalizationService localizationService,
            AllBankPaymentSettings allBankPaymentSettings,
            IWebHelper webHelper,
            IOrderTotalCalculationService orderTotalCalculationService,
            ISettingService settingService,
            ILanguageService languageService,
            IInstallDataService installDataService,
            IBankBinService bankBinService,
            IHttpContextAccessor httpContextAccessor,
            IBankOrderService bankOrderService,
            ICountryService countryService,
            IPriceCalculationService priceCalculationService,
            ITaxService taxService, IProductService productService,
            ICategoryService categoryService,
            IWorkContext workContext,
            IPaymentProviderFactory paymentProviderFactory,
            IMediator mediator,
            IOrderService orderService,
            ILogger logger, OrderSettings orderSettings)
        {
            _localizationService = localizationService;
            _allBankPaymentSettings = allBankPaymentSettings;
            _webHelper = webHelper;
            _orderTotalCalculationService = orderTotalCalculationService;
            _settingService = settingService;
            _languageService = languageService;
            _installDataService = installDataService;
            _bankBinService = bankBinService;
            _httpContextAccessor = httpContextAccessor;
            _bankOrderService = bankOrderService;
            _countryService = countryService;
            _priceCalculationService = priceCalculationService;
            _taxService = taxService;
            _productService = productService;
            _categoryService = categoryService;
            _workContext = workContext;
            _paymentProviderFactory = paymentProviderFactory;
            _mediator = mediator;
            _orderService = orderService;
            _logger = logger;
            _orderSettings = orderSettings;
        }

        #endregion

        #region Utilities

        private void PrePareIyzicoBasketItemModel(IList<ShoppingCartItem> shoppingCartItems,
            PaymentGatewayRequest gatewayRequest)
        {
            var price = 0M;
            List<BasketItem> basketItems = new List<BasketItem>();
            foreach (var cartItem in shoppingCartItems)
            {
                var product = _productService.GetProductById(cartItem.ProductId).Result;
                var categoryProduct = product.ProductCategories.FirstOrDefault();
                var categoryName = string.Empty;
                if (categoryProduct != null)
                {
                    var category = _categoryService.GetCategoryById(categoryProduct.CategoryId).Result;
                    categoryName = category.Name;
                }

                var basketItem = new BasketItem {
                    Id = cartItem.Id,
                    Category1 = categoryName ?? "Genel",
                    Category2 = string.Empty,
                    Name = product.Name,
                    ItemType = BasketItemType.PHYSICAL.ToString()
                };

                var result = _taxService.GetProductPrice(product, _priceCalculationService.GetUnitPrice(cartItem, product).Result.unitprice).Result.productprice;
                var priceValue = result * cartItem.Quantity;
                price += priceValue;
                basketItem.Price = $"{Math.Round(priceValue, 2).ToString("0.00",new CultureInfo("en-EN"))}";
                basketItems.Add(basketItem);
            }

            gatewayRequest.IyzicoPrice = price;
            gatewayRequest.BasketItems = basketItems;
        }

        private async Task<PaymentGatewayRequest> PaymentGatewayRequest(ProcessPaymentRequest processPaymentRequest, InstallmentViewModel installmentViewModel)
        {

            PaymentGatewayRequest gatewayRequest = new PaymentGatewayRequest 
            {
                CardHolderName = processPaymentRequest.CreditCardName,
                CardNumber = processPaymentRequest.CreditCardNumber?.Replace(" ", string.Empty)
                    .Replace(" ", string.Empty),
                ExpireMonth = processPaymentRequest.CreditCardExpireMonth,
                ExpireYear = processPaymentRequest.CreditCardExpireYear,
                CvvCode = processPaymentRequest.CreditCardCvv2,
                Installment = installmentViewModel.CurrentInstallmentCount,
                TotalAmount = installmentViewModel.TotalAmount,
                OrderNumber = installmentViewModel.OrderNumber,
                CurrencyIsoCode = _workContext.WorkingCurrency.CurrencyCode,
                LanguageIsoCode = _workContext.WorkingLanguage.UniqueSeoCode.ToLower(),
                CustomerIpAddress = _webHelper.GetCurrentIpAddress(),
                CardType = installmentViewModel.CardType,
                CardAssociation = installmentViewModel.BankBin.CardAssociation,
                CardFamily = installmentViewModel.BankBin.CardFamilyName,
                TestMode = _allBankPaymentSettings.TestMode,
                BankParameters = installmentViewModel.BankPos.GenericAttributes.ToDictionary(key => key.Key, value => value.Value),
                ShoppingCartItems = installmentViewModel.ShoppingCartItems,
                Customer = _workContext.CurrentCustomer,
                Currency = _workContext.WorkingCurrency,
                Country = (await _countryService.GetCountryById(_workContext.CurrentCustomer
                    .BillingAddress
                    .CountryId))
            };
            return gatewayRequest;
        }

        #endregion


        #region Method

        public async Task<ProcessPaymentResult> ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var processPaymentResult = new ProcessPaymentResult {
                NewPaymentStatus = PaymentStatus.Pending,
                AllowStoringCreditCardNumber = true
            };
            var paymentInfoModel = _httpContextAccessor.HttpContext.Session.Get<PaymentInfoModel>("PaymentInfoModel");
            var bankOrder = await _bankOrderService.GetBankOrderPaymentSession(paymentInfoModel.PaymentInfoSession);
            InstallmentViewModel installmentViewModel = JsonConvert.DeserializeObject<InstallmentViewModel>(bankOrder.PaymentInfo);
            var gatewayRequest = await PaymentGatewayRequest(processPaymentRequest, installmentViewModel);
            bankOrder.OrderGuid = processPaymentRequest.OrderGuid;
            bankOrder.OrderNumber = installmentViewModel.OrderNumber;
            bankOrder.TotalAmount = installmentViewModel.TotalAmount;
            bankOrder.UserIpAddress = _httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();
            bankOrder.UserAgent = _httpContextAccessor.HttpContext.Request.Headers[HeaderNames.UserAgent];
            bankOrder.BankRequest = JsonConvert.SerializeObject(gatewayRequest);
            await _bankOrderService.UpdateBankOrder(bankOrder);
            return await Task.FromResult(processPaymentResult);
        }

        public async  Task PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var order = postProcessPaymentRequest.Order;
            var paymentInfoModel = _httpContextAccessor.HttpContext.Session.Get<PaymentInfoModel>("PaymentInfoModel");
            var bankOrder = await _bankOrderService.GetBankOrderPaymentSession(paymentInfoModel.PaymentInfoSession);
            InstallmentViewModel installmentViewModel = JsonConvert.DeserializeObject<InstallmentViewModel>(bankOrder.PaymentInfo);
            var gatewayRequest = JsonConvert.DeserializeObject<PaymentGatewayRequest>(bankOrder.BankRequest);
            gatewayRequest.Order = order;
            if (installmentViewModel.BankPos.BankTypeId == (int)BankType.Bank)
            {
                var bankProviderName = installmentViewModel.BankPos.SystemName;
                gatewayRequest.BankName = Enum.Parse<BankNames>(bankProviderName);
                bankOrder.BankRequest = JsonConvert.SerializeObject(gatewayRequest);
               await _bankOrderService.UpdateBankOrder(bankOrder);
            }
            else
            {
                switch (installmentViewModel.BankPos.SystemName)
                {
                    case "Iyzico":
                        PrePareIyzicoBasketItemModel(installmentViewModel.ShoppingCartItems, gatewayRequest);
                        gatewayRequest.BankName = Enum.Parse<BankNames>(BankNames.Iyzico.ToString());
                        bankOrder.BankRequest = JsonConvert.SerializeObject(gatewayRequest);
                        await _bankOrderService.UpdateBankOrder(bankOrder);
                        break;
                    case "PayTr":
                        gatewayRequest.BankName = Enum.Parse<BankNames>(BankNames.PayTr.ToString());
                        bankOrder.BankRequest = JsonConvert.SerializeObject(gatewayRequest);
                        await _bankOrderService.UpdateBankOrder(bankOrder);
                        break;
                    case "IPara":
                        gatewayRequest.BankName = Enum.Parse<BankNames>(BankNames.IPara.ToString());
                        bankOrder.BankRequest = JsonConvert.SerializeObject(gatewayRequest);
                        await _bankOrderService.UpdateBankOrder(bankOrder);
                        break;
                }
            }

            IPaymentProvider provider = _paymentProviderFactory.Create(gatewayRequest.BankName);
            gatewayRequest.CallbackUrl = gatewayRequest.BankName == BankNames.PayTr
                ? new Uri($"{_webHelper.GetStoreLocation()}PaymentAllBank/ProcessPaymentPaytr/{bankOrder.Id}")
                : new Uri($"{_webHelper.GetStoreLocation()}PaymentAllBank/ProcessPayment/{paymentInfoModel.PaymentInfoSession}");
            PaymentGatewayResult gatewayResult = provider.ThreeDGatewayRequest(gatewayRequest).Result;
            if (!gatewayResult.Success)
            {
                bankOrder.BankResponse = JsonConvert.SerializeObject(gatewayResult);
                await _mediator.Send(new ReOrderCommand { Order = order });
                await _orderService.DeleteOrder(order);
                await _bankOrderService.DeleteBankOrder(bankOrder);
                VerifyGatewayResult failModel = VerifyGatewayResult.Failed(gatewayResult.ErrorMessage);
                _logger.Error(failModel.ErrorMessage, customer: _workContext.CurrentCustomer);
                _webHelper.IsPostBeingDone = true;
                string location = $"{_webHelper.GetStoreLocation()}PaymentAllBank/ProcessPaymentCart/" + failModel.ErrorMessage;
                _httpContextAccessor.HttpContext.Response.Redirect(location);
            }
            if (gatewayResult.HtmlContent)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var response = httpContext.Response;
                response.Clear();
                bankOrder.BankResponse = JsonConvert.SerializeObject(gatewayResult);
                await _bankOrderService.UpdateBankOrder(bankOrder);
                _webHelper.IsPostBeingDone = true;
                var data = Encoding.UTF8.GetBytes(gatewayResult.HtmlFormContent);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength = data.Length;
                response.Body.WriteAsync(data, 0, data.Length).Wait();
            }
            else if (gatewayResult.Success)
            {
                var httpContext = _httpContextAccessor.HttpContext;
                var response = httpContext.Response;
                response.Clear();
                _webHelper.IsPostBeingDone = true;
                var model = _paymentProviderFactory.CreatePaymentFormHtml(gatewayResult.Parameters, gatewayResult.GatewayUrl);
                var data1 = Encoding.UTF8.GetBytes(model);
                response.ContentType = "text/html; charset=utf-8";
                response.ContentLength = data1.Length;
                response.Body.WriteAsync(data1, 0, data1.Length).Wait();
            }
           
        }

        public async Task<bool> HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            //return false;
            return await Task.FromResult(false);
        }

        public async Task<decimal> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            if (_allBankPaymentSettings.AdditionalFee <= 0)
                return _allBankPaymentSettings.AdditionalFee;

            decimal result;
            if (_allBankPaymentSettings.AdditionalFeePercentage)
            {
                //percentage
                var subtotal = await _orderTotalCalculationService.GetShoppingCartSubTotal(cart, true);
                result = (decimal)((((float)subtotal.subTotalWithDiscount) *
                                     ((float)_allBankPaymentSettings.AdditionalFee)) / 100f);
            }
            else
            {
                //fixed value
                result = _allBankPaymentSettings.AdditionalFee;
            }

            //return result;
            return await Task.FromResult(result);
        }

        public async Task<CapturePaymentResult> Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return await Task.FromResult(result);
        }

        public async Task<RefundPaymentResult> Refund(Grand.Services.Payments.RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return await Task.FromResult(result);
        }

        public async Task<VoidPaymentResult> Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return await Task.FromResult(result);
        }

        public async Task<ProcessPaymentResult> ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return await Task.FromResult(result);
        }

        public async Task<CancelRecurringPaymentResult> CancelRecurringPayment(
            CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return await Task.FromResult(result);
        }

        public async Task<bool> CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //it's not a redirection payment method. So we always return false
            return await Task.FromResult(false);
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentAllBank/Configure";
        }

        public async Task<IList<string>> ValidatePaymentForm(IFormCollection form)
        {
            var warnings = new List<string>();
            //validate
            var validator = new PaymentInfoValidator(_allBankPaymentSettings, _localizationService);
            var model = new PaymentInfoModel {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpirationDate = form["ExpirationDate"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                {
                    warnings.Add(error.ErrorMessage);
                }

            return await Task.FromResult(warnings);
        }

        public async Task<ProcessPaymentRequest> GetPaymentInfo(IFormCollection form)
        {
            var processPaymentRequest = new ProcessPaymentRequest {
                CreditCardType = form[nameof(PaymentInfoModel.BankCardTypeName)],
                CreditCardName = form[nameof(PaymentInfoModel.CardholderName)],
                CreditCardNumber = form[nameof(PaymentInfoModel.CardNumber)].ToString()
                    ?.Replace(" ", string.Empty).Replace(" ", string.Empty).Trim(),
                CreditCardCvv2 = form[nameof(PaymentInfoModel.CardCode)]
            };
            if (!string.IsNullOrWhiteSpace(form[nameof(PaymentInfoModel.ExpirationDate)]) &&
                int.TryParse(form[nameof(PaymentInfoModel.ExpirationDate)].ToString().Substring(0, 2),
                    out var expireMonth))
            {
                processPaymentRequest.CreditCardExpireMonth = expireMonth;
            }

            if (!string.IsNullOrWhiteSpace(form[nameof(PaymentInfoModel.ExpirationDate)]) &&
                int.TryParse(form[nameof(PaymentInfoModel.ExpirationDate)].ToString().Substring(3, 2),
                    out var expireYear))
            {
                processPaymentRequest.CreditCardExpireYear = expireYear;
            }
            var paymentInfoModel = _httpContextAccessor.HttpContext.Session.Get<PaymentInfoModel>("PaymentInfoModel");
            var bankOrder = await _bankOrderService.GetBankOrderPaymentSession(paymentInfoModel.PaymentInfoSession);
            InstallmentViewModel viewModel = JsonConvert.DeserializeObject<InstallmentViewModel>(bankOrder.PaymentInfo);
            var dic = form.ToDictionary(x => x.Key, x => x.Value);
            foreach (var (key, value) in dic)
            {
                if (key.Contains("NumberOfInstallments"))
                {
                    processPaymentRequest.CustomValues.Add(key, Convert.ToInt32(value));
                    viewModel.CurrentInstallmentCount = Convert.ToInt32(value);
                }

                if (key.Contains("Total"))
                {
                    processPaymentRequest.CustomValues.Add(key, Convert.ToDecimal(value, CultureInfo.InvariantCulture.NumberFormat));
                    viewModel.TotalAmount = Convert.ToDecimal(value, CultureInfo.InvariantCulture.NumberFormat);
                    bankOrder.TotalAmount = Convert.ToDecimal(value, CultureInfo.InvariantCulture.NumberFormat);

                }

                if (key.Contains("BankOrderId"))
                {
                    processPaymentRequest.CustomValues.Add(key, value);
                    viewModel.BankOrderId = value;
                }
                   
            }
            bankOrder.PaymentInfo = JsonConvert.SerializeObject(viewModel);
            await _bankOrderService.UpdateBankOrder(bankOrder);
            return await Task.FromResult(processPaymentRequest);
        }

        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = AllBankPaymentDefaults.PAYMENT_INFO_VIEW_COMPONENT_NAME;
        }

        public override async Task Install()
        {
            var binlist = await _bankBinService.GetBankBinList();
            if (!binlist.Any())
                await _installDataService.InstallData();
            var languages =await _languageService.GetAllLanguages();
            foreach (var language in languages)
            {
                if (language.Published)
                {
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.ID", "ID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.ID.hint", "ID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BinNumber", "Bin No", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BinNumber.hint", "Bin No", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardType", "Banka Kart Tipi  Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardType.hint", "Banka Kart Tipi  Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BankName", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BankName.hint", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BusinessCard", "Bussines Kart", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BusinessCard.hint", "Bussines Kart", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardAssociation", "Kart Tipi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardAssociation", "Kart Tipi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardAssociation.hint", "Visa Master", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardFamilyName", "Kart Ailesi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardFamilyName.hint", "bonus axess world vb", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BankCode", "Banka Kodu ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BankCode.hint", "Banka Kadu ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.Force3Ds", "3D Etkin ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.Force3Ds.hint", "3D Etkin mi? ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.Name", "Kategori Adı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.Name.hint", "Kategori Adı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.CategoryId", "KategoriID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.CategoryId.hint", "KategoriID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.MaxInstallment", "Taksit Sayısı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.MaxInstallment.hint", "En Yüksek Taksit Sayısı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.BankName", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.BankName.hint", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.Percentage", "Oran", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.Percentage.hint", "Faiz Oranı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.ID", "Pos ID ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.ID.hint", "Pos ID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankTypeId", "Pos Tipi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankTypeId.hint", "Pos Tipi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankTypeName", "Banka Tipi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankTypeName.hint", "Banka Tipi Seçiniz Banka mı Kanal mı?", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Name", "Banka Adı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Name.hint", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SystemName", "Banka Sistem Adı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SystemName.hint", "Banka Sistem Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Picture", "Banka Resim", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Picture.hint", "Banka Resim Seçerseniz Taksit Alanında Gözükecektir. ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.IsActive", "Aktif mi?", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.IsActive.hint", "Pos Aktif mi? Eğer bu bir kanal değilse hangi banka kartı girilirse o pos çalışır.", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.PrimaryBank", "Varsayılan Banka", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.PrimaryBank.hint", "Bir tane varsayılan banka seçili olmalı. Eğer Girilrn kart için tanımlı pos yoksa bu postan çekim yapılacaktır. Mutlaka tanımlanmalıdır.Kanal kullanılıyorsa sadece kanaldan çekilsin istiyorsanız kanalı varsayılan yapmalısınız. ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Primary", "Tek Çekim Pos", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Primary.hint", "Eğer Site Genelinde Taksit uygulaması yoksa veya kart taksit desteklemiyorsa bu bankadan çekim yapılır tanımlamak zorunlu değildir.Eğer Daha iyi bir oran uygulanıyorsa seçilebilir.", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankColor", "Banka Kart Rengi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankColor.hint", "Tanımlayacağınız Renk Kart Tasarımında kullanılabilir. Tanımları #222 gibi yapınız.", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankImageId", "Kart Resim", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankImageId.hint", "Burada Tanımlayacağını resim kart tasarımında kullanılabilir. ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankPosId", "Bank ID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankPosId.hint", "Banka ID ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankName", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankName.hint", "Taksit Yapılacak Banka Adı  ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.NumberOfInstallment", "Taksit Sayısı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.NumberOfInstallment.hint", "Uygulanacak Taksit sayısını Giriniz Fakat Gireceğiniz Taksit sayısının Banka tarafından desteklendiğinden emin olun iyzcio 2,3,6,9 destekliyor yanlış girerseniz ödemede hata oluşur. ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Percentage", "Oran", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Percentage.hint", "Faiz Oranı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SearchBankId", "Bankaya Göre Ara", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SearchBankId.hint", "Bankaya Göre Arama ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SearchBankTypeId", "Banka Tipine Göre Ara", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SearchBankTypeId.hint", "Banka Tipine Göre Ara", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ClientId", "Mağaza Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ClientId.hint", "ClientId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserName", "Kullanıcı Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserName.hint", "User Name", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.StoreKey", "Mağaza Şifresi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.StoreKey.hint", "StoreKey", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.StoreType", "Pos Tipi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.StoreType.hint", "3d_pay,3d,3d_Hosting Bizde 3d_pay Kullanıyoruz.", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.Password", "Şifre", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.Password.hint", "Password", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.GatewayUrl", "Gönderilecek Adres", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.GatewayUrl.hint", "Gateway Url", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.VerifyUrl", "İade Url", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.VerifyUrl.hint", "VerifyUrl İade veya diğer işlemler için kullanılacak", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.iframe", "Ortak Ödeme Sayfası", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.iframe.hint", "Ortak Ödeme sayfası kullanılacaksa seçilecek şu anda kullanılmıyor.", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ShopCode", "Mağaza Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ShopCode.hint", "ShopCode", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserCode", "Kullanıcı Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserCode.hint", "UserCode", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserPass", "Şifre", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserPass.hint", " UserPass", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MbrId", "Mağaza Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MbrId.hint", "mbrId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantId", "Terminal ID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantId.hint", "MerchantId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantPass", "Mağaza Şifre", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantPass.hint", "MerchantPass", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalId", "Terminal Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalId.hint", "TerminalId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalProvPassword", "Terminal Şifresi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalProvPassword.hint", "Secure Key", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalUserId", "Kullanıcı Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalUserId.hint", "PROVAUT/PROVRFN/PROVOOS", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalMerchantId", "Terminal Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalMerchantId.hint", "TerminalId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalProvUserId", "Kullancı Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalProvUserId.hint", "PROVAUT/PROVRFN/PROVOOS", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.CompanyName", "Firma Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.CompanyName.hint", "Firma Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.Email", "Email", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.Email.hint", "Firma maili", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.CustomerNumber", "Kullanıcı Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.CustomerNumber.hint", "Customer Number", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.EnrollmentUrl", "Url", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.EnrollmentUrl.hint", "EnrollmentUrl", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalNo", "Terminal No", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalNo.hint", "Terminal No", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PosNetId", "Kullanıcı Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PosNetId.hint", "PosNetId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PosNetId", "Kullanıcı Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PosNetId.hint", "PosNetId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ApiKey", "Api Anahtarı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ApiKey.hint", "ApiKey", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.SecretKey", "Şifre", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.SecretKey.hint", "SecretKey", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ApiUrl", "Url", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ApiUrl.hint", "Api Url", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PublicKey", "Api Anahtarı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PublicKey.hint", "Api Anahtarı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PrivateKey", "Şifre", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PrivateKey.hint", "Şifre", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.BaseUrl", "Url", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.BaseUrl.hint", "Base Url", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ThreeDInquiryUrl", "3D Url", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ThreeDInquiryUrl.hint", "3D Url", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantKey", "Api Anahtarı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantKey.hint", "Api Anahtar", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantSalt", "Şifre", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantSalt.hint", "Merchant Salt", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.DescriptionText", "Açıklama", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.DescriptionText.hint", "Açıklama", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.IsInstallment", "Taksit Var mı?", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.IsInstallment.hint", "Site Genelinde Taksit Uygulanacaksa", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.AdditionalFee", "Ek Ücret", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.AdditionalFee.hint", "Ek Ücert uygulanacak mı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.AdditionalFeePercentage", "Ek Ücret Yüzde", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.AdditionalFeePercentage.hint", "Ek Ücert Yüzde uygulanacak mı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.InstallmentCategoryBased", "Kategori Bazlı Taksit", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.InstallmentCategoryBased.hint", "Kategori Bazlı Taksit", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TestMode", "Test Aktif", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TestMode.hint", "Test Modu Aktif mi", language.LanguageCulture);                   
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin", "Bank Bin Listesi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.No", "Bin Numarası", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.BankName", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.CardType", "Kart Tipi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.CardAssociation", "Marka", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.CardFamilyName", "Kart Ailesi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.BankCode", "Banka Kodu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.BusinessCard", "Business Kart", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.Force3Ds", "3D Zorunlu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.EditBankBinDetails", "Kart Detayı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.AddNew", "Yeni Ekle", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.BackToList", "Listeye Dön", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.EditBankPosDetails", "Düzenlenen Kayıt", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPosAdd.BackToList", "Listeye Dön", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.AddNew", "Ekle", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.CategoryInstallment", "Kategori Listesi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Field.Name", "Kategori Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Field.NumberOfInstallment", "Taksit Sayısı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Field.NumberOfInstallment.hint", "Taksit Sayısı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Field.Percentage", "Oran", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.AddButton", "Taksit Ekle", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos", "Pos Ayarları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.BankPosParameter", "Kullanıcı Ayarları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.BankPosInstallment", "Taksit Ayarları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Admin.Payment.BankPos.AddNew", "Pos Ekle/Düzenle", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Admin.Payment.BankBin.AddNew", "Bin Numarası Ekle/Düzenle", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos", "Pos Listesi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.Name", "Pos adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.BankName", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.BankBrand", "Marka", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.IsActive", "Aktif mi?", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.PrimaryBank", "Varsayılan Banka", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.Primary", "Primary", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Payment.CardholderName", "Kart Üstündeki isim", language.LanguageCulture);                    
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Payment.CardNumber", "KartNumarası", language.LanguageCulture);                    
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Payment.ExpirationDate", "Son Kullanma Tarihi", language.LanguageCulture);                  
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Payment.CardCode", "CVC", language.LanguageCulture);                   
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Payment.Total", "Toplam", language.LanguageCulture);                  
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Payment.NumberOfInstallment", "Taksit Sayısı", language.LanguageCulture);                   
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Payment.MonthTotal", "Aylık Tutar", language.LanguageCulture);                   
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Payment.Installment.Description", "Taksit seçenekleri kart bilgisi girildikten sonra görüntülenir.", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Deleted", "Pos Silindi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Error", "Hata Oluştu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Error", "Hata Oluştu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Updated", "Güncellendi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Deleted", "Güncellendi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "PaymentAllBank.settings", "Pos Ayarları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "PaymentAllBank.BankBin", "Banka Bin Listesi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "PaymentAllBank.BankCategory", "Kategoriler", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "PaymentAllBank.BankPos", "Poslar", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "PaymentAllBank.BankOrder", "Order Tablosu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "plugins.payments.AllBank.PaymentMethodDescription", "Kredi Kartı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Admin.AllBank.PaymentBankOrder.List.Customer", "Müşteriler", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankOrder", "Order Listesi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankOrder.Field.CustomerEmail", "Mail", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "plugins.allbank.admin.bankposinstallment", "Taksitler", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "payment.allbank", "Bank Ayarları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "plugins.allbank.admin.bank.editbankdetails", "Bank Detayları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "plugins.allbank.admin.bankorder.editbankorderdetails", "Bank Order Detayları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "plugins.allbank.admin.bankorder.backtolist", "Listeye Geri Dön", language.LanguageCulture);




                }
            }
            await base.Install();
        }

        public override async Task Uninstall()
        {
            await _settingService.DeleteSetting<AllBankPaymentSettings>();
            await _installDataService.UninstallData();
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.ID");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.ID.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BinNumber");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BinNumber.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardType");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardType.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BankName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BankName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BusinessCard");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BusinessCard.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardAssociation");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardAssociation");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardAssociation.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardFamilyName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.CardFamilyName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BankCode");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.BankCode.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.Force3Ds");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankBin.Field.Force3Ds.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.Name");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.Name.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.CategoryId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.CategoryId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.MaxInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.MaxInstallment.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.BankName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.BankName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.Percentage");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankInstallment.Field.Percentage.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.ID");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.ID.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankTypeId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankTypeId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankTypeName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankTypeName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Name");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Name.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SystemName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SystemName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Picture");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Picture.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.IsActive");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.IsActive.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.PrimaryBank");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.PrimaryBank.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Primary");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Primary.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankColor");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankColor.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankImageId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankImageId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankPosId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankPosId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.BankName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.NumberOfInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.NumberOfInstallment.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Percentage");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.Percentage.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SearchBankId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SearchBankId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SearchBankTypeId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.BankPos.Field.SearchBankTypeId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ClientId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ClientId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.StoreKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.StoreKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.StoreType");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.StoreType.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.Password");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.Password.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.GatewayUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.GatewayUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.VerifyUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.VerifyUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.iframe");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.iframe.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ShopCode");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ShopCode.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserCode");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserCode.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserPass");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.UserPass.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MbrId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MbrId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantPass");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantPass.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalProvPassword");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalProvPassword.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalUserId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalUserId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalMerchantId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalMerchantId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalProvUserId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalProvUserId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.CompanyName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.CompanyName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.Email");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.Email.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.CustomerNumber");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.CustomerNumber.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.EnrollmentUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.EnrollmentUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalNo");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TerminalNo.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PosNetId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PosNetId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PosNetId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PosNetId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ApiKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ApiKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.SecretKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.SecretKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ApiUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ApiUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PublicKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PublicKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PrivateKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.PrivateKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.BaseUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.BaseUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ThreeDInquiryUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.ThreeDInquiryUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantSalt");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.MerchantSalt.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.DescriptionText");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.DescriptionText.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.IsInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.IsInstallment.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.AdditionalFee");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.AdditionalFee.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.AdditionalFeePercentage");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.AdditionalFeePercentage.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.InstallmentCategoryBased");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.InstallmentCategoryBased.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TestMode");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.AllBank.Configuration.TestMode.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.No");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.BankName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.CardType");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.CardAssociation");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.CardFamilyName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.BankCode");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.BusinessCard");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Field.Force3Ds");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.EditBankBinDetails");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.AddNew");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.BackToList");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.EditBankPosDetails");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPosAdd.BackToList");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.AddNew");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.CategoryInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Field.Name");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Field.NumberOfInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Field.Percentage");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.AddButton");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.BankPosParameter");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.BankPosInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Admin.Payment.BankPos.AddNew");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Admin.Payment.BankBin.AddNew");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.Name");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.BankName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.BankBrand");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.IsActive");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.PrimaryBank");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Fields.Primary");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Payment.CardholderName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Payment.CardNumber");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Payment.ExpirationDate");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Payment.CardCode");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Payment.Total");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Payment.NumberOfInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Payment.MonthTotal");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Payment.Installment.Description");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Deleted");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankPos.Error");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Error");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Updated");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.AllBank.Admin.BankBin.Deleted");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "paymentallbank.settings");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "paymentallbank.bankbin");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "paymentallbank.bankcategory");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "paymentallbank.bankpos");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "plugins.payments.allbank.paymentmethoddescription");


            await base.Uninstall();
        }

        public async Task ManageSiteMap(SiteMapNode rootNode)
        {
            var pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "extensions");
            if (pluginNode == null)
            {
                rootNode.ChildNodes.Add(new SiteMapNode() {

                    SystemName = AllBankPaymentDefaults.SystemName,
                    Visible = true,
                    IconClass = "icon-credit-card",
                    ResourceName = "Payment.AllBank"
                });
                pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "extensions");
            }
            var menu = new SiteMapNode {
                Visible = true,
                SystemName = AllBankPaymentDefaults.MenuSystemName,
                ResourceName = "PaymentAllBank.Settings",
                ChildNodes = new List<SiteMapNode> {
                    new SiteMapNode {
                        Visible = true,
                        SystemName = "Admin.BankBin",
                        ResourceName = "PaymentAllBank.BankBin",
                        ControllerName = "PaymentBankBin",
                        ActionName = "List"
                    },
                    new SiteMapNode {
                        Visible = true,
                        SystemName = "Admin.BankCategory",
                        ResourceName = "PaymentAllBank.BankCategory",
                        ControllerName = "PaymentCategoryInstallment",
                        ActionName = "List"
                    },
                    new SiteMapNode {
                        Visible = true,
                        SystemName = "Admin.BankPos",
                        ResourceName = "PaymentAllBank.BankPos",
                        ControllerName = "PaymentBankPos",
                        ActionName = "List"
                    },
                    new SiteMapNode {
                        Visible = true,
                        SystemName = "Admin.BankOrder",
                        ResourceName = "PaymentAllBank.BankOrder",
                        ControllerName = "PaymentBankOrder",
                        ActionName = "List"
                    }

                }
            };
            if (pluginNode != null)
                pluginNode.ChildNodes.Add(menu);
            else
            {
                rootNode.ChildNodes.Add(menu);
            }

            await Task.CompletedTask;
        }

        #endregion

        #region Properties

        public async Task<bool> SupportCapture()
        {
            return await Task.FromResult(false);
        }

        public async Task<bool> SupportPartiallyRefund()
        {
            return await Task.FromResult(false);
        }

        public async Task<bool> SupportRefund()
        {
            return await Task.FromResult(false);
        }

        public async Task<bool> SupportVoid()
        {
            return await Task.FromResult(false);
        }

        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        public PaymentMethodType PaymentMethodType => _orderSettings.OnePageCheckoutEnabled ? PaymentMethodType.Redirection : PaymentMethodType.Standard;

        public async Task<bool> SkipPaymentInfo()
        {
            return await Task.FromResult(false);
        }

        public async Task<string> PaymentMethodDescription()
        {
            return await Task.FromResult(
                _localizationService.GetResource("Plugins.Payments.AllBank.PaymentMethodDescription"));
        }

        #endregion

        public IList<string> GetWidgetZones()
        {
            return new string[] { "opc_content_before", "checkout_payment_info_top", "op_checkout_payment_info_top" };
        }

        public void GetPublicViewComponent(string widgetZone, out string viewComponentName)
        {
            viewComponentName = AllBankPaymentDefaults.PAYMENT_SCRIPT_VIEW_COMPONENT_NAME;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Grand.Core;
using Grand.Core.Plugins;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Framework.Extensions;
using Grand.Framework.Menu;
using Grand.Plugin.Payments.GarantiPay.Domain;
using Grand.Plugin.Payments.GarantiPay.Models;
using Grand.Plugin.Payments.GarantiPay.Models.Banks;
using Grand.Plugin.Payments.GarantiPay.Services;
using Grand.Services.Catalog;
using Grand.Services.Configuration;
using Grand.Services.Localization;
using Grand.Services.Orders;
using Grand.Services.Payments;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Grand.Plugin.Payments.GarantiPay
{
    public class PaymentGarantiPayProcessor : BasePlugin, IPaymentMethod, IAdminMenuPlugin
    {
        private readonly ILocalizationService _localizationService;
        private readonly OrderSettings _orderSettings;
        private readonly IWebHelper _webHelper;
        private readonly PaymentGarantiPaySettings _paymentGarantiPaySettings;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ILanguageService _languageService;
        private readonly IGarantiPayOrderServices _garantiPayOrderServices;
        private readonly ISettingService _settingService;
        private readonly IWorkContext _workContext;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStoreContext _storeContext;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;


        public PaymentGarantiPayProcessor(ILocalizationService localizationService,
            OrderSettings orderSettings,
            IWebHelper webHelper,
            PaymentGarantiPaySettings paymentGarantiPaySettings,
            IOrderTotalCalculationService orderTotalCalculationService,
            ILanguageService languageService,
            IGarantiPayOrderServices garantiPayOrderServices,
            ISettingService settingService,
            IWorkContext workContext,
            IHttpContextAccessor httpContextAccessor,
            IStoreContext storeContext, IProductService productService, ICategoryService categoryService)
        {
            _localizationService = localizationService;
            _orderSettings = orderSettings;
            _webHelper = webHelper;
            _paymentGarantiPaySettings = paymentGarantiPaySettings;
            _orderTotalCalculationService = orderTotalCalculationService;
            _languageService = languageService;
            _garantiPayOrderServices = garantiPayOrderServices;
            _settingService = settingService;
            _workContext = workContext;
            _httpContextAccessor = httpContextAccessor;
            _storeContext = storeContext;
            _productService = productService;
            _categoryService = categoryService;
        }

        #region Utilities

        private static readonly Dictionary<string, string> CurrencyCodes = new Dictionary<string, string>
        {
            { "TRY","949" },
            { "USD","840" },
            { "EUR","978" },
            { "GBP","826" }
        };
        private static string GetPublicIp()
        {
            var webClient = new WebClient();
            string dnsString = webClient.DownloadString("http://checkip.dyndns.org");
            dnsString = (new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b")).Match(dnsString).Value;
            webClient.Dispose();
            return dnsString;
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

                Installment = installment.NumberOfInstallment,
                Rate = installment.Percentage,
                AmountValue = installmentAmount,
                TotalAmountValue = installmentTotalAmount
            });
        }
        #endregion
        #region Method
        public async Task<ProcessPaymentResult> ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            return await Task.FromResult(result);


        }

        public async Task PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {

            var paymentGarantiPaySettings = _settingService.LoadSetting<PaymentGarantiPaySettings>();
            var installmentViewModel =  new InstallmentViewModel();
            var garantiPayOrder = new OmniGarantiPayOrder();
            var order = postProcessPaymentRequest.Order;
            installmentViewModel.MarkGuid();
            installmentViewModel.TotalAmount = order.OrderTotal;
            installmentViewModel.AddOrderNumber();
            var posList = await _garantiPayOrderServices.GetGarantiPayPosList();
            var pos = posList.FirstOrDefault();
            List<BankInstallment> bankInstallments = pos.BankInstallments.ToList();
            var bankParameters = pos.GenericAttributes.ToDictionary(key => key.Key, value => value.Value);
            string terminalId = bankParameters[nameof(GarantiBankModel.TerminalId)];
            string terminalUserId = bankParameters[nameof(GarantiBankModel.TerminalUserId)];
            string terminalMerchantId = bankParameters[nameof(GarantiBankModel.TerminalMerchantId)];
            string terminalProvUserId = bankParameters[nameof(GarantiBankModel.TerminalProvUserId)];
            string terminalProvPassword = bankParameters[nameof(GarantiBankModel.TerminalProvPassword)];
            string storeKey = bankParameters[nameof(GarantiBankModel.StoreKey)];
            string mode = paymentGarantiPaySettings.TestMode ? "TEST" : "PROD";
            string email = bankParameters[nameof(GarantiBankModel.Email)];
            string companyName = bankParameters[nameof(GarantiBankModel.CompanyName)];
            string type = "gpdatarequest";
            string secure3dSecurityLevel = "CUSTOM_PAY";
            string amount = (installmentViewModel.TotalAmount * 100M).ToString("0.##", new CultureInfo("en-EN"));
            string paymentUrl = bankParameters[nameof(GarantiBankModel.GatewayUrl)];
            string successUrl = $"{_webHelper.GetStoreLocation(new bool?())}PaymentGarantiPay/Success";
            string cancelUrl = $"{_webHelper.GetStoreLocation(new bool?())}PaymentGarantiPay/Cancel";
            string lang = _workContext.WorkingLanguage.UniqueSeoCode.ToLower();
            string _terminalid = string.Format("{0:000000000}", int.Parse(terminalId));
            var parameters = new Dictionary<string, object>();

            List<Category> listCategory = new List<Category>();
            var categoryInstallmentList = await _garantiPayOrderServices.GetGarantiPayInstallmentCategoryList();
            if (paymentGarantiPaySettings.IsInstallment && installmentViewModel.InstallmentRates.Count == 0)
            {
                if (categoryInstallmentList.Any())
                {
                    foreach (var item in order.OrderItems)
                    {
                        var product = await _productService.GetProductById(item.ProductId);
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
                            installmentViewModel.NumberOfInstallments = bankInstallmentCategory.MaxInstallment;
                        }
                    }

                    if (bankInstallments.Count > 0)
                    {
                        foreach (var installment in bankInstallments.TakeWhile(installment => installment.NumberOfInstallment != installmentViewModel.NumberOfInstallments))
                        {
                            AddBankinstallment(installmentViewModel, installment);
                        }
                    }
                }
                else
                {
                    if (bankInstallments.Count > 0)
                    {
                        foreach (var installment in bankInstallments)
                        {
                            AddBankinstallment(installmentViewModel, installment);
                        }
                    }
                    installmentViewModel.NumberOfInstallments = bankInstallments.LastOrDefault().NumberOfInstallment;
                }
            }

            installmentViewModel.TestMode = paymentGarantiPaySettings.TestMode ? "TEST" : "PROD";
            Int32 unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var numberOfInstallment = installmentViewModel.NumberOfInstallments < 2 ? 0 : installmentViewModel.NumberOfInstallments;
            parameters.Add("secure3dsecuritylevel", secure3dSecurityLevel);
            parameters.Add("mode", mode);
            parameters.Add("apiversion", "v0.01");
            parameters.Add("terminalprovuserid", terminalProvUserId);
            parameters.Add("terminaluserid", terminalUserId);
            parameters.Add("terminalmerchantid", terminalMerchantId);
            parameters.Add("terminalid", terminalId);
            parameters.Add("txntype", type);
            parameters.Add("txnamount", amount);
            parameters.Add("txncurrencycode", CurrencyCodes[_workContext.WorkingCurrency.CurrencyCode]);
            parameters.Add("txninstallmentcount", numberOfInstallment);
            parameters.Add("customeremailaddress", email);
            parameters.Add("customeripaddress", GetPublicIp());
            parameters.Add("orderid", installmentViewModel.OrderNumber);
            parameters.Add("successurl", successUrl);
            parameters.Add("errorurl", cancelUrl);
            parameters.Add("companyname", companyName);
            parameters.Add("lang", lang);
            var hashBuilder = new StringBuilder();
            hashBuilder.Append(terminalId);
            hashBuilder.Append(installmentViewModel.OrderNumber);
            hashBuilder.Append(amount);
            hashBuilder.Append(successUrl);
            hashBuilder.Append(cancelUrl);
            hashBuilder.Append(type);
            hashBuilder.Append(numberOfInstallment);
            hashBuilder.Append(storeKey);
            var securityData = GetSHA1($"{terminalProvPassword}{_terminalid}");
            hashBuilder.Append(securityData);
            var hashData = GetSHA1(hashBuilder.ToString());
            parameters.Add("secure3dhash", hashData);
            parameters.Add("txntimestamp", unixTimestamp);
            parameters.Add("refreshtime", 10);
            parameters.Add("bnsuseflag", "N");
            parameters.Add("fbbuseflag", "N");
            parameters.Add("txnsubtype", "sales");
            parameters.Add("garantipay", "Y");
            string formId = "PaymentForm";
            StringBuilder formBuilder = new StringBuilder();
            formBuilder.Append($"<form id=\"{formId}\" name=\"{formId}\" action=\"{paymentUrl}\" role=\"form\" method=\"POST\">");
            foreach (KeyValuePair<string, object> parameter in parameters)
            {
                formBuilder.Append($"<input type=\"hidden\" name=\"{parameter.Key}\" value=\"{parameter.Value}\">");
            }
            if (numberOfInstallment > 1)
            {
                foreach (var (item, index) in installmentViewModel.InstallmentRates.Select((item, index) => (item, index)))
                {
                    formBuilder.Append("<input type=\"hidden\" name=\"" + $"installmentnumber{item.Installment}" + "\" value=\"" + item.Installment + "\" />");
                    formBuilder.Append("<input type=\"hidden\" name=\"" + $"installmentamount{item.Installment}" + "\" value=\"" + (item.TotalAmountValue * 100M).ToString("0.##", new CultureInfo("en-EN")) + "\" />");
                }
            }
            formBuilder.Append("<input type=\"hidden\" name=\"chequeuseflag\"  id=\"chequeuseflag\" Value=\"N\" />");
            formBuilder.Append("<font face=\"Helvetica\" size=\"3\" color=\"#606060\"><center><br /><br />");
            formBuilder.Append("<h2>Banka sayfasına yonlendiriliyorsunuz...</h2>  <br /><br /></center>");
            formBuilder.Append("</form>");
            StringBuilder scriptBuilder = new StringBuilder();
            scriptBuilder.Append("<script>");
            scriptBuilder.Append($"document.{formId}.submit();");
            scriptBuilder.Append("</script>");
            formBuilder.Append(scriptBuilder.ToString());
            var htmlForm = formBuilder.ToString();
            garantiPayOrder.PaymentInfoSession = installmentViewModel.PaymentInfoSession;
            garantiPayOrder.TotalAmount = installmentViewModel.TotalAmount;
            garantiPayOrder.OrderNumber = installmentViewModel.OrderNumber;
            garantiPayOrder.OrderGuid = order.OrderGuid;
            garantiPayOrder.BankRequest = JsonConvert.SerializeObject(htmlForm);
            garantiPayOrder.PaymentInfo = JsonConvert.SerializeObject(installmentViewModel);
            garantiPayOrder.CreateDate = DateTime.Now;
            garantiPayOrder.CustomerId = _workContext.CurrentCustomer.Id;
            garantiPayOrder.Hash = hashData;
            await _garantiPayOrderServices.InsertOmniGarantiPayOrder(garantiPayOrder);
            installmentViewModel.BankOrderId = garantiPayOrder.Id;
            var httpContext = _httpContextAccessor.HttpContext;
            var response = httpContext.Response;
            response.Clear();
            Encoding encoding = new CustomEncoding();
            var data = encoding.GetBytes(htmlForm);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength = data.Length;
            response.Body.WriteAsync(data, 0, data.Length).Wait();
            _webHelper.IsPostBeingDone = true;

        }

        public async Task<bool> HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return await Task.FromResult(false);
        }

        public async Task<decimal> GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            if (_paymentGarantiPaySettings.AdditionalFee <= 0)
                return _paymentGarantiPaySettings.AdditionalFee;

            decimal result;
            if (_paymentGarantiPaySettings.AdditionalFeePercentage)
            {
                //percentage
                var subtotal = await _orderTotalCalculationService.GetShoppingCartSubTotal(cart, true);
                result = (decimal)((((float)subtotal.subTotalWithDiscount) *
                                    ((float)_paymentGarantiPaySettings.AdditionalFee)) / 100f);
            }
            else
            {
                //fixed value
                result = _paymentGarantiPaySettings.AdditionalFee;
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

        public async Task<RefundPaymentResult> Refund(RefundPaymentRequest refundPaymentRequest)
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

        public async Task<CancelRecurringPaymentResult> CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return await Task.FromResult(result);
        }

        public async Task<bool> CanRePostProcessPayment(Order order)
        {
            return await Task.FromResult(false);
        }

        public async Task<IList<string>> ValidatePaymentForm(IFormCollection form)
        {
            return await Task.FromResult(new List<string>());
        }

        public async Task<ProcessPaymentRequest> GetPaymentInfo(IFormCollection form)
        {
            return await Task.FromResult(new ProcessPaymentRequest());
        }
        #endregion
        public override async Task Install()
        {

            await _garantiPayOrderServices.InstallData();

            var languages = await _languageService.GetAllLanguages();
            foreach (var language in languages)
            {
                if (language.Published)
                {

                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.Name", "Kategori Adı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.Name.hint", "Kategori Adı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.CategoryId", "KategoriID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.CategoryId.hint", "KategoriID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.MaxInstallment", "Taksit Sayısı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.MaxInstallment.hint", "En Yüksek Taksit Sayısı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.BankName", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.BankName.hint", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.Percentage", "Oran", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.Percentage.hint", "Faiz Oranı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.ID", "Pos ID ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.ID.hint", "Pos ID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankTypeId", "Pos Tipi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankTypeId.hint", "Pos Tipi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankTypeName", "Banka Tipi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankTypeName.hint", "Banka Tipi Seçiniz Banka mı Kanal mı?", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Name", "Banka Adı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Name.hint", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SystemName", "Banka Sistem Adı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SystemName.hint", "Banka Sistem Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Picture", "Banka Resim", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Picture.hint", "Banka Resim Seçerseniz Taksit Alanında Gözükecektir. ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.IsActive", "Aktif mi?", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.IsActive.hint", "Pos Aktif mi? Eğer bu bir kanal değilse hangi banka kartı girilirse o pos çalışır.", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.PrimaryBank", "Varsayılan Banka", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.PrimaryBank.hint", "Bir tane varsayılan banka seçili olmalı. Eğer Girilrn kart için tanımlı pos yoksa bu postan çekim yapılacaktır. Mutlaka tanımlanmalıdır.Kanal kullanılıyorsa sadece kanaldan çekilsin istiyorsanız kanalı varsayılan yapmalısınız. ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Primary", "Tek Çekim Pos", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Primary.hint", "Eğer Site Genelinde Taksit uygulaması yoksa veya kart taksit desteklemiyorsa bu bankadan çekim yapılır tanımlamak zorunlu değildir.Eğer Daha iyi bir oran uygulanıyorsa seçilebilir.", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankColor", "Banka Kart Rengi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankColor.hint", "Tanımlayacağınız Renk Kart Tasarımında kullanılabilir. Tanımları #222 gibi yapınız.", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankImageId", "Kart Resim", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankImageId.hint", "Burada Tanımlayacağını resim kart tasarımında kullanılabilir. ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankPosId", "Bank ID", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankPosId.hint", "Banka ID ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankName", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankName.hint", "Taksit Yapılacak Banka Adı  ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.NumberOfInstallment", "Taksit Sayısı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.NumberOfInstallment.hint", "Uygulanacak Taksit sayısını Giriniz Fakat Gireceğiniz Taksit sayısının Banka tarafından desteklendiğinden emin olun iyzcio 2,3,6,9 destekliyor yanlış girerseniz ödemede hata oluşur. ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Percentage", "Oran", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Percentage.hint", "Faiz Oranı ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SearchBankId", "Bankaya Göre Ara", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SearchBankId.hint", "Bankaya Göre Arama ", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SearchBankTypeId", "Banka Tipine Göre Ara", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SearchBankTypeId.hint", "Banka Tipine Göre Ara", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.DescriptionText", "Açıklama", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.DescriptionText.hint", "Açıklama", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.IsInstallment", "Taksit Var mı?", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.IsInstallment.hint", "Site Genelinde Taksit Uygulanacaksa", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.AdditionalFee", "Ek Ücret", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.AdditionalFee.hint", "Ek Ücert uygulanacak mı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.AdditionalFeePercentage", "Ek Ücret Yüzde", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.AdditionalFeePercentage.hint", "Ek Ücert Yüzde uygulanacak mı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TestMode", "Test Aktif", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TestMode.hint", "Test Modu Aktif mi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalId", "TerminalId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalId.hint", "TerminalId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalMerchantId", "MerchantId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalMerchantId.hint", "MerchantId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalProvUserId", "TerminalProvUserId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalProvUserId.hint", "TerminalProvUserId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalProvPassword", "TerminalProvPassword", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalProvPassword.hint", "TerminalProvPassword", language.LanguageCulture);

                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalProvUserId", "TerminalProvUserId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalProvUserId.hint", "TerminalProvUserId", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.Email", "Email", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.Email.hint", "Email", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.CompanyName", "CompanyName", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.CompanyName.hint", "CompanyName", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.StoreKey", "StoreKey", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.StoreKey.hint", "StoreKey", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.GatewayUrl", "GatewayUrl", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.GatewayUrl.hint", "GatewayUrl", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.EditBankPosDetails", "Düzenlenen Kayıt", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPosAdd.BackToList", "Listeye Dön", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.AddNew", "Ekle", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.CategoryInstallment", "Kategori Listesi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Field.Name", "Kategori Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Field.NumberOfInstallment", "Taksit Sayısı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Field.NumberOfInstallment.hint", "Taksit Sayısı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Field.Percentage", "Oran", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.AddButton", "Taksit Ekle", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos", "Pos Ayarları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.BankPosParameter", "Kullanıcı Ayarları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.BankPosInstallment", "Taksit Ayarları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Admin.Payment.BankPos.AddNew", "Pos Ekle/Düzenle", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos", "Pos Listesi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.Name", "Pos adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.BankName", "Banka Adı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.BankBrand", "Marka", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.IsActive", "Aktif mi?", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.PrimaryBank", "Varsayılan Banka", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.Primary", "Primary", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Deleted", "Pos Silindi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Error", "Hata Oluştu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "PaymentGarantiPay.settings", "Pos Ayarları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "PaymentGarantiPay.BankCategory", "Kategoriler", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "PaymentGarantiPay.BankPos", "Poslar", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "PaymentGarantiPay.BankOrder", "Order Tablosu", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "plugins.payments.GarantiPay.PaymentMethodDescription", "Kredi Kartı", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Admin.GarantiPay.PaymentBankOrder.List.Customer", "Müşteriler", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankOrder", "Order Listesi", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankOrder.Field.CustomerEmail", "Mail", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "plugins.GarantiPay.admin.bankposinstallment", "Taksitler", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "plugins.GarantiPay.admin.bank.editbankdetails", "Bank Detayları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "plugins.GarantiPay.admin.bankorder.editbankorderdetails", "Bank Order Detayları", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "plugins.GarantiPay.admin.bankorder.backtolist", "Listeye Geri Dön", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "payment.garantipay", "Garanti Pay", language.LanguageCulture);
                    await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "paymentgarantipay.garantipayorder", "GarantiPay Order", language.LanguageCulture);




                }
            }
            await base.Install();
        }

        public override async Task Uninstall()
        {
            await _settingService.DeleteSetting<PaymentGarantiPaySettings>();
            await _garantiPayOrderServices.UninstallData();
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.Name");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.Name.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.CategoryId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.CategoryId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.MaxInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.MaxInstallment.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.BankName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.BankName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.Percentage");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankInstallment.Field.Percentage.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.ID");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.ID.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankTypeId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankTypeId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankTypeName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankTypeName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Name");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Name.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SystemName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SystemName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Picture");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Picture.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.IsActive");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.IsActive.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.PrimaryBank");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.PrimaryBank.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Primary");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Primary.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankColor");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankColor.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankImageId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankImageId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankPosId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankPosId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.BankName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.NumberOfInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.NumberOfInstallment.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Percentage");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.Percentage.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SearchBankId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SearchBankId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SearchBankTypeId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.BankPos.Field.SearchBankTypeId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.ClientId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.ClientId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.UserName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.UserName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.StoreKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.StoreKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.StoreType");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.StoreType.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.Password");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.Password.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.GatewayUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.GatewayUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.VerifyUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.VerifyUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.iframe");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.iframe.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.ShopCode");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.ShopCode.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.UserCode");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.UserCode.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.UserPass");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.UserPass.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.MbrId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.MbrId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.MerchantId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.MerchantId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.MerchantPass");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.MerchantPass.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalProvPassword");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalProvPassword.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalUserId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalUserId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalMerchantId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalMerchantId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalProvUserId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalProvUserId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.CompanyName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.CompanyName.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.Email");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.Email.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.CustomerNumber");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.CustomerNumber.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.EnrollmentUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.EnrollmentUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalNo");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TerminalNo.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.PosNetId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.PosNetId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.PosNetId");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.PosNetId.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.ApiKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.ApiKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.SecretKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.SecretKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.ApiUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.ApiUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.PublicKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.PublicKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.PrivateKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.PrivateKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.BaseUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.BaseUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.ThreeDInquiryUrl");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.ThreeDInquiryUrl.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.MerchantKey");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.MerchantKey.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.MerchantSalt");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.MerchantSalt.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.DescriptionText");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.DescriptionText.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.IsInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.IsInstallment.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.AdditionalFee");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.AdditionalFee.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.AdditionalFeePercentage");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.AdditionalFeePercentage.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.InstallmentCategoryBased");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.InstallmentCategoryBased.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TestMode");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.Payment.GarantiPay.Configuration.TestMode.hint");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.Field.No");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.Field.BankName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.Field.CardType");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.Field.CardAssociation");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.Field.CardFamilyName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.Field.BankCode");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.Field.BusinessCard");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.Field.Force3Ds");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.EditBankBinDetails");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.AddNew");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankBin.BackToList");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.EditBankPosDetails");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPosAdd.BackToList");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.AddNew");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.CategoryInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Field.Name");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Field.NumberOfInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Field.Percentage");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.AddButton");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.BankPosParameter");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.BankPosInstallment");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Admin.Payment.BankPos.AddNew");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.Name");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.BankName");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.BankBrand");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.IsActive");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.PrimaryBank");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Fields.Primary");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Deleted");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "Plugins.GarantiPay.Admin.BankPos.Error");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "paymentGarantiPay.settings");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "paymentGarantiPay.bankbin");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "paymentGarantiPay.bankcategory");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "paymentGarantiPay.bankpos");
            await this.DeletePluginLocaleResource(_localizationService, _languageService, "plugins.payments.GarantiPay.paymentmethoddescription");


            await base.Uninstall();
        }

        public async Task ManageSiteMap(SiteMapNode rootNode)
        {
            var pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "extensions");
            if (pluginNode == null)
            {
                rootNode.ChildNodes.Add(new SiteMapNode()
                {

                    SystemName = PaymentGarantiPayDefaults.SystemName,
                    Visible = true,
                    IconClass = "icon-credit-card",
                    ResourceName = "Payment.GarantiPay"
                });
                pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "extensions");
            }
            var menu = new SiteMapNode
            {
                Visible = true,
                SystemName = PaymentGarantiPayDefaults.MenuSystemName,
                ResourceName = "PaymentGarantiPay.Settings",
                ChildNodes = new List<SiteMapNode> {

                    new SiteMapNode {
                        Visible = true,
                        SystemName = "Admin.GarantiPayCategory",
                        ResourceName = "PaymentGarantiPay.BankCategory",
                        ControllerName = "PaymentGarantiPayCategoryInstallment",
                        ActionName = "List"
                    },
                    new SiteMapNode {
                        Visible = true,
                        SystemName = "Admin.GarantiPayPos",
                        ResourceName = "PaymentGarantiPay.BankPos",
                        ControllerName = "PaymentGarantiPayPos",
                        ActionName = "List"
                    },
                    new SiteMapNode {
                        Visible = true,
                        SystemName = "Admin.GarantiPayOrder",
                        ResourceName = "PaymentGarantiPay.GarantiPayOrder",
                        ControllerName = "PaymentGarantiPayOrder",
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

        #region Config
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/PaymentGarantiPay/Configure";
        }
        public void GetPublicViewComponent(out string viewComponentName)
        {
            viewComponentName = PaymentGarantiPayDefaults.GARANTIPAY_VIEW_COMPONENT_NAME;
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
            return await Task.FromResult(_localizationService.GetResource("Plugins.Payments.GarantiPay.PaymentMethodDescription"));
        }

        #endregion

    }
}
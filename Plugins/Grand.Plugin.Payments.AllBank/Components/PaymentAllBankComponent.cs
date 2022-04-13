using Grand.Core;
using Grand.Domain.Orders;
using Grand.Framework.Components;
using Grand.Framework.Extensions;
using Grand.Plugin.Payments.AllBank.Models;
using Grand.Plugin.Payments.AllBank.Validators;
using Grand.Services.Localization;
using Grand.Services.Orders;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;


namespace Grand.Plugin.Payments.AllBank.Components
{
    [ViewComponent(Name = AllBankPaymentDefaults.PAYMENT_INFO_VIEW_COMPONENT_NAME)]
    public class PaymentAllBankComponent : BaseViewComponent
    {
        private readonly AllBankPaymentSettings _allBankPaymentSettings;
        private readonly ILocalizationService _localizationService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly OrderSettings _orderSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentAllBankComponent(
            AllBankPaymentSettings allBankPaymentSettings,
            ILocalizationService localizationService,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            OrderSettings orderSettings,
             IHttpContextAccessor httpContextAccessor)
        {
            _allBankPaymentSettings = allBankPaymentSettings;
            _localizationService = localizationService;
            _shoppingCartService = shoppingCartService;
            _storeContext = storeContext;
            _orderSettings = orderSettings;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cart = _shoppingCartService.GetShoppingCart(_storeContext.CurrentStore.Id,
                ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);
            if (!cart.Any())
                throw new Exception("Your cart is empty");
            var model = _httpContextAccessor.HttpContext.Session.Get<PaymentInfoModel>("PaymentInfoModel") ?? new PaymentInfoModel();
            model.Installment = _allBankPaymentSettings.IsInstallment;
            model.NumberOfInstallments = 1;
            model.DescriptionText = _allBankPaymentSettings.DescriptionText;
            //ödeme session bilgisi gönderiliyor.
            if (model.PaymentInfoSession==Guid.Empty)
            {
                model.MarkGuid();
                _httpContextAccessor.HttpContext.Session.Set("PaymentInfoModel",model);
            }
            if (Request.Method == WebRequestMethods.Http.Get)
                return View(
                    _orderSettings.OnePageCheckoutEnabled
                        ? "~/Plugins/Payments.AllBank/Views/OnePagePaymentInfo.cshtml"
                        : "~/Plugins/Payments.AllBank/Views/PaymentInfo.cshtml", model);
            var form = await HttpContext.Request.ReadFormAsync();
            model.CardholderName = form["CardholderName"];
            model.CardNumber = form["CardNumber"];
            model.CardCode = form["CardCode"];
            model.ExpireMonth = form["ExpireMonth"];
            model.ExpireYear = form["ExpireYear"];
            var validator = new PaymentInfoValidator(_allBankPaymentSettings, _localizationService);
            var results = validator.Validate(model);
            if (results.IsValid)
                return View(
                    _orderSettings.OnePageCheckoutEnabled
                        ? "~/Plugins/Payments.AllBank/Views/OnePagePaymentInfo.cshtml"
                        : "~/Plugins/Payments.AllBank/Views/PaymentInfo.cshtml", model);
            var query = from error in results.Errors
                select error.ErrorMessage;
            model.Errors = string.Join(", ", query);
            return View(_orderSettings.OnePageCheckoutEnabled ? "~/Plugins/Payments.AllBank/Views/OnePagePaymentInfo.cshtml" : "~/Plugins/Payments.AllBank/Views/PaymentInfo.cshtml", model);
        }
    }
}

using Grand.Core;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Core.Plugins;
using Grand.Services.Common;
using Grand.Services.Customers;
using Grand.Services.Localization;
using Grand.Services.Logging;
using Grand.Services.Orders;
using Grand.Services.Payments;
using Grand.Services.Shipping;
using Grand.Web.Features.Models.Checkout;
using Grand.Web.Models.Checkout;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Grand.Domain.Data;
using Grand.Framework.Themes;
using Microsoft.AspNetCore.Hosting;
using Grand.Framework.Extensions;

namespace Grand.Plugin.Misc.SinglePageCheckout.Controllers
{
    public class SinglePageCheckoutController : Grand.Web.Controllers.CheckoutController
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ILocalizationService _localizationService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IShippingService _shippingService;
        private readonly IPaymentService _paymentService;
        private readonly IPluginFinder _pluginFinder;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IWebHelper _webHelper;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IRepository<StateProvince> _stateProvinceRepository;
        private readonly IRepository<Country> _countryRepository;
        private readonly OrderSettings _orderSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly ShippingSettings _shippingSettings;
        private readonly ShoppingCartSettings _shoppingCartSettings;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IMediator _mediator;
        private readonly AddressSettings _addressSettings;
        private readonly IThemeProvider _themeProvider;
        private readonly IThemeContext _themeContext;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly IPickupPointService _pickupPointService;
        private readonly SinglePageCheckoutSettings _singlePageCheckoutSettings;

        #endregion

        #region Constructors

        public SinglePageCheckoutController(
            IWorkContext workContext,
            IStoreContext storeContext,
            ILocalizationService localizationService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            IShippingService shippingService,
            IPaymentService paymentService,
            IPluginFinder pluginFinder,
            ILogger logger,
            IOrderService orderService,
            IWebHelper webHelper,
            IShoppingCartService shoppingCartService,
            IRepository<StateProvince> stateProvinceRepository,
            IRepository<Country> countryRepository,
            OrderSettings orderSettings,
            RewardPointsSettings rewardPointsSettings,
            PaymentSettings paymentSettings,
            ShippingSettings shippingSettings,
            ShoppingCartSettings shoppingCartSettings,
            IAddressAttributeParser addressAttributeParser,
            ICustomerActivityService customerActivityService,
            IMediator mediator,
            AddressSettings addressSettings,
            IThemeProvider themeProvider,
            IThemeContext themeContext,
            IWebHostEnvironment hostingEnvironment,
            IPickupPointService pickupPointService,
            SinglePageCheckoutSettings singlePageCheckoutSettings
            )
            : base(
            workContext,
            storeContext,
            localizationService,
            customerService,
            shoppingCartService,
            genericAttributeService,
            shippingService,
            pickupPointService,
            paymentService,
            pluginFinder,
            logger,
            orderService,
            webHelper,
            addressAttributeParser,
            customerActivityService,
            mediator,
            orderSettings,
            rewardPointsSettings,
            paymentSettings,
            shippingSettings,
            addressSettings
                  )

        {
            _workContext = workContext;
            _storeContext = storeContext;
            _localizationService = localizationService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _shippingService = shippingService;
            _paymentService = paymentService;
            _pluginFinder = pluginFinder;
            _logger = logger;
            _orderService = orderService;
            _webHelper = webHelper;
            _stateProvinceRepository = stateProvinceRepository;
            _countryRepository = countryRepository;
            _orderSettings = orderSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _paymentSettings = paymentSettings;
            _shippingSettings = shippingSettings;
            _shoppingCartSettings = shoppingCartSettings;
            _shoppingCartService = shoppingCartService;
            _addressAttributeParser = addressAttributeParser;
            _customerActivityService = customerActivityService;
            _mediator = mediator;
            _addressSettings = addressSettings;
            _themeProvider = themeProvider;
            _themeContext = themeContext;
            _hostingEnvironment = hostingEnvironment;
            _pickupPointService = pickupPointService;
            _singlePageCheckoutSettings = singlePageCheckoutSettings;
        }

        #endregion

        public override async Task<IActionResult> OnePageCheckout()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart || sci.ShoppingCartType == ShoppingCartType.Auctions)
                .LimitPerStore(false, _storeContext.CurrentStore.Id)
                .ToList();
            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (!_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("Checkout");

            if ((_workContext.CurrentCustomer.IsGuest() && !_orderSettings.AnonymousCheckoutAllowed))
                return Challenge();

            var model = new OnePageCheckoutModel {
                ShippingRequired = cart.RequiresShipping(),
                DisableBillingAddressCheckoutStep = _orderSettings.DisableBillingAddressCheckoutStep,
                //BillingAddress = await _checkoutViewModelService.PrepareBillingAddress(cart, prePopulateNewAddressWithCustomerFields: true)
                BillingAddress = await _mediator.Send(new GetBillingAddress() { 
                    Cart = cart,
                    Currency = _workContext.WorkingCurrency,
                    Customer = _workContext.CurrentCustomer,
                    PrePopulateNewAddressWithCustomerFields = true,
                    Language = _workContext.WorkingLanguage,
                    Store = _storeContext.CurrentStore
                })
            };
            if (_themeContext.WorkingThemeName == "VueTheme")
                return View("~/Plugins/Misc.SinglePageCheckout/Views/VueOnePageCheckout.cshtml", model);
            return View("~/Plugins/Misc.SinglePageCheckout/Views/OnePageCheckout.cshtml", model);
        }

        public async Task<IActionResult> OpcReloadBilling()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart || sci.ShoppingCartType == ShoppingCartType.Auctions)
                .LimitPerStore(false, _storeContext.CurrentStore.Id)
                .ToList();
            if (!cart.Any())
                throw new Exception("Your cart is empty");

            //var billingAddressModel = await _checkoutViewModelService.PrepareBillingAddress(cart, prePopulateNewAddressWithCustomerFields: true);
            var billingAddressModel = await _mediator.Send(new GetBillingAddress() {
                Cart = cart,
                PrePopulateNewAddressWithCustomerFields = true,
                Currency = _workContext.WorkingCurrency,
                Language = _workContext.WorkingLanguage,
                Customer = _workContext.CurrentCustomer,
                Store = _storeContext.CurrentStore
            });
            return Json(new
            {
                update_section = new UpdateSectionJsonModel {
                    name = "billing",
                    html = await RenderPartialViewToString("~/Views/Checkout/OpcBillingAddress.cshtml", billingAddressModel),
                    model = billingAddressModel
                },
                wrong_billing_address = false,
            });
        }

        public async Task<IActionResult> OpcReloadShipping()
        {
            //var shippingAddressModel = await _checkoutViewModelService.PrepareShippingAddress(prePopulateNewAddressWithCustomerFields: true);
            var shippingAddressModel = await _mediator.Send(new GetShippingAddress() {
               PrePopulateNewAddressWithCustomerFields  = true,
               Currency = _workContext.WorkingCurrency,
               Language = _workContext.WorkingLanguage,
               Customer = _workContext.CurrentCustomer,
               Store = _storeContext.CurrentStore
            });

            if (_shippingSettings.AllowPickUpInStore && _shippingService.LoadActiveShippingRateComputationMethods(_storeContext.CurrentStore.Id).Result.Count == 0)
            {
                shippingAddressModel.PickUpInStoreOnly = true;
                shippingAddressModel.PickUpInStore = true;
            }

            return Json(new
            {
                update_section = new UpdateSectionJsonModel {
                    name = "shipping",
                    html = await RenderPartialViewToString("~/Views/Checkout/OpcShippingAddress.cshtml", shippingAddressModel),
                    model = shippingAddressModel
                },
                goto_section = "shipping"
            });
        }
        public async Task<IActionResult> OpcReloadShippingMethod()
        {
            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart || sci.ShoppingCartType == ShoppingCartType.Auctions)
                .LimitPerStore(false, _storeContext.CurrentStore.Id)
                .ToList();

            if (!cart.Any())
                throw new Exception("Your cart is empty");

            if (cart.RequiresShipping())
            {
                var shippingaddress = _workContext.CurrentCustomer.ShippingAddress;
                if (shippingaddress == null)
                {
                    shippingaddress = new Address() {

                    };
                }

                var shippingMethodModel = await _mediator.Send(new GetShippingMethod() {
                    Currency = _workContext.WorkingCurrency,
                    Language = _workContext.WorkingLanguage,
                    Customer = _workContext.CurrentCustomer,
                    Store = _storeContext.CurrentStore,
                    Cart = cart,
                    ShippingAddress = shippingaddress
                });

                return Json(new
                {
                    update_section = new UpdateSectionJsonModel {
                        name = "shipping-method",
                        html = await RenderPartialViewToString("~/Views/Checkout/OpcShippingMethods.cshtml", shippingMethodModel),
                        model = shippingMethodModel
                    },
                    goto_section = "shipping_method"
                });
            }
            else
            {
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel {
                        name = "shipping-method",
                        html = "NotRequiresShipping"
                    },
                    goto_section = "shipping_method"
                });
            }
        }

        public async Task<IActionResult> OpcReloadPaymentMethod()
        {
            var cart = _workContext.CurrentCustomer.ShoppingCartItems
            .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart || sci.ShoppingCartType == ShoppingCartType.Auctions)
            .LimitPerStore(false, _storeContext.CurrentStore.Id)
            .ToList();

            var theme = _themeContext.WorkingThemeName;
            var path = $"~/Themes/" + theme + "/Views/Checkout/OpcConfirmOrder.cshtml";
            if(theme == "VueTheme")
                path = $"~/Plugins/Misc.SinglePageCheckout/Views/VueOpcConfirmOrder.cshtml";
            if (!System.IO.File.Exists(path.Replace("~/", "")))
                path = "~/Views/Checkout/OpcConfirmOrder.cshtml";

            if (!cart.Any())
                throw new Exception("Your cart is empty");

            //Check whether payment workflow is required
            //we ignore reward points during cart total calculation
            //bool isPaymentWorkflowRequired = await _checkoutViewModelService.IsPaymentWorkflowRequired(cart, false);
            bool isPaymentWorkflowRequired = await _mediator.Send(new GetIsPaymentWorkflowRequired() {
                Cart = cart,
                UseRewardPoints = false
            });
            if (isPaymentWorkflowRequired)
            {
                //filter by country
                string filterByCountryId = "";
                if (_addressSettings.CountryEnabled &&
                    _workContext.CurrentCustomer.BillingAddress != null &&
                    !string.IsNullOrEmpty(_workContext.CurrentCustomer.BillingAddress.CountryId))
                {
                    filterByCountryId = _workContext.CurrentCustomer.BillingAddress.CountryId;
                }

                //payment is required
                var paymentMethodModel = await _mediator.Send(new GetPaymentMethod() {
                    Currency = _workContext.WorkingCurrency,
                    Language = _workContext.WorkingLanguage,
                    Customer = _workContext.CurrentCustomer,
                    Store = _storeContext.CurrentStore,
                    FilterByCountryId = filterByCountryId,
                    Cart = cart
                });

                //customer have to choose a payment method
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel {
                        name = "payment-method",
                        html = await RenderPartialViewToString("~/Views/Checkout/OpcPaymentMethods.cshtml", paymentMethodModel),
                        model = paymentMethodModel
                    },
                    goto_section = "payment_method"
                });
            }

            //payment is not required
            await _genericAttributeService.SaveAttribute<string>(_workContext.CurrentCustomer,
                 SystemCustomerAttributeNames.SelectedPaymentMethod, null, _storeContext.CurrentStore.Id);

            var confirmOrderModel = await _mediator.Send(new GetConfirmOrder() {
                //Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                Cart = cart
            });

            return Json(new
            {
                update_section = new UpdateSectionJsonModel {
                    name = "confirm-order",
                    html = await RenderPartialViewToString(path, confirmOrderModel),
                    model = confirmOrderModel
                },
                goto_section = "confirm_order"
            });            
        }

        public override async Task<IActionResult> OpcSavePaymentInfo(IFormCollection form)
        {
            try
            {
                //validation
                var cart = _shoppingCartService.GetShoppingCart(_storeContext.CurrentStore.Id, ShoppingCartType.ShoppingCart, ShoppingCartType.Auctions);

                var isPaymentWorkflowRequired = await _mediator.Send(new GetIsPaymentWorkflowRequired() {
                    Cart = cart,
                    UseRewardPoints = false
                });

                var theme = _themeContext.WorkingThemeName;
                var path = $"~/Themes/" + theme + "/Views/Checkout/OpcConfirmOrder.cshtml";
                if (theme == "VueTheme")
                    path = $"~/Plugins/Misc.SinglePageCheckout/Views/VueOpcConfirmOrder.cshtml";
                if (!System.IO.File.Exists(path.Replace("~/", "")))
                    path = "~/Views/Checkout/OpcConfirmOrder.cshtml";

                var paymentMethodSystemName = await _workContext.CurrentCustomer.GetAttribute<string>(
                       _genericAttributeService, SystemCustomerAttributeNames.SelectedPaymentMethod,
                       _storeContext.CurrentStore.Id);
                var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(paymentMethodSystemName);
                if (paymentMethod == null)
                    throw new Exception("Payment method is not selected");

                if (isPaymentWorkflowRequired)
                {
                   
                    var warnings = await paymentMethod.ValidatePaymentForm(form);
                    foreach (var warning in warnings)
                        ModelState.AddModelError("", warning);
                    if (ModelState.IsValid)
                    {
                        //get payment info
                        var paymentInfo = await paymentMethod.GetPaymentInfo(form);
                        //session save
                        HttpContext.Session.Set("OrderPaymentInfo", paymentInfo);

                        var confirmOrderModel = await _mediator.Send(new GetConfirmOrder() {
                            //Currency = _workContext.WorkingCurrency,
                            Customer = _workContext.CurrentCustomer,
                            Cart = cart
                        });
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel {
                                name = "confirm-order",
                                html = await RenderPartialViewToString(path, confirmOrderModel),
                                model = confirmOrderModel
                            },
                            goto_section = "confirm_order"
                        });
                    }

                    //If we got this far, something failed, redisplay form
                    return Json(new { error = 1, message = warnings });

                }
                //If we got this far, something failed, redisplay form
                var paymenInfoModel = await _mediator.Send(new GetPaymentInfo() { PaymentMethod = paymentMethod });
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel {
                        name = "payment-info",
                        html = await RenderPartialViewToString("OpcPaymentInfo", paymenInfoModel),
                        model = paymenInfoModel
                    }
                });

            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }


        public async Task<IActionResult> OpcReloadConfirmOrder()
        {
            var theme = _themeContext.WorkingThemeName;
            var path = $"~/Themes/" + theme + "/Views/Checkout/OpcConfirmOrder.cshtml";
            if (theme == "VueTheme")
                path = $"~/Plugins/Misc.SinglePageCheckout/Views/VueOpcConfirmOrder.cshtml";
            if (!System.IO.File.Exists(path.Replace("~/", "")))
                path = "~/Views/Checkout/OpcConfirmOrder.cshtml";

            //validation
            var cart = _workContext.CurrentCustomer.ShoppingCartItems
                .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart || sci.ShoppingCartType == ShoppingCartType.Auctions)
                .LimitPerStore(false, _storeContext.CurrentStore.Id)
                .ToList();
            if (!cart.Any())
                throw new Exception("Your cart is empty");

            var confirmOrderModel = await _mediator.Send(new GetConfirmOrder() {
                //Currency = _workContext.WorkingCurrency,
                Customer = _workContext.CurrentCustomer,
                Cart = cart
            });
             return Json(new
                {
                    update_section = new UpdateSectionJsonModel {
                        name = "confirm-order",
                        //html = await RenderPartialViewToString("~/Themes/" + path + "/Views/Checkout/OpcConfirmOrder.cshtml", confirmOrderModel)
                        html = await RenderPartialViewToString(path, confirmOrderModel),
                        model = confirmOrderModel
                    },
                    goto_section = "confirm_order"
                }); 
        }
    }
}
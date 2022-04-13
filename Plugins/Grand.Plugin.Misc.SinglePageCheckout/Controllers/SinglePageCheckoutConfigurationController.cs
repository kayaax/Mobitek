using Grand.Framework.Controllers;
using Grand.Framework.Mvc.Filters;
using Grand.Plugin.Misc.SinglePageCheckout.Models;
using Grand.Services.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Plugin.Misc.SinglePageCheckout.Controllers
{
    [AuthorizeAdmin]
    [Area("Admin")]
    public class SinglePageCheckoutConfigurationController : BasePluginController
    {
        private readonly ISettingService _settingService;
        private readonly SinglePageCheckoutSettings _settings;

        public SinglePageCheckoutConfigurationController(
            ISettingService settingService,
            SinglePageCheckoutSettings settings
            )
        {
            _settingService = settingService;
            _settings = settings;
        }
        public IActionResult Configure()
        {
            var model = new SinglePageCheckoutConfigurationModel();
            model.Enabled = _settings.Enabled;

            return View("~/Plugins/Misc.SinglePageCheckout/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(SinglePageCheckoutConfigurationModel model)
        {
            _settings.Enabled = model.Enabled;

            _settingService.SaveSetting(_settings);
            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification("Every change of this settings require restart application, please do it");
            return Configure();
        }
    }
}

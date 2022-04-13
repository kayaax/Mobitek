using Grand.Core;
using Grand.Domain.Messages;
using Grand.Core.Plugins;
using Grand.Framework.Menu;
using Grand.Services.Cms;
using Grand.Services.Common;
using Grand.Services.Configuration;
using Grand.Services.Localization;
using Grand.Services.Messages;
using Grand.Services.Security;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Plugin.Misc.SinglePageCheckout
{
    /// <summary>
    /// Single Page Checkout plugin
    /// </summary>
    public class SinglePageCheckoutPlugin : BasePlugin, IMiscPlugin, IAdminMenuPlugin
    {
        private static string pn = "Single Page Checkout";

        private readonly IWebHelper _webHelper;
        private readonly ILocalizationService _localizationService;
        private readonly IWorkContext _workContext;
        private readonly IEmailAccountService _emailAccountService;
        private readonly EmailAccountSettings _emailAccountSettings;
        private readonly IStoreContext _storeContext;
        private readonly ILanguageService _languageService;
        private readonly IQueuedEmailService _queuedEmailService;
        private readonly IPermissionService _permissionService;

        public SinglePageCheckoutPlugin(IWebHelper webHelper,
            ILocalizationService localizationService,
            IWorkContext workContext,
            IEmailAccountService emailAccountService,
            EmailAccountSettings emailAccountSettings,
            IStoreContext storeContext,
            ILanguageService languageService,
            IQueuedEmailService queuedEmailService,
            IPermissionService permissionService)
        {
            _webHelper = webHelper;
            _workContext = workContext;
            _localizationService = localizationService;
            _emailAccountService = emailAccountService;
            _emailAccountSettings = emailAccountSettings;
            _storeContext = storeContext;
            _languageService = languageService;
            _queuedEmailService = queuedEmailService;
            _permissionService = permissionService;
        }
        
        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/singlepagecheckoutconfiguration/Configure";
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override async Task Install()
        {
            //locales
            await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Misc.SinglePageCheckout.AddNewShippingAddress", "Add new shipping address");
            await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Misc.SinglePageCheckout.AddNewBillingAddress", "Add new billing address");
            await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Misc.SinglePageCheckout.Enabled", "Enable plugin");
            await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Misc.SinglePageCheckout.Configure", "Configure plugin");
            await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "GrandNode.Plugin.Misc.SinglePageCheckout", "Single Checkout");
            await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "GrandNode.Plugin.Misc.SinglePageCheckout.configure", "Configuration");
            await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "GrandNode.Sitemap.Extensions", "Extensions");
            await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Misc.SinglePageCheckout", "One Page Checkout");
            await this.AddOrUpdatePluginLocaleResource(_localizationService, _languageService, "Plugins.Misc.SinglePageCheckout.Support", "Support");

            await CreateMessage();

            await base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override async Task Uninstall()
        {
            //locales
            var en = _localizationService.GetAllResources(_workContext.WorkingLanguage.Id)
                .Where(r => r.ResourceName.StartsWith(PluginDescriptor.SystemName));
            foreach (var item in en)
            {
               await _localizationService.DeleteLocaleStringResource(item);
            }

            await CreateMessage(false);

            await base.Uninstall();
        }

        public async Task ManageSiteMap(SiteMapNode rootNode)
        {
            if (await _permissionService.Authorize(StandardPermissionProvider.ManagePlugins))
            {
                var pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "extensions");
                if (pluginNode == null)
                {
                    rootNode.ChildNodes.Add(new SiteMapNode() {
                        SystemName = "extensions",
                        ResourceName = "GrandNode.Sitemap.Extensions",
                        Visible = true,
                        IconClass = "icon-paper-clip"
                    });
                    pluginNode = rootNode.ChildNodes.FirstOrDefault(x => x.SystemName == "extensions");
                }

                SiteMapNode Menu = new SiteMapNode();

                Menu.ResourceName = "GrandNode.Plugin.Misc.SinglePageCheckout";
                Menu.Visible = true;
                Menu.SystemName = "SOPC";
                Menu.IconClass = "fa fa-pencil-square-o";
                Menu.ChildNodes.Add(new SiteMapNode() {
                    ResourceName = "GrandNode.Plugin.Misc.SinglePageCheckout.configure",
                    Url = "~/Admin/singlepagecheckoutconfiguration/Configure",
                    Visible = true
                });

                Menu.ChildNodes.Add(new SiteMapNode() {
                    ResourceName = "Plugins.Misc.SinglePageCheckout.Support",
                    Visible = true,
                    SystemName = "SOPC.Support",
                    IconClass = "fa-life-ring",
                    OpenUrlInNewTab = true,
                    Url = "https://grandnode.com/boards/forum/59f6f028ba1646279c61a5e4/community-plugins-themes",
                });

                pluginNode.ChildNodes.Add(Menu);
            }
        }

        private async Task CreateMessage(bool install = true)
        {
            var emailAccount = await _emailAccountService.GetEmailAccountById(_emailAccountSettings.DefaultEmailAccountId);
            await _queuedEmailService.InsertQueuedEmail(new QueuedEmail() {
                CreatedOnUtc = DateTime.UtcNow,
                EmailAccountId = _emailAccountSettings.DefaultEmailAccountId,
                Priority = QueuedEmailPriority.Low,
                Subject = $"Plugin {(install ? "installation" : "uninstall")} notification {pn}",
                Body = $"<p><a href=\"{_storeContext.CurrentStore.Url}\">{_storeContext.CurrentStore.Name}</a>&nbsp;</p><p>Plugin {pn} has been installed</p>",
                To = "support@grandnode.com",
                ToName = "Support GrandNode",
                From = emailAccount.Email,
                FromName = emailAccount.DisplayName
            });
        }

    }
}
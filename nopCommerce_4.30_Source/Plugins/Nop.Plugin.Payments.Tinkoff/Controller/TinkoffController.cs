using Microsoft.AspNetCore.Mvc;

using Nop.Core;
using Nop.Plugin.Payments.Tinkoff.Models;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Tinkoff.Controller
{
    [AutoValidateAntiforgeryToken]
    public class TinkoffController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPermissionService _permissionService;
        private readonly IStoreContext _storeContext;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizationService;

        public TinkoffController(
            ISettingService settingService,
            IPermissionService permissionService,
            IStoreContext storeContext,
            INotificationService notificationService,
            ILocalizationService localizationService)
        {
            _settingService = settingService;
            _permissionService = permissionService;
            _storeContext = storeContext;
            _notificationService = notificationService;
            _localizationService = localizationService;
        }

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var tinkoffPaymentSettings = _settingService.LoadSetting<TinkoffPaymentSettings>(storeScope);

            var model = new TinkoffConfigureModel()
            {
                UseSandbox = tinkoffPaymentSettings.UseSandbox,
                SandboxPassword = tinkoffPaymentSettings.SandboxPassword,
                SandboxTerminalKey = tinkoffPaymentSettings.SandboxTerminalKey,
                WorkPassword = tinkoffPaymentSettings.WorkPassword,
                WorkTerminalKey = tinkoffPaymentSettings.WorkTerminalKey
            };

            return View("~/Plugins/Payments.Tinkoff/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        public IActionResult Configure(TinkoffConfigureModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var tinkoffPaymentSettings = _settingService.LoadSetting<TinkoffPaymentSettings>(storeScope);

            tinkoffPaymentSettings.UseSandbox = model.UseSandbox;
            tinkoffPaymentSettings.SandboxPassword = model.SandboxPassword;
            tinkoffPaymentSettings.SandboxTerminalKey = model.SandboxTerminalKey;
            tinkoffPaymentSettings.WorkPassword = model.WorkPassword;
            tinkoffPaymentSettings.WorkTerminalKey = model.WorkTerminalKey;
            tinkoffPaymentSettings.Commission = model.Commission;

            _settingService.ClearCache();

            _settingService.SaveSetting(tinkoffPaymentSettings);

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }
    }
}

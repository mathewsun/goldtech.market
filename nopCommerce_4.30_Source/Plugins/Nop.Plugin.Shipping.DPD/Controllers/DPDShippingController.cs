using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Core.Domain.Orders;
using Nop.Plugin.Shipping.DPD.Domain;
using Nop.Plugin.Shipping.DPD.Infrastructure;
using Nop.Plugin.Shipping.DPD.Models;
using Nop.Plugin.Shipping.DPD.Services;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Shipping.DPD.Controllers
{
    [AuthorizeAdmin]
    [Area(AreaNames.Admin)]
    [AutoValidateAntiforgeryToken]
    public class DPDShippingController : BasePluginController
    {
        private readonly DPDService _dpdService;
        private readonly DPDSettings _dpdSettings;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly INotificationService _notificationService;
        private readonly ILocalizationService _localizationService;
        private readonly ILanguageService _languageService;

        public DPDShippingController(
            IPermissionService permissionService,
            DPDSettings dpdSettings,
            DPDService dpdService,
            ISettingService settingService,
            INotificationService notificationService,
            ILocalizationService localizationService,
            ILanguageService languageService)
        {
            _permissionService = permissionService;
            _dpdSettings = dpdSettings;
            _dpdService = dpdService;
            _settingService = settingService;
            _notificationService = notificationService;
            _localizationService = localizationService;
            _languageService = languageService;
        }

        public IActionResult Configure()
        {
            //whether user has the authority to manage configuration
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            //prepare common model
            var model = new DPDShippingModel
            {
                ClientNumber = _dpdSettings.ClientNumber,
                ClientKey = _dpdSettings.ClientKey,
                CargoRegistered = _dpdSettings.CargoRegistered,
                UseSandbox = _dpdSettings.UseSandbox,
                AddressCode = _dpdSettings.AddressCode,
                SenderCity = _dpdSettings.SenderCity,
                CitiesApiUrl = _dpdSettings.CitiesApiUrl
            };

            List<string> serviceCodes = new List<string>();
            if (_dpdSettings.ServiceCodesOffered != null)
            {
                if (_dpdSettings.ServiceCodesOffered.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList().Count > 0)
                {
                    serviceCodes = _dpdSettings.ServiceCodesOffered.Split(':', StringSplitOptions.RemoveEmptyEntries)
                         .Select(idValue => idValue.Replace("[", "").Replace("]", "")).ToList();
                }
                else
                {
                    serviceCodes.Add(_dpdSettings.ServiceCodesOffered.Replace("[", "").Replace("]", ""));
                }
            }

            List<string> serviceVariants = new List<string>();
            if (_dpdSettings.ServiceVariantsOffered != null)
            {
                if (_dpdSettings.ServiceVariantsOffered.Split(':', StringSplitOptions.RemoveEmptyEntries).ToList().Count > 0)
                {
                    serviceVariants = _dpdSettings.ServiceVariantsOffered.Split(':', StringSplitOptions.RemoveEmptyEntries)
                        .Select(idValue => idValue.Replace("[", "").Replace("]", "")).ToList();
                }
                else
                {
                    serviceVariants.Add(_dpdSettings.ServiceVariantsOffered.Replace("[", "").Replace("]", ""));
                }
            }

            //prepare available options
            model.AvailableServiceCodeTypes = ServiceCodeType.DPDOnlineExpress.ToSelectList(false).Select(item =>
            {
                var serviceCode = _dpdService.GetDPDCode((ServiceCodeType)int.Parse(item.Value));

                return new SelectListItem(
                    $"{item.Text?.TrimStart('_').Replace(" ", "")}",
                    serviceCode,
                    serviceCodes == null ? false : serviceCodes.Any(x => x == item.Text?.TrimStart('_').Replace(" ", "")));
            }).ToList();

            model.AvailableServiceVariantTypes = ServiceVariantType.DD.ToSelectList(false).Select(item =>
            {
                var serviceVariant = _dpdService.GetDPDCode((ServiceVariantType)int.Parse(item.Value));
                return new SelectListItem(
                    $"{item.Text?.TrimStart('_').Replace(" ", "")}",
                    serviceVariant,
                    serviceVariants == null ? false : serviceVariants.Any(x => x == item.Text?.TrimStart('_').Replace(" ", "")));
            }).ToList();

            return View("~/Plugins/Shipping.DPD/Views/Configure.cshtml", model);
        }



        [HttpPost]
        public IActionResult Configure(DPDShippingModel model)
        {
            List<string> errors = new List<string>();

            //whether user has the authority to manage configuration
            if (!_permissionService.Authorize(StandardPermissionProvider.ManageShippingSettings))
                return AccessDeniedView();

            //save settings
            _dpdSettings.ClientNumber = model.ClientNumber;
            _dpdSettings.ClientKey = model.ClientKey;
            _dpdSettings.UseSandbox = model.UseSandbox;
            _dpdSettings.CargoRegistered = model.CargoRegistered;
            _dpdSettings.AddressCode = model?.AddressCode;
            _dpdSettings.CitiesApiUrl = model?.CitiesApiUrl;

            _dpdSettings.ServiceVariantsOffered =
                string.Join(':', model.ServiceVariantTypes.Select(service => $"[{service}]"));
            _dpdSettings.ServiceCodesOffered =
                string.Join(':', model.ServiceCodeTypes.Select(service => $"[{service}]"));

            if (model.IsCreateNewAddressSelected)
            {
                if (!string.IsNullOrEmpty(model.Code) &&
                !string.IsNullOrEmpty(model.Name) &&
                !string.IsNullOrEmpty(model.CountryName) &&
                !string.IsNullOrEmpty(model.City) &&
                !string.IsNullOrEmpty(model.Street) &&
                !string.IsNullOrEmpty(model.ContactFullName) &&
                !string.IsNullOrEmpty(model.ContactPhone))
                {
                    var response = (Order.createAddressResponse)_dpdService.CreateNewAddress(model);
                    
                    if (string.IsNullOrEmpty(response.@return.errorMessage))
                    {
                        _dpdSettings.SenderCity = model.City;
                        _dpdSettings.AddressCode = response.@return.code;
                    }
                    else
                    {
                        errors.Add(response.@return.errorMessage);
                    }
                }
                else
                {
                    errors.Add("Please fill in all required fields");
                }
            }
            else
            {
                if (string.IsNullOrEmpty(model.SenderCity))
                {
                    errors.Add("Sender city can't be null");
                }
                else
                {
                    _dpdSettings.SenderCity = model.SenderCity;
                }

                if (string.IsNullOrEmpty(model.AddressCode))
                {
                    errors.Add("Please fill the address code input");
                }
            }
            
            if(errors.Count > 0)
            {
                _notificationService.ErrorNotification(string.Join("\r\n", errors));
                return Configure();
            }
            else
            {
                _settingService.SaveSetting(_dpdSettings);

                _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

                return Configure();
            }
        }
    }
}
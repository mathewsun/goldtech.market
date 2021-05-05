using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Plugin.Shipping.DPD.Domain;
using Nop.Plugin.Shipping.DPD.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Plugins;
using Nop.Services.Shipping;
using Nop.Data;
using Nop.Services.Shipping.Tracking;
using Nop.Plugin.Shipping.DPD.Infrastructure;

namespace Nop.Plugin.Shipping.DPD
{
    /// <summary>
    /// Represents DPD computation method
    /// </summary>
    public class DPDComputationMethod : BasePlugin, IShippingRateComputationMethod
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly DPDService _DPDService;

        #endregion

        #region Ctor

        public DPDComputationMethod(ILocalizationService localizationService,
            ISettingService settingService,
            IWebHelper webHelper,
            DPDService DPDService)
        {
            _localizationService = localizationService;
            _settingService = settingService;
            _webHelper = webHelper;
            _DPDService = DPDService;
        }

        #endregion

        #region Methods

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException(nameof(getShippingOptionRequest));

            if (!getShippingOptionRequest.Items?.Any() ?? true)
                return new GetShippingOptionResponse { Errors = new[] { "No shipment items" } };

            if (getShippingOptionRequest.ShippingAddress == null)
                return new GetShippingOptionResponse { Errors = new[] { "Shipping address is not set" } };

            if (getShippingOptionRequest.ShippingAddress?.CountryId == null || string.IsNullOrEmpty(getShippingOptionRequest.ShippingAddress?.City))
                return new GetShippingOptionResponse { Errors = new[] { "Shipping address is not set" } };


            return _DPDService.GetRates(getShippingOptionRequest);
        }

        /// <summary>
        /// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            return null;
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/DPDShipping/Configure";
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {

            _settingService.SaveSetting(new DPDSettings
            {
                UseSandbox = true,
                CargoRegistered = false,
                ServiceCodesOffered = "[DPDExpress]:[DPDClassic]:[DPDOnlineExpress]:[DPDOptium]:[DPDEconomy]",
                ServiceVariantsOffered = "[TT]:[TD]",
                CitiesApiUrl = PluginDefaults.DefaultCitiesApiUrl
            });

            //all locales
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Enums.Nop.Plugin.Shipping.DPD.PickupTimePeriodType.NineAMToSixPM"] = "9-18",
                ["Enums.Nop.Plugin.Shipping.DPD.PickupTimePeriodType.NineAMToOnePM"] = "9-13",
                ["Enums.Nop.Plugin.Shipping.DPD.PickupTimePeriodType.OnePMToSixPM"] = "13-18",

                ["Plugins.Shipping.DPD.Fields.SenderCity"] = "Sender city",

                ["Plugins.Shipping.DPD.Fields.UseSandbox"] = "Use sandbox",

                ["Plugins.Shipping.DPD.Fields.ClientNumber"] = "Client Number",
                ["Plugins.Shipping.DPD.Fields.ClientKey"] = "Client Key",
                ["Enums.Nop.Plugin.Shipping.DPD.PaymentType.OUP"] = "Payment at the recipient",
                ["Enums.Nop.Plugin.Shipping.DPD.PaymentType.OUO"] = "Payment at the sender",

                ["Enums.Nop.Plugin.Shipping.DPD.ServiceVariantType.DD"] = "DD (Door-to-door delivery)",
                ["Enums.Nop.Plugin.Shipping.DPD.ServiceVariantType.DT"] = "DT (Door-to-terminal delivery)",
                ["Enums.Nop.Plugin.Shipping.DPD.ServiceVariantType.TD"] = "TD (Terminal-to-door delivery)",
                ["Enums.Nop.Plugin.Shipping.DPD.ServiceVariantType.TT"] = "TT (Terminal-to-terminal delivery)",

                ["Plugins.Shipping.DPD.Fields.UseSandbox"] = "Use Sandbox",
                ["Plugins.Shipping.DPD.Fields.UseSandbox.Hint"] = "Use the test version",

                ["Plugins.Shipping.DPD.Fields.PickupTimePeriodType"] = "Time period of receipt of goods",

                ["Plugins.Shipping.DPD.Fields.AvailableServiceCodeTypes"] = "DPD service codes",

                ["Plugins.Shipping.DPD.Fields.AvailableServiceVariantTypes"] = "Delivery variants",

                ["Plugins.Shipping.DPD.Fields.AddressCode"] = "Address code",
                ["Plugins.Shipping.DPD.Fields.AddressCode.Hint"] = "Address code in DPD",

                ["Plugins.Shipping.DPD.Fields.CitiesApiUrl"] = "Cities API URL",
                ["Plugins.Shipping.DPD.Fields.CitiesApiUrl.Hint"] = "URL api, to get a list of cities, then use it when searching for the city of the buyer and the sender.",

                ["Plugins.Shipping.DPD.Fields.CargoRegistered"] = "Cargo registered",
                ["Plugins.Shipping.DPD.Fields.CargoRegistered.Hint"] =
                    "Enclosure included into the list of goods subject to extra safety measures reducing risk of its loss or damage during transportation.",

                ["Plugins.Shipping.DPD.Fields.Code"] = "DPD Code (Required)",
                ["Plugins.Shipping.DPD.Fields.Name"] = "Full Name (Required)",
                ["Plugins.Shipping.DPD.Fields.CountryName"] = "Country name (Required)",
                ["Plugins.Shipping.DPD.Fields.Index"] = "Index",
                ["Plugins.Shipping.DPD.Fields.Region"] = "Region",
                ["Plugins.Shipping.DPD.Fields.City"] = "City name (Required)",
                ["Plugins.Shipping.DPD.Fields.Street"] = "Street name (Required)",
                ["Plugins.Shipping.DPD.Fields.House"] = "House (Required)",
                ["Plugins.Shipping.DPD.Fields.HouseCorps"] = "Corps",
                ["Plugins.Shipping.DPD.Fields.Building"] = "Building",
                ["Plugins.Shipping.DPD.Fields.OwnerShip"] = "Owner ship",
                ["Plugins.Shipping.DPD.Fields.Office"] = "Office",
                ["Plugins.Shipping.DPD.Fields.Apartament"] = "Apartament",
                ["Plugins.Shipping.DPD.Fields.ContactFullName"] = "Contact full name (Required)",
                ["Plugins.Shipping.DPD.Fields.ContactPhone"] = "Contact phone (Required)",
                ["Plugins.Shipping.DPD.Fields.ContactEmail"] = "Contact email",
                ["Plugins.Shipping.DPD.Fields.Instructions"] = "Insturctions for courier",
                ["Plugins.Shipping.DPD.Fields.NeedPass"] = "Need pass",
            });


            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<DPDSettings>();

            //locales
            _localizationService.DeletePluginLocaleResources("Enums.Nop.Plugin.Shipping.DPD");
            _localizationService.DeletePluginLocaleResources("Plugins.Shipping.DPD");

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType => ShippingRateComputationMethodType.Realtime;

        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker => null;

        #endregion
    }
}
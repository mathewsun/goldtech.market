using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Shipping.DPD.Infrastructure
{
    public static class PluginDefaults
    {
        public static string PluginBasePath => "~/Plugins/Shipping.DPD/";
        public static string CustomCheckoutViewPathFormat => $"{PluginBasePath}Views/Checkout/";
        public static string CustomSharedViewPathFormat = $"{PluginBasePath}Views/Shared/";
        public static string ContentPath = $"{PluginBasePath}Content/";
        public static string ScriptsPath => $"{PluginBasePath}Content/scripts/";
        public static string StylesPath => $"{PluginBasePath}Content/Styles/";
        public static string DefaultCitiesApiUrl => "http://web-api.online/api/AllCities?query=";


    }
}

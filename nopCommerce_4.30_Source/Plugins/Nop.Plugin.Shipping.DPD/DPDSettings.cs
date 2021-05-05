using Nop.Core.Configuration;

namespace Nop.Plugin.Shipping.DPD
{
    /// <summary>
    /// Represents settings of the DPD shipping plugin
    /// </summary>
    public class DPDSettings : ISettings
    {
        public long ClientNumber { get; set; }
        public string ClientKey { get; set; }
        public bool UseSandbox { get; set; }
        public bool CargoRegistered { get; set; }
        public string AddressCode { get; set; }
        public string SenderCity { get; set; }
        public string CitiesApiUrl { get; set; }
        public string ServiceCodesOffered { get; set; }
        public string ServiceVariantsOffered { get; set; }
    }
}
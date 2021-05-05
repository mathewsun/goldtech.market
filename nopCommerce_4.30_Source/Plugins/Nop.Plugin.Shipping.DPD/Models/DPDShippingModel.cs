using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc.Rendering;

using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Shipping.DPD.Models
{
    public class DPDShippingModel : BaseNopModel
    {
        public DPDShippingModel()
        {
            AvailableServiceCodeTypes = new List<SelectListItem>();
            AvailableServiceVariantTypes = new List<SelectListItem>();
        }
        public bool IsCreateNewAddressSelected { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.Code")]
        public string Code { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.CitiesApiUrl")]
        public string CitiesApiUrl { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.Name")]
        public string Name { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.CountryName")]
        public string CountryName { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.Index")]
        public string Index { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.Region")]
        public string Region { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.City")]
        public string City { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.Street")]
        public string Street { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.House")]
        public string House { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.HouseCorps")]
        public string HouseCorps { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.Building")]
        public string Building { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.OwnerShip")]
        public string OwnerShip { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.Office")]
        public string Office { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.Apartament")]
        public string Apartament { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.ContactFullName")]
        public string ContactFullName { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.ContactPhone")]
        public string ContactPhone { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.ContactEmail")]
        public string ContactEmail { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.Instructions")]
        public string Instructions { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.NeedPass")]
        public bool NeedPass { get; set; }
        [Required]
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.ClientNumber")]
        public long ClientNumber { get; set; }
        [Required]
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.ClientKey")]
        public string ClientKey { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.SenderCity")]
        public string SenderCity { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.AddressCode")]
        public string AddressCode { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.CargoRegistered")]
        public bool CargoRegistered { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.AvailableServiceCodeTypes")]
        public IList<SelectListItem> AvailableServiceCodeTypes { get; set; }
        public IList<string> ServiceCodeTypes { get; set; }
        [NopResourceDisplayName("Plugins.Shipping.DPD.Fields.AvailableServiceVariantTypes")]
        public IList<SelectListItem> AvailableServiceVariantTypes { get; set; }
        public IList<string> ServiceVariantTypes { get; set; }

    }
}
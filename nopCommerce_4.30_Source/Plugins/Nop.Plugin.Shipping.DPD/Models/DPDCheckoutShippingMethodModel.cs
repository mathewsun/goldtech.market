using Nop.Web.Models.Checkout;

using System;
using System.Collections.Generic;
using System.Text;

namespace Nop.Plugin.Shipping.DPD.Models
{
    public class DPDCheckoutShippingMethodModel : CheckoutShippingMethodModel
    {
        public OnePageCheckoutModel OnePageModel { get; set; }
        public string SenderCity { get; set; }
        public string DeliveryCity { get; set; }
        public string ClientKey { get; set; }
        public long ClientNumber { get; set; }
        public double ProductWeight { get; set; }
        public double ProductCost { get; set; }
        public string JsonAvailableServiceCodes { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

using Nop.Core;

namespace Nop.Plugin.Shipping.DPD.Domain
{
    public class PickupPointAddress : BaseEntity
    {
        public double CartItemsWeight { get; set; }
        public double CartItemsCost { get; set; }
        public string Category { get; set; }
        public string House { get; set; }
        public int UserId { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string CountryName { get; set; }
        public string CustomerFullName { get; set; }
        public string TerminalCode { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Domain.Localization;
using Nop.Data;
using Nop.Services.Localization;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Shipping.DPD.Infrastructure
{
    class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //checkout pages
            endpointRouteBuilder.MapControllerRoute("areaRouteDPDSaveShippingMethod", $"checkout/opcsaveshippingmethod/",
                new { controller = "DPDCheckout", action = "OpcSaveShippingMethod" });
            endpointRouteBuilder.MapControllerRoute("areaRouteDPDSaveShipping", $"checkout/opcsaveshipping/",
                new { controller = "DPDCheckout", action = "OpcSaveShipping" });
            
                endpointRouteBuilder.MapControllerRoute("areaRouteOpcConfirmOrder", $"checkout/opcconfirmorder",
                new { controller = "DPDCheckout", action = "OpcConfirmOrder" });
            endpointRouteBuilder.MapControllerRoute("areaRouteDPDSaveShippingMethod", $"checkout/opcsaveshippingmethod/",
                new { controller = "DPDCheckout", action = "OpcSaveShippingMethod" });
            endpointRouteBuilder.MapControllerRoute("areaCheckoutShippingMethod", $"checkout/shippingmethod",
                new { controller = "DPDCheckout", action = "ShippingMethod" });
        }

        public int Priority { get; } = int.MaxValue;
    }
}

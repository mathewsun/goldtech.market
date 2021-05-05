using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Http.Extensions;
using Nop.Plugin.Shipping.DPD.Domain;
using Nop.Plugin.Shipping.DPD.Infrastructure;
using Nop.Plugin.Shipping.DPD.Models;
using Nop.Plugin.Shipping.DPD.Services;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Data;
using Nop.Services.Directory;
using Nop.Services.Events;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Shipping;
using Nop.Web.Controllers;
using Nop.Web.Extensions;
using Nop.Web.Factories;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;
using Nop.Web.Models.Checkout;
using Nop.Web.Models.Common;
using Nop.Core.Domain.Catalog;

namespace Nop.Plugin.Shipping.DPD.Controllers
{
    [HttpsRequirement]
    [AutoValidateAntiforgeryToken]
    public class DPDCheckoutController : BasePluginController, IConsumer<OrderPlacedEvent>
    {

        private readonly AddressSettings _addressSettings;
        private readonly CustomerSettings _customerSettings;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IAddressService _addressService;
        private readonly ICheckoutModelFactory _checkoutModelFactory;
        private readonly ICountryService _countryService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPaymentService _paymentService;
        private readonly IProductService _productService;
        private readonly IShippingService _shippingService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IWebHelper _webHelper;
        private readonly ICategoryService _categoryService;
        private readonly DPDSettings _dpdSettings;
        private readonly IWorkContext _workContext;
        private readonly IRepository<PickupPointAddress> _dpdPickupPointAddressRepository;
        private readonly DPDService _dpdService;
        private readonly Order.DPDOrderClient _dpdOrder;
        private readonly OrderSettings _orderSettings;
        private readonly PaymentSettings _paymentSettings;
        private readonly IRepository<ProductCategory> _productCategoryRepository;
        private readonly IRepository<Category> _categoryRepository;
        private readonly RewardPointsSettings _rewardPointsSettings;
        private readonly ShippingSettings _shippingSettings;

        public DPDCheckoutController(AddressSettings addressSettings,
            CustomerSettings customerSettings,
            IAddressAttributeParser addressAttributeParser,
            IAddressService addressService,
            ICheckoutModelFactory checkoutModelFactory,
            ICountryService countryService,
            IRepository<PickupPointAddress> dpdPickupPointAddressRepository,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            IRepository<ProductCategory> productCategoryRepository,
            IRepository<Category> categoryRepository,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IPaymentPluginManager paymentPluginManager,
            IPaymentService paymentService,
            IProductService productService,
            IShippingService shippingService,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IWebHelper webHelper,
            ICategoryService categoryService,
            IWorkContext workContext,
            DPDSettings dpdSettings,
            DPDService dpdService,
            OrderSettings orderSettings,
            PaymentSettings paymentSettings,
            RewardPointsSettings rewardPointsSettings,
            ShippingSettings shippingSettings)
        {
            _productCategoryRepository = productCategoryRepository;
            _categoryRepository = categoryRepository;
            _categoryService = categoryService;
            _dpdSettings = dpdSettings;
            _dpdService = dpdService;
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _storeContext = storeContext;
            _shippingSettings = shippingSettings;
            _checkoutModelFactory = checkoutModelFactory;
            _orderSettings = orderSettings;
            _rewardPointsSettings = rewardPointsSettings;
            _paymentSettings = paymentSettings;
            _webHelper = webHelper;
            _dpdPickupPointAddressRepository = dpdPickupPointAddressRepository;
            _shippingService = shippingService;
            _productService = productService;
            _paymentService = paymentService;
            _paymentPluginManager = paymentPluginManager;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _logger = logger;
            _dpdOrder = new Order.DPDOrderClient();
            _localizationService = localizationService;
            _countryService = countryService;
            _checkoutModelFactory = checkoutModelFactory;
            _addressService = addressService;
            _addressAttributeParser = addressAttributeParser;
            _customerSettings = customerSettings;
            _addressSettings = addressSettings;
        }

        protected virtual bool ParsePickupInStore(IFormCollection form)
        {
            var pickupInStore = false;

            var pickupInStoreParameter = form["PickupInStore"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(pickupInStoreParameter))
                bool.TryParse(pickupInStoreParameter, out pickupInStore);

            return pickupInStore;
        }
        protected virtual void SavePickupOption(PickupPoint pickupPoint)
        {
            var pickUpInStoreShippingOption = new ShippingOption
            {
                Name = string.Format(_localizationService.GetResource("Checkout.PickupPoints.Name"), pickupPoint.Name),
                Rate = pickupPoint.PickupFee,
                Description = pickupPoint.Description,
                ShippingRateComputationMethodSystemName = pickupPoint.ProviderSystemName,
                IsPickupInStore = true
            };
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, pickUpInStoreShippingOption, _storeContext.CurrentStore.Id);
            _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedPickupPointAttribute, pickupPoint, _storeContext.CurrentStore.Id);
        }
        public virtual IActionResult OnePageCheckout()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (!_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("Checkout");

            if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            var model = _checkoutModelFactory.PrepareOnePageCheckoutModel(cart);
            return View(PluginDefaults.CustomCheckoutViewPathFormat + "OnePageCheckout.cshtml", model);
        }

        protected JsonResult OpcLoadStepAfterShippingAddress(IList<ShoppingCartItem> cart)
        {
            var shippingMethodModel = _checkoutModelFactory.PrepareShippingMethodModel(cart, _customerService.GetCustomerShippingAddress(_workContext.CurrentCustomer));
            if (_shippingSettings.BypassShippingMethodSelectionIfOnlyOne &&
                shippingMethodModel.ShippingMethods.Count == 1)
            {
                //if we have only one shipping method, then a customer doesn't have to choose a shipping method
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                    NopCustomerDefaults.SelectedShippingOptionAttribute,
                    shippingMethodModel.ShippingMethods.First().ShippingOption,
                    _storeContext.CurrentStore.Id);

                //load next step
                return OpcLoadStepAfterShippingMethod(cart);
            }

            var onePageModel = _checkoutModelFactory.PrepareOnePageCheckoutModel(cart);

            double weight = 0;
            double cost = 0;

            foreach(var item in cart)
            {
                var product = _productService.GetProductById(item.ProductId);

                weight += (double)product.Weight * item.Quantity;
                cost += (double)product.Price * item.Quantity;
            }

            var address = _addressService.GetAddressById(_workContext.CurrentCustomer.ShippingAddressId.GetValueOrDefault());            

            DPDCheckoutShippingMethodModel dpdShippingMethodModel = new DPDCheckoutShippingMethodModel()
            {
                CustomProperties = shippingMethodModel.CustomProperties,
                DisplayPickupInStore = shippingMethodModel.DisplayPickupInStore,
                NotifyCustomerAboutShippingFromMultipleLocations = shippingMethodModel.NotifyCustomerAboutShippingFromMultipleLocations,
                PickupPointsModel = shippingMethodModel.PickupPointsModel,
                ShippingMethods = shippingMethodModel.ShippingMethods,
                Warnings = shippingMethodModel.Warnings,
                OnePageModel = onePageModel,
                SenderCity = _dpdSettings.SenderCity,
                DeliveryCity = address.City,
                ClientKey = _dpdSettings.ClientKey,
                ClientNumber = _dpdSettings.ClientNumber,
                ProductWeight = weight,
                ProductCost = cost,
                JsonAvailableServiceCodes = _dpdSettings.ServiceCodesOffered
            };

            dpdShippingMethodModel.OnePageModel.BillingAddress.NewAddressPreselected = false;

            return Json(new
            {
                update_section = new UpdateSectionJsonModel
                {
                    name = "shipping-method",
                    html = RenderPartialViewToString(PluginDefaults.CustomCheckoutViewPathFormat + "OpcShippingMethods.cshtml", dpdShippingMethodModel)
                },
                goto_section = "shipping_method"
            });
        }



        protected JsonResult OpcLoadStepAfterPaymentMethod(IPaymentMethod paymentMethod, IList<ShoppingCartItem> cart)
        {
            if (paymentMethod.SkipPaymentInfo ||
                (paymentMethod.PaymentMethodType == PaymentMethodType.Redirection && _paymentSettings.SkipPaymentInfoStepForRedirectionPaymentMethods))
            {
                //skip payment info page
                var paymentInfo = new ProcessPaymentRequest();

                //session save 
                HttpContext.Session.Set("OrderPaymentInfo", paymentInfo);

                var confirmOrderModel = _checkoutModelFactory.PrepareConfirmOrderModel(cart);
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "confirm-order",
                        html = RenderPartialViewToString(PluginDefaults.CustomCheckoutViewPathFormat + "OpcConfirmOrder.cshtml", confirmOrderModel)
                    },
                    goto_section = "confirm_order"
                });
            }

            //return payment info page
            var paymenInfoModel = _checkoutModelFactory.PreparePaymentInfoModel(paymentMethod);
            return Json(new
            {
                update_section = new UpdateSectionJsonModel
                {
                    name = "payment-info",
                    html = RenderPartialViewToString(PluginDefaults.CustomCheckoutViewPathFormat + "OpcPaymentInfo.cshtml", paymenInfoModel)
                },
                goto_section = "payment_info"
            });
        }
        protected virtual bool IsMinimumOrderPlacementIntervalValid(Customer customer)
        {
            //prevent 2 orders being placed within an X seconds time frame
            if (_orderSettings.MinimumOrderPlacementInterval == 0)
                return true;

            var lastOrder = _orderService.SearchOrders(storeId: _storeContext.CurrentStore.Id,
                customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                .FirstOrDefault();
            if (lastOrder == null)
                return true;

            var interval = DateTime.UtcNow - lastOrder.CreatedOnUtc;
            return interval.TotalSeconds > _orderSettings.MinimumOrderPlacementInterval;
        }
        protected JsonResult OpcLoadStepAfterShippingMethod(IList<ShoppingCartItem> cart)
        {
            //Check whether payment workflow is required
            //we ignore reward points during cart total calculation
            var isPaymentWorkflowRequired = _orderProcessingService.IsPaymentWorkflowRequired(cart, false);
            if (isPaymentWorkflowRequired)
            {
                //filter by country
                var filterByCountryId = 0;
                if (_addressSettings.CountryEnabled)
                {
                    filterByCountryId = _customerService.GetCustomerBillingAddress(_workContext.CurrentCustomer)?.CountryId ?? 0;
                }

                //payment is required
                var paymentMethodModel = _checkoutModelFactory.PreparePaymentMethodModel(cart, filterByCountryId);

                if (_paymentSettings.BypassPaymentMethodSelectionIfOnlyOne &&
                    paymentMethodModel.PaymentMethods.Count == 1 && !paymentMethodModel.DisplayRewardPoints)
                {
                    //if we have only one payment method and reward points are disabled or the current customer doesn't have any reward points
                    //so customer doesn't have to choose a payment method

                    var selectedPaymentMethodSystemName = paymentMethodModel.PaymentMethods[0].PaymentMethodSystemName;
                    _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                        NopCustomerDefaults.SelectedPaymentMethodAttribute,
                        selectedPaymentMethodSystemName, _storeContext.CurrentStore.Id);

                    var paymentMethodInst = _paymentPluginManager
                        .LoadPluginBySystemName(selectedPaymentMethodSystemName, _workContext.CurrentCustomer, _storeContext.CurrentStore.Id);
                    if (!_paymentPluginManager.IsPluginActive(paymentMethodInst))
                        throw new Exception("Selected payment method can't be parsed");

                    return OpcLoadStepAfterPaymentMethod(paymentMethodInst, cart);
                }

                //customer have to choose a payment method
                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "payment-method",
                        html = RenderPartialViewToString(PluginDefaults.CustomCheckoutViewPathFormat + "OpcPaymentMethods.cshtml", paymentMethodModel)
                    },
                    goto_section = "payment_method"
                });
            }

            //payment is not required
            _genericAttributeService.SaveAttribute<string>(_workContext.CurrentCustomer,
                NopCustomerDefaults.SelectedPaymentMethodAttribute, null, _storeContext.CurrentStore.Id);

            var confirmOrderModel = _checkoutModelFactory.PrepareConfirmOrderModel(cart);
            return Json(new
            {
                update_section = new UpdateSectionJsonModel
                {
                    name = "confirm-order",
                    html = RenderPartialViewToString(PluginDefaults.CustomCheckoutViewPathFormat + "OpcConfirmOrder.cshtml", confirmOrderModel)
                },
                goto_section = "confirm_order"
            });
        }
        protected virtual PickupPoint ParsePickupOption(IFormCollection form)
        {
            var pickupPoint = form["pickup-points-id"].ToString().Split(new[] { "___" }, StringSplitOptions.None);
            var pickupPoints = _shippingService.GetPickupPoints(_workContext.CurrentCustomer.BillingAddressId ?? 0,
                _workContext.CurrentCustomer, pickupPoint[1], _storeContext.CurrentStore.Id).PickupPoints.ToList();
            var selectedPoint = pickupPoints.FirstOrDefault(x => x.Id.Equals(pickupPoint[0]));
            if (selectedPoint == null)
                throw new Exception("Pickup point is not allowed");

            return selectedPoint;
        }

        [IgnoreAntiforgeryToken]
        public IActionResult OpcSaveBilling(CheckoutBillingAddressModel model, IFormCollection form)
        {
            try
            {
                _dpdPickupPointAddressRepository.Insert(new PickupPointAddress()
                {
                    UserId = _workContext.CurrentCustomer.Id,
                    CustomerFullName = model.BillingNewAddress.LastName + model.BillingNewAddress.FirstName
                });

                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(_localizationService.GetResource("Checkout.Disabled"));

                var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                int.TryParse(form["billing_address_id"], out var billingAddressId);

                if (billingAddressId > 0)
                {
                    //existing address
                    var address = _customerService.GetCustomerAddress(_workContext.CurrentCustomer.Id, billingAddressId)
                        ?? throw new Exception(_localizationService.GetResource("Checkout.Address.NotFound"));

                    _workContext.CurrentCustomer.BillingAddressId = address.Id;
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                }
                else
                {
                    //new address
                    var newAddress = model.BillingNewAddress;

                    //custom address attributes
                    var customAttributes = _addressAttributeParser.ParseCustomAddressAttributes(form);
                    var customAttributeWarnings = _addressAttributeParser.GetAttributeWarnings(customAttributes);
                    foreach (var error in customAttributeWarnings)
                    {
                        ModelState.AddModelError("", error);
                    }

                    //validate model
                    if (!ModelState.IsValid)
                    {
                        //model is not valid. redisplay the form with errors
                        var billingAddressModel = _checkoutModelFactory.PrepareBillingAddressModel(cart,
                            selectedCountryId: newAddress.CountryId,
                            overrideAttributesXml: customAttributes);
                        billingAddressModel.NewAddressPreselected = true;
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel
                            {
                                name = "billing",
                                html = RenderPartialViewToString(PluginDefaults.CustomCheckoutViewPathFormat + "OpcBillingAddress.cshtml", billingAddressModel)
                            },
                            wrong_billing_address = true,
                        });
                    }

                    //try to find an address with the same values (don't duplicate records)
                    var address = _addressService.FindAddress(_customerService.GetAddressesByCustomerId(_workContext.CurrentCustomer.Id).ToList(),
                        newAddress.FirstName, newAddress.LastName, newAddress.PhoneNumber,
                        newAddress.Email, newAddress.FaxNumber, newAddress.Company,
                        newAddress.Address1, newAddress.Address2, newAddress.City,
                        newAddress.County, newAddress.StateProvinceId, newAddress.ZipPostalCode,
                        newAddress.CountryId, customAttributes);

                    if (address == null)
                    {
                        //address is not found. let's create a new one
                        address = newAddress.ToEntity();
                        address.CustomAttributes = customAttributes;
                        address.CreatedOnUtc = DateTime.UtcNow;

                        //some validation
                        if (address.CountryId == 0)
                            address.CountryId = null;

                        if (address.StateProvinceId == 0)
                            address.StateProvinceId = null;

                        _addressService.InsertAddress(address);

                        _customerService.InsertCustomerAddress(_workContext.CurrentCustomer, address);
                    }

                    _workContext.CurrentCustomer.BillingAddressId = address.Id;

                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                }

                if (_shoppingCartService.ShoppingCartRequiresShipping(cart))
                {
                    //shipping is required
                    var address = _customerService.GetCustomerBillingAddress(_workContext.CurrentCustomer);

                    //by default Shipping is available if the country is not specified
                    var shippingAllowed = _addressSettings.CountryEnabled ? _countryService.GetCountryByAddress(address)?.AllowsShipping ?? false : true;
                    if (_shippingSettings.ShipToSameAddress && model.ShipToSameAddress && shippingAllowed)
                    {
                        //ship to the same address
                        _workContext.CurrentCustomer.ShippingAddressId = address.Id;
                        _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                        //reset selected shipping method (in case if "pick up in store" was selected)
                        _genericAttributeService.SaveAttribute<ShippingOption>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, _storeContext.CurrentStore.Id);
                        _genericAttributeService.SaveAttribute<PickupPoint>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedPickupPointAttribute, null, _storeContext.CurrentStore.Id);
                        //limitation - "Ship to the same address" doesn't properly work in "pick up in store only" case (when no shipping plugins are available) 
                        return OpcLoadStepAfterShippingAddress(cart);
                    }

                    //do not ship to the same address
                    var shippingAddressModel = _checkoutModelFactory.PrepareShippingAddressModel(cart, prePopulateNewAddressWithCustomerFields: true);

                    return Json(new
                    {
                        update_section = new UpdateSectionJsonModel
                        {
                            name = "shipping",
                            html = RenderPartialViewToString(PluginDefaults.CustomCheckoutViewPathFormat + "OpcShippingAddress.cshtml", shippingAddressModel)
                        },
                        goto_section = "shipping"
                    });
                }

                //shipping is not required
                _genericAttributeService.SaveAttribute<ShippingOption>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, _storeContext.CurrentStore.Id);

                //load next step
                return OpcLoadStepAfterShippingMethod(cart);
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        public virtual IActionResult ShippingMethod()
        {
            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            if (!_shoppingCartService.ShoppingCartRequiresShipping(cart))
            {
                _genericAttributeService.SaveAttribute<ShippingOption>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, null, _storeContext.CurrentStore.Id);
                return RedirectToRoute("CheckoutPaymentMethod");
            }

            var shippingMethodModel = _checkoutModelFactory.PrepareShippingMethodModel(cart, _customerService.GetCustomerShippingAddress(_workContext.CurrentCustomer));
            var onePageModel = _checkoutModelFactory.PrepareOnePageCheckoutModel(cart);

            double weight = 0;
            double cost = 0;

            foreach (var item in cart)
            {
                var product = _productService.GetProductById(item.ProductId);

                weight += (double)product.Weight * item.Quantity;
                cost += (double)product.Price * item.Quantity;
            }

            var address = _addressService.GetAddressById(_workContext.CurrentCustomer.ShippingAddressId.GetValueOrDefault());

            DPDCheckoutShippingMethodModel dpdShippingMethodModel = new DPDCheckoutShippingMethodModel()
            {
                CustomProperties = shippingMethodModel.CustomProperties,
                DisplayPickupInStore = shippingMethodModel.DisplayPickupInStore,
                NotifyCustomerAboutShippingFromMultipleLocations = shippingMethodModel.NotifyCustomerAboutShippingFromMultipleLocations,
                PickupPointsModel = shippingMethodModel.PickupPointsModel,
                ShippingMethods = shippingMethodModel.ShippingMethods,
                Warnings = shippingMethodModel.Warnings,
                OnePageModel = onePageModel,
                SenderCity = _dpdSettings.SenderCity,
                DeliveryCity = address.City,
                ClientKey = _dpdSettings.ClientKey,
                ClientNumber = _dpdSettings.ClientNumber,
                ProductWeight = weight,
                ProductCost = cost,
                JsonAvailableServiceCodes = _dpdSettings.ServiceCodesOffered
            };

            dpdShippingMethodModel.OnePageModel.BillingAddress.NewAddressPreselected = false;

            if (_shippingSettings.BypassShippingMethodSelectionIfOnlyOne &&
                dpdShippingMethodModel.ShippingMethods.Count == 1)
            {
                //if we have only one shipping method, then a customer doesn't have to choose a shipping method
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer,
                    NopCustomerDefaults.SelectedShippingOptionAttribute,
                    dpdShippingMethodModel.ShippingMethods.First().ShippingOption,
                    _storeContext.CurrentStore.Id);

                return RedirectToRoute("CheckoutPaymentMethod");
            }

            return View(PluginDefaults.CustomCheckoutViewPathFormat + "ShippingMethod.cshtml", dpdShippingMethodModel);
        }

        [IgnoreAntiforgeryToken]
        public virtual IActionResult OpcSaveShipping(CheckoutShippingAddressModel model, IFormCollection form)
        {
            try
            {
                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(_localizationService.GetResource("Checkout.Disabled"));

                var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                if (!_shoppingCartService.ShoppingCartRequiresShipping(cart))
                    throw new Exception("Shipping is not required");

                //pickup point
                if (_shippingSettings.AllowPickupInStore && !_orderSettings.DisplayPickupInStoreOnShippingMethodPage)
                {
                    var pickupInStore = ParsePickupInStore(form);
                    if (pickupInStore)
                    {
                        var pickupOption = ParsePickupOption(form);
                        SavePickupOption(pickupOption);

                        return OpcLoadStepAfterShippingMethod(cart);
                    }
                    //set value indicating that "pick up in store" option has not been chosen
                    _genericAttributeService.SaveAttribute<PickupPoint>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedPickupPointAttribute, null, _storeContext.CurrentStore.Id);
                }

                int.TryParse(form["shipping_address_id"], out var shippingAddressId);

                if (shippingAddressId > 0)
                {
                    //existing address

                    var address = _customerService.GetCustomerAddress(_workContext.CurrentCustomer.Id, shippingAddressId)
                        ?? throw new Exception(_localizationService.GetResource("Checkout.Address.NotFound"));

                    _workContext.CurrentCustomer.ShippingAddressId = address.Id;
                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                }
                else
                {
                    //new address
                    var newAddress = model.ShippingNewAddress;

                    //custom address attributes
                    var customAttributes = _addressAttributeParser.ParseCustomAddressAttributes(form);
                    var customAttributeWarnings = _addressAttributeParser.GetAttributeWarnings(customAttributes);
                    foreach (var error in customAttributeWarnings)
                    {
                        ModelState.AddModelError("", error);
                    }

                    //validate model
                    if (!ModelState.IsValid)
                    {
                        //model is not valid. redisplay the form with errors
                        var shippingAddressModel = _checkoutModelFactory.PrepareShippingAddressModel(cart,
                            selectedCountryId: newAddress.CountryId,
                            overrideAttributesXml: customAttributes);
                        shippingAddressModel.NewAddressPreselected = true;
                        return Json(new
                        {
                            update_section = new UpdateSectionJsonModel
                            {
                                name = "shipping",
                                html = RenderPartialViewToString(PluginDefaults.CustomCheckoutViewPathFormat + "OpcShippingAddress.cshtml", shippingAddressModel)
                            }
                        });
                    }

                    //try to find an address with the same values (don't duplicate records)
                    var address = _addressService.FindAddress(_customerService.GetAddressesByCustomerId(_workContext.CurrentCustomer.Id).ToList(),
                        newAddress.FirstName, newAddress.LastName, newAddress.PhoneNumber,
                        newAddress.Email, newAddress.FaxNumber, newAddress.Company,
                        newAddress.Address1, newAddress.Address2, newAddress.City,
                        newAddress.County, newAddress.StateProvinceId, newAddress.ZipPostalCode,
                        newAddress.CountryId, customAttributes);

                    if (address == null)
                    {
                        address = newAddress.ToEntity();
                        address.CustomAttributes = customAttributes;
                        address.CreatedOnUtc = DateTime.UtcNow;

                        _addressService.InsertAddress(address);

                        _customerService.InsertCustomerAddress(_workContext.CurrentCustomer, address);
                    }

                    _workContext.CurrentCustomer.ShippingAddressId = address.Id;

                    _customerService.UpdateCustomer(_workContext.CurrentCustomer);
                }

                return OpcLoadStepAfterShippingAddress(cart);
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }
        [IgnoreAntiforgeryToken]
        public virtual IActionResult OpcSaveShippingMethod(string shippingoption, IFormCollection form)
        {
            try
            {
                if (shippingoption.ToLower().Contains("dpd-dt"))
                {
                    if (!string.IsNullOrEmpty(form["pickupaddress"][0]) &&
                       !string.IsNullOrEmpty(form["pickupaddress"][1]) &&
                       !string.IsNullOrEmpty(form["pickupaddress"][2]) &&
                       !string.IsNullOrEmpty(form["pickupaddress"][3]) &&
                       !string.IsNullOrEmpty(form["pickupaddress"][4]))
                    {
                        var dpdPickupPointAddresses = from pickupAddresses in _dpdPickupPointAddressRepository.Table
                                                      select pickupAddresses;

                        var dpdPickupPointAddress = dpdPickupPointAddresses.ToList().LastOrDefault(x => x.UserId == _workContext.CurrentCustomer.Id);
                        if (dpdPickupPointAddress != null)
                        {
                            dpdPickupPointAddress.TerminalCode = form["pickupaddress"][0];
                            dpdPickupPointAddress.CountryName = form["pickupaddress"][1];
                            dpdPickupPointAddress.City = form["pickupaddress"][2];
                            dpdPickupPointAddress.Street = form["pickupaddress"][3];
                            dpdPickupPointAddress.House = form["pickupaddress"][4];

                            _dpdPickupPointAddressRepository.Update(dpdPickupPointAddress);
                        }
                        else
                        {
                            _logger.Warning("Pick-up address was not saved", new ArgumentNullException(), _workContext.CurrentCustomer);
                            return Json(new { error = 1, message = "Pick-up address is null" });
                        }


                    }
                    else
                    {
                        _logger.Warning("Pick-up address is null", new ArgumentNullException(), _workContext.CurrentCustomer);
                        return Json(new { error = 1, message = "Pick-up address is null" });
                    }
                }
                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(_localizationService.GetResource("Checkout.Disabled"));

                var cart =
                    _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                if (!_shoppingCartService.ShoppingCartRequiresShipping(cart))
                    throw new Exception("Shipping is not required");

                //pickup point
                if (_shippingSettings.AllowPickupInStore && _orderSettings.DisplayPickupInStoreOnShippingMethodPage)
                {
                    var pickupInStore = ParsePickupInStore(form);
                    if (pickupInStore)
                    {
                        var pickupOption = ParsePickupOption(form);
                        SavePickupOption(pickupOption);

                        return OpcLoadStepAfterShippingMethod(cart);
                    }

                    //set value indicating that "pick up in store" option has not been chosen
                    _genericAttributeService.SaveAttribute<PickupPoint>(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedPickupPointAttribute, null, _storeContext.CurrentStore.Id);
                }

                //parse selected method 
                if (string.IsNullOrEmpty(shippingoption))
                    throw new Exception("Selected shipping method can't be parsed");
                var splittedOption = shippingoption.Split(new[] { "___" }, StringSplitOptions.RemoveEmptyEntries);
                if (splittedOption.Length != 2)
                    throw new Exception("Selected shipping method can't be parsed");
                var selectedName = splittedOption[0];
                var shippingRateComputationMethodSystemName = splittedOption[1];

                //find it
                //performance optimization. try cache first
                var shippingOptions = _genericAttributeService.GetAttribute<List<ShippingOption>>(_workContext.CurrentCustomer,
                    NopCustomerDefaults.OfferedShippingOptionsAttribute, _storeContext.CurrentStore.Id);
                if (shippingOptions == null || !shippingOptions.Any())
                {
                    //not found? let's load them using shipping service
                    shippingOptions = _shippingService.GetShippingOptions(cart, _customerService.GetCustomerShippingAddress(_workContext.CurrentCustomer),
                        _workContext.CurrentCustomer, shippingRateComputationMethodSystemName, _storeContext.CurrentStore.Id).ShippingOptions.ToList();
                }
                else
                {
                    //loaded cached results. let's filter result by a chosen shipping rate computation method
                    shippingOptions = shippingOptions.Where(so => so.ShippingRateComputationMethodSystemName.Equals(shippingRateComputationMethodSystemName, StringComparison.InvariantCultureIgnoreCase))
                        .ToList();
                }

                var shippingOption = shippingOptions
                    .Find(so => !string.IsNullOrEmpty(so.Name) && so.Name.Equals(selectedName, StringComparison.InvariantCultureIgnoreCase));
                if (shippingOption == null)
                    throw new Exception("Selected shipping method can't be loaded");

                //save
                _genericAttributeService.SaveAttribute(_workContext.CurrentCustomer, NopCustomerDefaults.SelectedShippingOptionAttribute, shippingOption, _storeContext.CurrentStore.Id);

                //load next step
                return OpcLoadStepAfterShippingMethod(cart);
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [IgnoreAntiforgeryToken]
        public virtual IActionResult OpcConfirmOrder()
        {
            try
            {
                //validation
                if (_orderSettings.CheckoutDisabled)
                    throw new Exception(_localizationService.GetResource("Checkout.Disabled"));

                var cart =
                    _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

                if (!cart.Any())
                    throw new Exception("Your cart is empty");

                var dpdPickupPointAddresses = from pickupAddresses in _dpdPickupPointAddressRepository.Table
                                              select pickupAddresses;

                var dpdPickupPointAddress = dpdPickupPointAddresses.ToList().LastOrDefault(x => x.UserId == _workContext.CurrentCustomer.Id);

                List<string> productsCategories = new List<string>();

                dpdPickupPointAddress.CartItemsCost = 50000;

                foreach (var productId in cart.Select(x => x.ProductId))
                {
                    var product = _productService.GetProductById(productId);

                    dpdPickupPointAddress.CartItemsWeight += (double)product.Weight;
                    dpdPickupPointAddress.CartItemsCost += (double)product.Price;

                    var categoryId = _productCategoryRepository.Table.FirstOrDefault(x => x.ProductId == productId).CategoryId;

                    var categoryName = _categoryRepository.Table.FirstOrDefault(x => x.Id == categoryId).Name;

                    productsCategories.Add(categoryName);
                }

                string uniqueCategoryNamesString = productsCategories.FirstOrDefault();

                if (productsCategories.Count > 1)
                {
                    uniqueCategoryNamesString = string.Join(", ", productsCategories.Distinct());
                }
                 

                dpdPickupPointAddress.Category = uniqueCategoryNamesString;

                _dpdPickupPointAddressRepository.Update(dpdPickupPointAddress);

                if (!_orderSettings.OnePageCheckoutEnabled)
                    throw new Exception("One page checkout is disabled");

                if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                    throw new Exception("Anonymous checkout is not allowed");

                //prevent 2 orders being placed within an X seconds time frame
                if (!IsMinimumOrderPlacementIntervalValid(_workContext.CurrentCustomer))
                    throw new Exception(_localizationService.GetResource("Checkout.MinOrderPlacementInterval"));

                //place order
                var processPaymentRequest = HttpContext.Session.Get<ProcessPaymentRequest>("OrderPaymentInfo");
                if (processPaymentRequest == null)
                {
                    //Check whether payment workflow is required
                    if (_orderProcessingService.IsPaymentWorkflowRequired(cart))
                    {
                        throw new Exception("Payment information is not entered");
                    }

                    processPaymentRequest = new ProcessPaymentRequest();
                }
                _paymentService.GenerateOrderGuid(processPaymentRequest);
                processPaymentRequest.StoreId = _storeContext.CurrentStore.Id;
                processPaymentRequest.CustomerId = _workContext.CurrentCustomer.Id;
                processPaymentRequest.PaymentMethodSystemName = _genericAttributeService.GetAttribute<string>(_workContext.CurrentCustomer,
                    NopCustomerDefaults.SelectedPaymentMethodAttribute, _storeContext.CurrentStore.Id);
                HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", processPaymentRequest);
                var placeOrderResult = _orderProcessingService.PlaceOrder(processPaymentRequest);

                if (placeOrderResult.Success)
                {
                    HttpContext.Session.Set<ProcessPaymentRequest>("OrderPaymentInfo", null);
                    var postProcessPaymentRequest = new PostProcessPaymentRequest
                    {
                        Order = placeOrderResult.PlacedOrder
                    };

                    var paymentMethod = _paymentPluginManager
                        .LoadPluginBySystemName(placeOrderResult.PlacedOrder.PaymentMethodSystemName, _workContext.CurrentCustomer, _storeContext.CurrentStore.Id);
                    if (paymentMethod == null)
                        //payment method could be null if order total is 0
                        //success
                        return Json(new { success = 1 });

                    if (paymentMethod.PaymentMethodType == PaymentMethodType.Redirection)
                    {
                        //Redirection will not work because it's AJAX request.
                        //That's why we don't process it here (we redirect a user to another page where he'll be redirected)

                        //redirect
                        return Json(new
                        {
                            redirect = $"{_webHelper.GetStoreLocation()}checkout/OpcCompleteRedirectionPayment"
                        });
                    }

                    _paymentService.PostProcessPayment(postProcessPaymentRequest);
                    //success
                    return Json(new { success = 1 });
                }

                //error
                var confirmOrderModel = new CheckoutConfirmModel();
                foreach (var error in placeOrderResult.Errors)
                    confirmOrderModel.Warnings.Add(error);

                return Json(new
                {
                    update_section = new UpdateSectionJsonModel
                    {
                        name = "confirm-order",
                        html = RenderPartialViewToString("OpcConfirmOrder", confirmOrderModel)
                    },
                    goto_section = "confirm_order"
                });
            }
            catch (Exception exc)
            {
                _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                return Json(new { error = 1, message = exc.Message });
            }
        }

        [HttpPost, ActionName("BillingAddress")]
        public virtual IActionResult NewBillingAddress(CheckoutBillingAddressModel model, IFormCollection form)
        {

            //validation
            if (_orderSettings.CheckoutDisabled)
                return RedirectToRoute("ShoppingCart");

            var cart = _shoppingCartService.GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);

            if (!cart.Any())
                return RedirectToRoute("ShoppingCart");

            if (_orderSettings.OnePageCheckoutEnabled)
                return RedirectToRoute("CheckoutOnePage");

            if (_customerService.IsGuest(_workContext.CurrentCustomer) && !_orderSettings.AnonymousCheckoutAllowed)
                return Challenge();

            //custom address attributes
            var customAttributes = _addressAttributeParser.ParseCustomAddressAttributes(form);
            var customAttributeWarnings = _addressAttributeParser.GetAttributeWarnings(customAttributes);
            foreach (var error in customAttributeWarnings)
            {
                ModelState.AddModelError("", error);
            }

            var newAddress = model.BillingNewAddress;

            if (ModelState.IsValid)
            {
                //try to find an address with the same values (don't duplicate records)
                var address = _addressService.FindAddress(_customerService.GetAddressesByCustomerId(_workContext.CurrentCustomer.Id).ToList(),
                    newAddress.FirstName, newAddress.LastName, newAddress.PhoneNumber,
                    newAddress.Email, newAddress.FaxNumber, newAddress.Company,
                    newAddress.Address1, newAddress.Address2, newAddress.City,
                    newAddress.County, newAddress.StateProvinceId, newAddress.ZipPostalCode,
                    newAddress.CountryId, customAttributes);

                if (address == null)
                {
                    //address is not found. let's create a new one
                    address = newAddress.ToEntity();
                    address.CustomAttributes = customAttributes;
                    address.CreatedOnUtc = DateTime.UtcNow;

                    //some validation
                    if (address.CountryId == 0)
                        address.CountryId = null;
                    if (address.StateProvinceId == 0)
                        address.StateProvinceId = null;

                    _addressService.InsertAddress(address);

                    _customerService.InsertCustomerAddress(_workContext.CurrentCustomer, address);
                }

                _workContext.CurrentCustomer.BillingAddressId = address.Id;

                _customerService.UpdateCustomer(_workContext.CurrentCustomer);

                return RedirectToRoute("CheckoutShippingAddress");
            }

            //If we got this far, something failed, redisplay form
            model = _checkoutModelFactory.PrepareBillingAddressModel(cart,
                selectedCountryId: newAddress.CountryId,
                overrideAttributesXml: customAttributes);

            return View(model);
        }

        private void ClearPickUpPointsByUserId(int UserId)
        {
            var pickUpPoints = _dpdPickupPointAddressRepository.Table.Where(x => x.UserId == UserId).ToList();

            if (pickUpPoints != null)
            {
                pickUpPoints.ForEach(x => _dpdPickupPointAddressRepository.Delete(x));
            }
        }

        public void HandleEvent(OrderPlacedEvent eventMessage)
        {
            if (eventMessage.Order.ShippingRateComputationMethodSystemName.Contains("DPD"))
            {
                try
                {
                    Order.createOrderResponse response = new Order.createOrderResponse();

                    var dpdPickupPointAddresses = from pickupAddresses in _dpdPickupPointAddressRepository.Table            
                                select pickupAddresses;

                    var dpdPickupPointAddress = dpdPickupPointAddresses.ToList().LastOrDefault(x => x.UserId == _workContext.CurrentCustomer.Id);

                    if (dpdPickupPointAddress != null)
                    {
                        response = (Order.createOrderResponse)_dpdService.CreateShippingRequest(eventMessage.Order.Id, dpdPickupPointAddress);
                    }
                    else
                    {
                        var exc = new ArgumentNullException(nameof(dpdPickupPointAddress), "was null");

                        _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);

                        throw exc;
                    }

                    ClearPickUpPointsByUserId(_workContext.CurrentCustomer.Id);

                    response.@return.ToList().ForEach(x =>
                    {
                        if (!string.IsNullOrEmpty(x.errorMessage))
                        {
                            _logger.Warning(x.errorMessage, new Exception(nameof(x)), _workContext.CurrentCustomer);
                            throw new Exception(x.errorMessage);
                        }
                    });
                }
                catch (Exception exc)
                {
                    ClearPickUpPointsByUserId(_workContext.CurrentCustomer.Id);
                    _logger.Warning(exc.Message, exc, _workContext.CurrentCustomer);
                    throw new Exception(exc.Message);
                }

            }
        }
    }
}


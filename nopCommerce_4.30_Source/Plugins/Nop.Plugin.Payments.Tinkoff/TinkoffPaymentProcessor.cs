using Microsoft.AspNetCore.Http;

using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi;
using Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Commands;
using Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Enums;
using Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Models;
using Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.ResponseEntity;
using Nop.Services.Catalog;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Plugins;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nop.Plugin.Payments.Tinkoff
{
    public class TinkoffPaymentProcessor : BasePlugin, IPaymentMethod
    {
        private readonly IWebHelper _webHelper;
        private readonly IOrderService _orderService;
        private readonly IProductService _productService;
        private readonly INotificationService _notificationService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IStoreContext _storeContext;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TinkoffPaymentProcessor(
            IWebHelper webHelper,
            IOrderService orderService,
            IProductService productService,
            IHttpContextAccessor httpContextAccessor,
            INotificationService notificationService,
            IOrderProcessingService orderProcessingService,
            ISettingService settingService,
            ILocalizationService localizationService,
            IStoreContext storeContext)
        {
            _webHelper = webHelper;
            _orderService = orderService;
            _productService = productService;
            _httpContextAccessor = httpContextAccessor;
            _notificationService = notificationService;
            _orderProcessingService = orderProcessingService;
            _settingService = settingService;
            _localizationService = localizationService;
            _storeContext = storeContext;
        }

        private List<ReceiptItem> GetReceiptItems(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var products = _orderService.GetOrderItems(postProcessPaymentRequest.Order.Id).Select(x =>
            {
                var product = _productService.GetProductById(x.ProductId);

                var item = new ReceiptItem(
                    product.Name,
                    x.Quantity,
                    product.Price * 100,
                    TinkoffPaymentClientApi.Enums.ETax.vat20);

                return item;
            });

            var shippingCost = new ReceiptItem(
                "Shipping",
                1,
                postProcessPaymentRequest.Order.OrderShippingInclTax * 100,
                TinkoffPaymentClientApi.Enums.ETax.vat20);

            var orderTax = new ReceiptItem(
                "Order tax",
                1,
                postProcessPaymentRequest.Order.OrderTax * 100,
                TinkoffPaymentClientApi.Enums.ETax.vat20);

            var paymentTax = new ReceiptItem(
                "Payment tax",
                1,
                postProcessPaymentRequest.Order.PaymentMethodAdditionalFeeInclTax * 100,
                TinkoffPaymentClientApi.Enums.ETax.vat20);

            var receiptItems = new List<ReceiptItem>()
            {
                shippingCost, orderTax, paymentTax
            };
            receiptItems.AddRange(products);

            return receiptItems;
        }

        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/Tinkoff/Configure";
        }

        //Copied from PayPalStandart plugin
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //let's ensure that at least 5 seconds passed after order is placed
            //P.S. there's no any particular reason for that. we just do it
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds < 5)
                return false;

            return true;
        }

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var tinkoffPaymentSettings = _settingService.LoadSetting<TinkoffPaymentSettings>(storeScope);

            return tinkoffPaymentSettings.Commission;
        }

        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
        }

        public string GetPublicViewComponentName()
        {
            return "Tinkoff";
        }

        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var tinkoffPaymentSettings = _settingService.LoadSetting<TinkoffPaymentSettings>(storeScope);

            using (var clientApi = new TinkoffPaymentClient(
                tinkoffPaymentSettings.UseSandbox ? tinkoffPaymentSettings.SandboxTerminalKey : tinkoffPaymentSettings.WorkTerminalKey,
                tinkoffPaymentSettings.UseSandbox ? tinkoffPaymentSettings.SandboxPassword : tinkoffPaymentSettings.WorkPassword))
            {
                CancellationToken cancellationToken = CancellationToken.None;

                var result = clientApi.InitAsync(
                    new Init(postProcessPaymentRequest.Order.Id.ToString(),
                             postProcessPaymentRequest.Order.OrderTotal * 100)
                    {
                        Receipt = new Receipt("alex.pigaloyv@gmail.com", TinkoffPaymentClientApi.Enums.ETaxation.osn)
                        {
                            ReceiptItems = GetReceiptItems(postProcessPaymentRequest)
                        }
                    }, cancellationToken).Result;

                if (result.Success)
                {
                    _httpContextAccessor.HttpContext.Response.Redirect(result.PaymentURL);
                    _ = ListenResponseFromTinkoff(new GetState(result.PaymentId)
                    {
                        IP = postProcessPaymentRequest.Order.CustomerIp,
                        TerminalKey = result.TerminalKey,
                        Token = cancellationToken.ToString()
                    }, cancellationToken, postProcessPaymentRequest.Order);
                }
                else
                {
                    _notificationService.ErrorNotification(result.ErrorCode + ": " + result.Details);
                }
            }
        }

        public async Task ListenResponseFromTinkoff(GetState state, CancellationToken token, Order order)
        {
            var storeScope = _storeContext.ActiveStoreScopeConfiguration;
            var tinkoffPaymentSettings = _settingService.LoadSetting<TinkoffPaymentSettings>(storeScope);

            using (var clientApi = new TinkoffPaymentClient(
                tinkoffPaymentSettings.UseSandbox ? tinkoffPaymentSettings.SandboxTerminalKey : tinkoffPaymentSettings.WorkTerminalKey,
                tinkoffPaymentSettings.UseSandbox ? tinkoffPaymentSettings.SandboxPassword : tinkoffPaymentSettings.WorkPassword))
            {
                GetStateResponse paymentResponse = null;
                int tick = 0;

                do
                {
                    await Task.Delay(1000 * 5);
                    tick += 1;
                    paymentResponse = await clientApi.GetStateAsync(state, token);

                    if (tick >= 120) break;
                } 
                while (paymentResponse.Status != EStatusResponse.CONFIRMED);

                if(paymentResponse.Status != EStatusResponse.CONFIRMED && tick >= 120)
                {
                    _orderService.InsertOrderNote(new OrderNote()
                    {
                        OrderId = order.Id,
                        Note = "Payment time out",
                        CreatedOnUtc = DateTime.Now,
                        DisplayToCustomer = false
                    });
                }
                else if(paymentResponse.Status == EStatusResponse.CONFIRMED)
                {
                    order.AuthorizationTransactionId = paymentResponse.PaymentId;
                    _orderService.UpdateOrder(order);
                    _orderProcessingService.MarkOrderAsPaid(order);
                }
            }
        }

        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new TinkoffPaymentSettings
            {
                UseSandbox = true
            });

            //locales
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
            {
                ["Plugins.Payments.Tinkoff.Fields.UseSandbox"] = "Use sandbox",
                ["Plugins.Payments.Tinkoff.Fields.SandboxTerminalKey"] = "Sandbox terminal key",
                ["Plugins.Payments.Tinkoff.Fields.SandboxPassword"] = "Sandbox password",
                ["Plugins.Payments.Tinkoff.Fields.WorkTerminalKey"] = "Work terminal key",
                ["Plugins.Payments.Tinkoff.Fields.WorkPassword"] = "Work password",
                ["Plugins.Payments.Tinkoff.Fields.Comission"] = "Comission",
            });

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<TinkoffPaymentSettings>();

            //locales
            _localizationService.DeletePluginLocaleResources("Plugins.Payments.Tinkoff");

            base.Uninstall();
        }

        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult();
        }

        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            return new RefundPaymentResult { Errors = new[] { "Refund method not supported" } };
        }

        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        #region Properties

        public bool SupportCapture => false;

        public bool SupportPartiallyRefund => false;

        public bool SupportRefund => false;

        public bool SupportVoid => false;

        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        public bool SkipPaymentInfo => true;

        public string PaymentMethodDescription => "";

        #endregion
    }
}

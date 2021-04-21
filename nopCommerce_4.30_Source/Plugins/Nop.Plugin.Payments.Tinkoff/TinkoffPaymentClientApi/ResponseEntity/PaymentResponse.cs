using Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Enums;

namespace Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.ResponseEntity
{
    public class PaymentResponse : TinkoffResponse
    {
        public decimal Amount { get; set; }
        public string OrderId { get; set; }
        public string PaymentId { get; set; }

        public EStatusResponse Status { get; set; }
        public string PaymentURL { get; set; }
    }
}
using Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Enums;

namespace Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.ResponseEntity
{
    public class GetStateResponse : TinkoffResponse
    {
        public string OrderId { get; set; }
        public string PaymentId { get; set; }

        public EStatusResponse Status { get; set; }
    }
}
using Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Enums;

namespace Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.ResponseEntity
{
    public class RemoveCardResponse : TinkoffResponse
    {
        public string CustomerKey { get; set; }
        public string CardId { get; set; }
        public ECardStatus Status { get; set; }
        public ECardType CardType { get; set; }
    }
}
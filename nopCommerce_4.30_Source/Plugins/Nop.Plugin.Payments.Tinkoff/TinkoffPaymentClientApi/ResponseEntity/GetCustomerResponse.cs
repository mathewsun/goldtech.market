namespace Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.ResponseEntity
{
    public class GetCustomerResponse : CustomerResponse
    {
        public string Email { get; set; }
        public string Phone { get; set; }
    }
}
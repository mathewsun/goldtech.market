using Newtonsoft.Json;

using Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Attributes;

namespace Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Commands
{
    public abstract class BaseCommand
    {
        [IgnoreTokenCalculate]
        internal abstract string CommandName { get; }
        [JsonProperty]
        [IgnoreTokenCalculate]
        internal string Token { get; set; }
        [JsonProperty]
        internal string TerminalKey { get; set; }
    }
}
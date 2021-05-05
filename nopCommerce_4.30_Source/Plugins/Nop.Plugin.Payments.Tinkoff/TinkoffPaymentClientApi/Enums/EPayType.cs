using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EPayType
    {
        /// <summary>
        /// Одностадийная
        /// </summary>
        O = 1,
        /// <summary>
        /// Двухстадийная
        /// </summary>
        T
    }
}
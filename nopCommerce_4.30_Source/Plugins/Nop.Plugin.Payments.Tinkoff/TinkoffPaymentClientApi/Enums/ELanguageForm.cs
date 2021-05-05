using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Enums
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ELanguageForm
    {
        en = 1,
        ru,
    }
}
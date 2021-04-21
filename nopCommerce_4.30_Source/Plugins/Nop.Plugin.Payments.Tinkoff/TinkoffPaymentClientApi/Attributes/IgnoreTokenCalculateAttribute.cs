using System;

namespace Nop.Plugin.Payments.Tinkoff.TinkoffPaymentClientApi.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class IgnoreTokenCalculateAttribute : Attribute
    {
        public IgnoreTokenCalculateAttribute() { }
    }
}

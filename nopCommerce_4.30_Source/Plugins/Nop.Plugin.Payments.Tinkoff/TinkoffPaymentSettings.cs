using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Tinkoff
{
    public class TinkoffPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }

        #region Sandbox data
        public string SandboxTerminalKey { get; set; }
        public string SandboxPassword { get; set; }
        #endregion

        #region WorkData
        public string WorkTerminalKey { get; set; }
        public string WorkPassword { get; set; }
        #endregion
        public int Commission { get; set; }
    }
}

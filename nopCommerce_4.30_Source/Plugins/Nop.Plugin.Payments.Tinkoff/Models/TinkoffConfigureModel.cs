using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Tinkoff.Models
{
    public class TinkoffConfigureModel
    {
        [NopResourceDisplayName("Plugins.Payments.Tinkoff.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }
        #region Sandbox data
        [NopResourceDisplayName("Plugins.Payments.Tinkoff.Fields.SandboxTerminalKey")]
        public string SandboxTerminalKey { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Tinkoff.Fields.SandboxPassword")]
        public string SandboxPassword { get; set; }
        #endregion

        #region WorkData
        [NopResourceDisplayName("Plugins.Payments.Tinkoff.Fields.WorkTerminalKey")]
        public string WorkTerminalKey { get; set; }
        [NopResourceDisplayName("Plugins.Payments.Tinkoff.Fields.WorkPassword")]
        public string WorkPassword { get; set; }
        #endregion
        public int Commission { get; set; }
    }
}

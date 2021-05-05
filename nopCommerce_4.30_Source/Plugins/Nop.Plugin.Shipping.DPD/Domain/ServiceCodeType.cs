namespace Nop.Plugin.Shipping.DPD.Domain
{
    public enum ServiceCodeType
    {
        [DPDCode("NDY")] DPDExpress,
        [DPDCode("CUR")] DPDClassic,
        [DPDCode("CSM")] DPDOnlineExpress,
        [DPDCode("PCL")] DPDOptium,
        [DPDCode("ECN")] DPDEconomy,
    }
}
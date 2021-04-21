namespace Nop.Plugin.Shipping.DPD.Domain
{
    public enum ServiceVariantType
    {
        [DPDCode("ToPerson")] DD,
        [DPDCode("ToTerminal")] DT
    }
}
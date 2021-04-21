using System;

namespace Nop.Plugin.Shipping.DPD.Domain
{
    internal class DPDCodeAttribute : Attribute
    {
        #region Ctor

        public DPDCodeAttribute(string codeValue)
        {
            Code = codeValue;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets a code value
        /// </summary>
        public string Code { get; }

        #endregion
    }
}
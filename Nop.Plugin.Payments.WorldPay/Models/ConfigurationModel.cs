using System.ComponentModel;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.WorldPay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.WorldPay.UseSandbox")]
        public bool UseSandbox { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.InstanceId")]
        public string InstanceId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.CreditCard")]
        public string CreditCard { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.CallbackPassword")]
        public string CallbackPassword { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.CssName")]
        public string CssName { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.AdditionalFee")]
        public decimal AdditionalFee { get; set; }
    }
}
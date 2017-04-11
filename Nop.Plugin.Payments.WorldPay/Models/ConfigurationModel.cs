using System.Web.Mvc;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.WorldPay.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        [NopResourceDisplayName("Plugins.Payments.WorldPay.UseSandbox")]
        public bool UseSandbox { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.SecureNetID")]
        public string SecureNetID { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.SecureKey")]
        public string SecureKey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.AdditionalFee")]
        public decimal AdditionalFee { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.AdditionalFeePercentage")]
        public bool AdditionalFeePercentage { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.EndPoint")]
        public string EndPoint { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.TransactMode")]
        public int TransactModeId { get; set; }
        public SelectList TransactModes { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.DeveloperId")]
        public int DeveloperId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.WorldPay.DeveloperVersion")]
        public string DeveloperVersion { get; set; }
    }
}
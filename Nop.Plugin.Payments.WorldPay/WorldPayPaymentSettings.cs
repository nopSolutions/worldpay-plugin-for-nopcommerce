using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.WorldPay
{
    public class WorldPayPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }
        public string InstanceId { get; set; }
        public string CreditCard { get; set; }
        public string CallbackPassword { get; set; }
        public string CssName { get; set; }
        public decimal AdditionalFee { get; set; }
    }
}

using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.WorldPay
{
    public class WorldPayPaymentSettings : ISettings
    {
        public bool UseSandbox { get; set; }
        
        public string SecureNetID { get; set; }

        public string SecureKey { get; set; }

        public decimal AdditionalFee { get; set; }

        public bool AdditionalFeePercentage { get; set; }

        public TransactMode TransactMode { get; set; }

        public string EndPoint { get; set; }

        public int DeveloperId { get; set; }

        public string DeveloperVersion { get; set; }
    }
}

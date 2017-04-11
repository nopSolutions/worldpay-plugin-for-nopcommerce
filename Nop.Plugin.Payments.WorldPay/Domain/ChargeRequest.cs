using Newtonsoft.Json;

namespace Nop.Plugin.Payments.WorldPay.Domain
{
    public class ChargeRequest : PaymentRequest
    {
        [JsonIgnore]
        public override string Command { get { return "/payments/Charge"; } }
    }
}
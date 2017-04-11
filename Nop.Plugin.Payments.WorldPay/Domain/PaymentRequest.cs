using Newtonsoft.Json;

namespace Nop.Plugin.Payments.WorldPay.Domain
{
    public class PaymentRequest
    {
        [JsonIgnore]
        public virtual string Command { get; protected set; }

        [JsonProperty("amount")]
        public virtual decimal Amount { get; set; }

        [JsonProperty("card")]
        public virtual Card Card { get; set; }

        [JsonProperty("developerApplication")]
        public DeveloperApplication DeveloperApplication { get; set; }
    }
}
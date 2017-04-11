using Newtonsoft.Json;

namespace Nop.Plugin.Payments.WorldPay.Domain
{
    public class VoidRequest : PaymentRequest
    {
        [JsonProperty("transactionId")]
        public int? TransactionId { get; set; }

        [JsonIgnore]
        public override Card Card { get; set; }

        [JsonIgnore]
        public override decimal Amount { get; set; }

        [JsonIgnore]
        public override string Command { get { return "/payments/Void"; } }
    }
}
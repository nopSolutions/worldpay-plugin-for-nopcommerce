using Newtonsoft.Json;

namespace Nop.Plugin.Payments.WorldPay.Domain
{
    public class PriorAuthCaptureRequest : PaymentRequest
    {
        [JsonProperty("transactionId")]
        public int? TransactionId { get; set; }

        [JsonIgnore]
        public override Card Card { get; set; }

        [JsonIgnore]
        public override string Command
        {
            get { return "/payments/Capture"; }
        }
    }
}
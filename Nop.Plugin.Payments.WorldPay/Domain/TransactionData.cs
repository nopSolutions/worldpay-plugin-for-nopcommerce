using Newtonsoft.Json;

namespace Nop.Plugin.Payments.WorldPay.Domain
{
    public class TransactionData
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("amount")]
        public double Amount { get; set; }
    }
}

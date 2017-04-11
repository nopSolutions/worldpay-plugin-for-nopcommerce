using Newtonsoft.Json;

namespace Nop.Plugin.Payments.WorldPay.Domain
{
    public class PaymentResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("responseCode")]
        public int ResponseCode { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("responseDateTime")]
        public string ResponseDateTime { get; set; }

        [JsonProperty("rawRequest")]
        public object RawRequest { get; set; }

        [JsonProperty("rawResponse")]
        public object RawResponse { get; set; }

        [JsonProperty("jsonRequest")]
        public object JsonRequest { get; set; }

        [JsonProperty("transaction")]
        public Transaction Transaction { get; set; }
    }
}
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.WorldPay.Domain
{
    public class Address
    {
        [JsonProperty("line1")]
        public string Line1 { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }
        
        [JsonProperty("state")]
        public string State { get; set; }
        
        [JsonProperty("zip")]
        public string Zip { get; set; }
        
        [JsonProperty("country")]
        public string Country { get; set; }
    }
}
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.WorldPay.Domain
{
    public class DeveloperApplication
    {
        [JsonProperty("developerId")]
        public int DeveloperId { get; set; }
        
        [JsonProperty("version")]
        public string Version { get; set; }
    }
}
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.WorldPay.Domain
{
    public class AuthorizeRequest : PaymentRequest
    {
        [JsonIgnore]
        public override string Command { get { return "/payments/Authorize"; } }
    }
}
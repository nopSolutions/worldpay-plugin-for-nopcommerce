using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.WorldPay
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Return
            routes.MapRoute("Plugin.Payments.WorldPay.Return",
                 "Plugins/PaymentWorldPay/Return",
                 new { controller = "PaymentWorldPay", action = "Return" },
                 new[] { "Nop.Plugin.Payments.WorldPay.Controllers" }
            );
        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.WorldPay.Models;
using Nop.Services.Configuration;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.WorldPay.Controllers
{
    public class PaymentWorldPayController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly IPaymentService _paymentService;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IWebHelper _webHelper;
        private readonly WorldPayPaymentSettings _worldPayPaymentSettings;
        private readonly PaymentSettings _paymentSettings;

        public PaymentWorldPayController(ISettingService settingService, 
            IPaymentService paymentService, IOrderService orderService, 
            IOrderProcessingService orderProcessingService, IWebHelper webHelper,
            WorldPayPaymentSettings worldPayPaymentSettings,
            PaymentSettings paymentSettings)
        {
            this._settingService = settingService;
            this._paymentService = paymentService;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
            this._webHelper = webHelper;
            this._worldPayPaymentSettings = worldPayPaymentSettings;
            this._paymentSettings = paymentSettings;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel();
            model.UseSandbox = _worldPayPaymentSettings.UseSandbox;
            model.InstanceId = _worldPayPaymentSettings.InstanceId;
            model.CreditCard = _worldPayPaymentSettings.CreditCard;
            model.CallbackPassword = _worldPayPaymentSettings.CallbackPassword;
            model.CssName = _worldPayPaymentSettings.CssName;
            model.AdditionalFee = _worldPayPaymentSettings.AdditionalFee;

            return View("~/Plugins/Payments.WorldPay/Views/PaymentWorldPay/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _worldPayPaymentSettings.UseSandbox = model.UseSandbox;
            _worldPayPaymentSettings.InstanceId = model.InstanceId;
            _worldPayPaymentSettings.CreditCard = model.CreditCard;
            _worldPayPaymentSettings.CallbackPassword = model.CallbackPassword;
            _worldPayPaymentSettings.CssName = model.CssName;
            _worldPayPaymentSettings.AdditionalFee = model.AdditionalFee;
            _settingService.SaveSetting(_worldPayPaymentSettings);

            return View("~/Plugins/Payments.WorldPay/Views/PaymentWorldPay/Configure.cshtml", model);
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();
            return View("~/Plugins/Payments.WorldPay/Views/PaymentWorldPay/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        [ValidateInput(false)]
        public string Return(FormCollection form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.WorldPay") as WorldPayPaymentProcessor;
            if (processor == null || !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("WorldPay module cannot be loaded");

            string transStatus = CommonHelper.EnsureNotNull(form["transStatus"]);
            string returnedcallbackPw = CommonHelper.EnsureNotNull(form["callbackPW"]);
            string orderId = CommonHelper.EnsureNotNull(form["cartId"]);
            string returnedInstanceId = CommonHelper.EnsureNotNull(form["instId"]);
            string callbackPassword = _worldPayPaymentSettings.CallbackPassword;
            string transId = CommonHelper.EnsureNotNull(form["transId"]);
            string transResult = _webHelper.QueryString<string>("msg");
            string instanceId = _worldPayPaymentSettings.InstanceId;

            var order = _orderService.GetOrderById(Convert.ToInt32(orderId));
            if (order == null)
                throw new NopException(string.Format("The order ID {0} doesn't exists", orderId));

            if (string.IsNullOrEmpty(instanceId))
                throw new NopException("Worldpay Instance ID is not set");

            if (string.IsNullOrEmpty(returnedInstanceId))
                throw new NopException("Returned Worldpay Instance ID is not set");

            if (instanceId.Trim() != returnedInstanceId.Trim())
                throw new NopException(string.Format("The Instance ID ({0}) received for order {1} does not match the WorldPay Instance ID stored in the database ({2})", returnedInstanceId, orderId, instanceId));

            if (returnedcallbackPw.Trim() != callbackPassword.Trim())
                throw new NopException(string.Format("The callback password ({0}) received within the Worldpay Callback for the order {1} does not match that stored in your database.", returnedcallbackPw, orderId));


            string status = "n/a";
            if (transStatus.ToUpper() == "Y")
            {
                status = "Successful";
            }
            else if (transStatus.ToUpper() == "C")
            {
                status = "Cancelled";
            }

            var sb = new StringBuilder();
            sb.AppendLine("WorldPay details:");
            sb.AppendLine("OrderID: " + orderId);
            sb.AppendLine("WorldPay Transaction ID: " + transId);
            sb.AppendLine(String.Format("Transaction Status: {0} ({1})", transStatus, status));
            sb.AppendLine("Transaction Result: " + transResult);
            order.OrderNotes.Add(new OrderNote()
            {
                Note = sb.ToString(),
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });
            _orderService.UpdateOrder(order);

            /*if (transStatus.ToLower() != "y")
                return  Content(string.Format("The transaction status received from WorldPay ({0}) for the order {1} was declined.", transStatus, orderId));
            */

            if (_orderProcessingService.CanMarkOrderAsPaid(order))
            {
                _orderProcessingService.MarkOrderAsPaid(order);
            }

            //return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
            string html = "<html><head><meta http-equiv=\"refresh\" content=\"0; url=" + _webHelper.GetStoreLocation(false) + "/checkout/completed/" + order.Id + "\"><title>Some title here</title></head><body><h2><WPDISPLAY ITEM=banner></h2><br /><h2>Object moved to <a href=\"" + _webHelper.GetStoreLocation(false) + "/checkout/completed/" + order.Id + "\">here</a>.</h2></body></html>";
            return html;
        }
    }
}
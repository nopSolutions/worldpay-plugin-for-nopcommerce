using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.WorldPay.Controllers;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Framework;

namespace Nop.Plugin.Payments.WorldPay
{
    /// <summary>
    /// WorldPay payment processor
    /// </summary>
    public class WorldPayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly WorldPayPaymentSettings _worldPayPaymentSettings;
        private readonly IStoreContext _storeContext;
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;

        #endregion

        #region Ctor

        public WorldPayPaymentProcessor(WorldPayPaymentSettings worldPayPaymentSettings,
            IStoreContext storeContext, ICurrencyService currencyService,
            CurrencySettings currencySettings,
            ISettingService settingService, IWebHelper webHelper, IWorkContext workContext)
        {
            this._worldPayPaymentSettings = worldPayPaymentSettings;
            this._storeContext = storeContext;
            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._settingService = settingService;
            this._webHelper = webHelper;
            this._workContext = workContext;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Gets Worldpay URL
        /// </summary>
        /// <returns></returns>
        private string GetWorldpayUrl()
        {
            return _worldPayPaymentSettings.UseSandbox ?
                "https://secure-test.worldpay.com/wcc/purchase" :
                "https://secure.worldpay.com/wcc/purchase";
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            #region Dummy test card numbers
            //card type
            //card number
            //number length
            //issue no length
            //Mastercard
            //5100080000000000
            //16
            //0
            //Visa Delta - UK
            //4406080400000000
            //16
            //0
            //Visa Delta - Non UK
            //4462030000000000
            //16
            //0
            //Visa
            //4911830000000
            //13
            //0
            //Visa
            //4917610000000000
            //16
            //0
            //American Express
            //370000200000000
            //15
            //0
            //Diners
            //36700102000000
            //14
            //0
            //JCB
            //3528000700000000
            //16
            //0
            //Visa Electron (UK only)
            //4917300800000000
            //16
            //0
            //Solo
            //6334580500000000
            //16
            //0
            //Solo
            //633473060000000000
            //18
            //1
            //Discover Card
            //6011000400000000
            //16
            //0
            //Laser
            //630495060000000000
            //18
            //0
            //Maestro (UK only)
            //6759649826438453
            //16
            //0
            //Visa Purchasing
            //4484070000000000
            //16
            //0
            #endregion


            string returnUrl = _webHelper.GetStoreLocation(false) + "Plugins/PaymentWorldPay/Return";

            var remotePostHelper = new RemotePost();
            remotePostHelper.FormName = "WorldpayForm";
            remotePostHelper.Url = GetWorldpayUrl();

            remotePostHelper.Add("instId", _worldPayPaymentSettings.InstanceId);
            remotePostHelper.Add("cartId", postProcessPaymentRequest.Order.Id.ToString());

            if (!string.IsNullOrEmpty(_worldPayPaymentSettings.CreditCard))
            {
                remotePostHelper.Add("paymentType", _worldPayPaymentSettings.CreditCard);
            }

            if (!string.IsNullOrEmpty(_worldPayPaymentSettings.CssName))
            {
                remotePostHelper.Add("MC_WorldPayCSSName", _worldPayPaymentSettings.CssName);
            }

            remotePostHelper.Add("currency", _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode);
            remotePostHelper.Add("email", postProcessPaymentRequest.Order.BillingAddress.Email);
            remotePostHelper.Add("hideContact", "true");
            remotePostHelper.Add("noLanguageMenu", "true");
            remotePostHelper.Add("withDelivery", postProcessPaymentRequest.Order.ShippingStatus != ShippingStatus.ShippingNotRequired ? "true" : "false");
            remotePostHelper.Add("fixContact", "false");
            remotePostHelper.Add("amount", postProcessPaymentRequest.Order.OrderTotal.ToString(new CultureInfo("en-US", false).NumberFormat));
            remotePostHelper.Add("desc", _storeContext.CurrentStore.Name);
            remotePostHelper.Add("M_UserID", postProcessPaymentRequest.Order.CustomerId.ToString());
            remotePostHelper.Add("M_FirstName", postProcessPaymentRequest.Order.BillingAddress.FirstName);
            remotePostHelper.Add("M_LastName", postProcessPaymentRequest.Order.BillingAddress.LastName);
            remotePostHelper.Add("M_Addr1", postProcessPaymentRequest.Order.BillingAddress.Address1);
            remotePostHelper.Add("tel", postProcessPaymentRequest.Order.BillingAddress.PhoneNumber);
            remotePostHelper.Add("M_Addr2", postProcessPaymentRequest.Order.BillingAddress.Address2);
            remotePostHelper.Add("M_Business", postProcessPaymentRequest.Order.BillingAddress.Company);

            var cultureInfo = new CultureInfo(_workContext.WorkingLanguage.LanguageCulture);
            remotePostHelper.Add("lang", cultureInfo.TwoLetterISOLanguageName);

            var billingStateProvince = postProcessPaymentRequest.Order.BillingAddress.StateProvince;
            if (billingStateProvince != null)
                remotePostHelper.Add("M_StateCounty", billingStateProvince.Abbreviation);
            else
                remotePostHelper.Add("M_StateCounty", "");
            if (!_worldPayPaymentSettings.UseSandbox)
                remotePostHelper.Add("testMode", "0");
            else
                remotePostHelper.Add("testMode", "100");
            remotePostHelper.Add("postcode", postProcessPaymentRequest.Order.BillingAddress.ZipPostalCode);
            var billingCountry = postProcessPaymentRequest.Order.BillingAddress.Country;
            if (billingCountry != null)
                remotePostHelper.Add("country", billingCountry.TwoLetterIsoCode);
            else
                remotePostHelper.Add("country", "");

            remotePostHelper.Add("address", postProcessPaymentRequest.Order.BillingAddress.Address1 + "," + (billingCountry != null ? billingCountry.Name : ""));
            remotePostHelper.Add("MC_callback", returnUrl);
            remotePostHelper.Add("name", postProcessPaymentRequest.Order.BillingAddress.FirstName + " " + postProcessPaymentRequest.Order.BillingAddress.LastName);

            if (postProcessPaymentRequest.Order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            {
                remotePostHelper.Add("delvName", postProcessPaymentRequest.Order.ShippingAddress.FirstName + " " + postProcessPaymentRequest.Order.ShippingAddress.LastName);
                string delvAddress = postProcessPaymentRequest.Order.ShippingAddress.Address1;
                delvAddress += (!string.IsNullOrEmpty(postProcessPaymentRequest.Order.ShippingAddress.Address2)) ? " " + postProcessPaymentRequest.Order.ShippingAddress.Address2 : string.Empty;
                remotePostHelper.Add("delvAddress", delvAddress);
                remotePostHelper.Add("delvPostcode", postProcessPaymentRequest.Order.ShippingAddress.ZipPostalCode);
                var shippingCountry = postProcessPaymentRequest.Order.ShippingAddress.Country;
                remotePostHelper.Add("delvCountry", shippingCountry.TwoLetterIsoCode);
            }

            remotePostHelper.Post();
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return _worldPayPaymentSettings.AdditionalFee;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            //WorldPay is the redirection payment method
            //It also validates whether order is also paid (after redirection) so customers will not be able to pay twice
            
            //payment status should be Pending
            if (order.PaymentStatus != PaymentStatus.Pending)
                return false;

            //let's ensure that at least 1 minute passed after order is placed
            if ((DateTime.UtcNow - order.CreatedOnUtc).TotalMinutes < 1)
                return false;

            return true;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentWorldPay";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.WorldPay.Controllers" }, { "area", null } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentWorldPay";
            routeValues = new RouteValueDictionary() { { "Namespaces", "Nop.Plugin.Payments.WorldPay.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentWorldPayController);
        }

        public override void Install()
        {
            var settings = new WorldPayPaymentSettings()
            {
                UseSandbox = true,
                InstanceId = "",
                CreditCard = "",
                CallbackPassword = "",
                CssName = "",
                AdditionalFee = 0,
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.RedirectionTip", "You will be redirected to WorldPay site to complete the order.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.UseSandbox", "Use Sandbox");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.UseSandbox.Hint", "Use sandbox?");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.InstanceId", "Instance ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.InstanceId.Hint", "Enter instance ID.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.CreditCard", "Payment Method");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.CreditCard.Hint", "Enter payment method");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.CallbackPassword", "Callback password");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.CallbackPassword.Hint", "Enter callback password.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.CssName", "CSS");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.CssName.Hint", "Enter CSS.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            
            base.Install();
        }

        public override void Uninstall()
        {
            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.RedirectionTip");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.InstanceId");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.InstanceId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.CreditCard");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.CreditCard.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.CallbackPassword");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.CallbackPassword.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.CssName");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.CssName.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFee.Hint");
            

            base.Uninstall();
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.NotSupported;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Redirection;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        #endregion
    }
}

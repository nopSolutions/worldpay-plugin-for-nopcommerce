using System;
using System.Collections.Generic;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Plugins;
using Nop.Plugin.Payments.WorldPay.Controllers;
using Nop.Plugin.Payments.WorldPay.Domain;
using Nop.Plugin.Payments.WorldPay.Helpers;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;

namespace Nop.Plugin.Payments.WorldPay
{
    /// <summary>
    /// WorldPay payment processor
    /// </summary>
    public class WorldPayPaymentProcessor : BasePlugin, IPaymentMethod
    {
        #region Fields

        private readonly WorldPayPaymentSettings _worldPayPaymentSettings;
        private readonly ICurrencyService _currencyService;
        private readonly ISettingService _settingService;
        private readonly ICustomerService _customerService;
        private readonly ILogger _logger;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ILocalizationService _localizationService;

        #endregion

        #region Ctor

        public WorldPayPaymentProcessor(WorldPayPaymentSettings worldPayPaymentSettings,
            ICurrencyService currencyService,
            ISettingService settingService,
            ICustomerService customerService,
            ILogger logger,
            IOrderTotalCalculationService orderTotalCalculationService,
            ILocalizationService localizationService)
        {
            this._worldPayPaymentSettings = worldPayPaymentSettings;
            this._currencyService = currencyService;
            this._settingService = settingService;
            this._customerService = customerService;
            this._logger = logger;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._localizationService = localizationService;
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
            //get USD currency
            var usdCurrency = _currencyService.GetCurrencyByCode("USD");
            if (usdCurrency == null)
                throw new NopException("USD currency could not be loaded");

            //get order amount in USD currency
            var amount = _currencyService.ConvertFromPrimaryStoreCurrency(processPaymentRequest.OrderTotal, usdCurrency);

            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);

            var names = processPaymentRequest.CreditCardName.Split(' ');
            var firstName = names[0];
            var lastName = names.Length > 1 ? names[1] : string.Empty;

            var card = new Card
            {
                Number = processPaymentRequest.CreditCardNumber,
                Cvv = processPaymentRequest.CreditCardCvv2,
                ExpirationDate =
                    processPaymentRequest.CreditCardExpireMonth.ToString("D2") + "/" +
                    processPaymentRequest.CreditCardExpireYear,
                Address = new Address
                {
                    Line1 = customer.BillingAddress.Address1,
                    City = customer.BillingAddress.City,
                    Zip = customer.BillingAddress.ZipPostalCode
                },
                FirstName = firstName
            };

            if (!string.IsNullOrEmpty(lastName))
                card.LastName = lastName;

            PaymentRequest request;

            if (_worldPayPaymentSettings.TransactMode == TransactMode.Authorize)
            {
                request = new AuthorizeRequest
                {
                    Amount = amount,
                    Card = card
                };
            }
            else
            {
                request = new ChargeRequest
                {
                    Amount = amount,
                    Card = card
                };
            }

            var result = new ProcessPaymentResult();

            var response = WorldPayHelper.PostRequest(_worldPayPaymentSettings, request, _logger);

            if (response == null)
            {
                const string error = "worldPay unknown error";
                _logger.Error(error);
                result.AddError(error);
                return result;
            }

            if (response.Success)
            {
                if (_worldPayPaymentSettings.TransactMode == TransactMode.Authorize)
                {
                    result.AuthorizationTransactionId = response.Transaction.TransactionId.ToString(); 
                }
                if (_worldPayPaymentSettings.TransactMode == TransactMode.AuthorizeAndCapture)
                {
                    result.CaptureTransactionId = string.Format("{0},{1}", response.Transaction.TransactionId, response.Transaction.AuthorizationCode);
                }

                result.AuthorizationTransactionResult = response.Result;
                result.AvsResult = response.Transaction.AvsResult;
                result.NewPaymentStatus = _worldPayPaymentSettings.TransactMode == TransactMode.Authorize ? PaymentStatus.Authorized : PaymentStatus.Paid;
            }
            else
            {
                var error = string.Format("worldPay error: {0} ({1})", response.Result, response.Message);
               _logger.Error(error);
                result.AddError(error);
            }

            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //nothing
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
            var result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart,
                _worldPayPaymentSettings.AdditionalFee, _worldPayPaymentSettings.AdditionalFeePercentage);
            return result;
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            //get USD currency
            var usdCurrency = _currencyService.GetCurrencyByCode("USD");
            if (usdCurrency == null)
                throw new NopException("USD currency could not be loaded");

            //get order amount in USD currency
            var amount = _currencyService.ConvertFromPrimaryStoreCurrency(capturePaymentRequest.Order.OrderTotal, usdCurrency);

            var result = new CapturePaymentResult();

            PaymentRequest request = new PriorAuthCaptureRequest
            {
                Amount = amount,
                TransactionId = int.Parse(capturePaymentRequest.Order.AuthorizationTransactionId)
            };

            var response = WorldPayHelper.PostRequest(_worldPayPaymentSettings, request, _logger);

            //validate
            if (response == null || !response.Success)
            {
                result.Errors.Add(string.Format("worldPay error: {0}", response == null ? "unknown error" : response.Message));
                return result;
            }

            result.CaptureTransactionId = string.Format("{0},{1}", response.Transaction.TransactionId, response.Transaction.AuthorizationCode);
            result.CaptureTransactionResult = response.Result;
            result.NewPaymentStatus = PaymentStatus.Paid;

            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            //get USD currency
            var usdCurrency = _currencyService.GetCurrencyByCode("USD");
            if (usdCurrency == null)
                throw new NopException("USD currency could not be loaded");

            //get order amount in USD currency
            var amount = _currencyService.ConvertFromPrimaryStoreCurrency(refundPaymentRequest.AmountToRefund, usdCurrency);

            var result = new RefundPaymentResult();

            PaymentRequest request = new RefundRequest
            {
                Amount = amount,
                TransactionId = int.Parse(refundPaymentRequest.Order.AuthorizationTransactionId ?? refundPaymentRequest.Order.CaptureTransactionId.Split(',')[0])
            };

            var response = WorldPayHelper.PostRequest(_worldPayPaymentSettings, request, _logger);

            //validate
            if (response == null || !response.Success)
            {
                result.Errors.Add(string.Format("worldPay error: {0}", response == null ? "unknown error" : response.Message));
            }

            result.NewPaymentStatus = PaymentStatus.PartiallyRefunded;

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

            PaymentRequest request = new VoidRequest
            {
                TransactionId = int.Parse(voidPaymentRequest.Order.AuthorizationTransactionId ?? voidPaymentRequest.Order.CaptureTransactionId.Split(',')[0])
            };

            var response = WorldPayHelper.PostRequest(_worldPayPaymentSettings, request, _logger);

            //validate
            if (response == null || !response.Success)
            {
                result.Errors.Add(string.Format("worldPay error: {0}", response == null ? "unknown error" : response.Message));
            }

            result.NewPaymentStatus = PaymentStatus.Voided;

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
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.WorldPay.Controllers" }, { "area", null } };
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
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.WorldPay.Controllers" }, { "area", null } };
        }

        public Type GetControllerType()
        {
            return typeof(PaymentWorldPayController);
        }

        public override void Install()
        {
            var settings = new WorldPayPaymentSettings
            {
                UseSandbox = true,
                AdditionalFee = 0,
                TransactMode = TransactMode.AuthorizeAndCapture,
                EndPoint = "https://gwapi.demo.securenet.com/api/",
                DeveloperId = 12345678,
                DeveloperVersion = "1.0"
            };
            _settingService.SaveSetting(settings);

            //locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.UseSandbox", "Use Sandbox");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.UseSandbox.Hint", "Use sandbox?");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.PaymentMethodDescription", "Pay by credit / debit card");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.TransactMode", "Transaction mode");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.TransactMode.Hint", "Choose transaction mode.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.SecureNetID", "Secure Net ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.SecureNetID.Hint", "Specify secure Net ID. You will get this in an email shortly after signing up for your account.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.SecureKey", "Secure key");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.SecureKey.Hint", "Specify secure key. You can obtain the Secure Key by signing into the Virtual Terminal with the login credentials that you were emailed to you during the sign-up process. You will then need to navigate to Settings and click on the Key Management link.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.EndPoint", "Endpoint");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.EndPoint.Hint", "Specify processed endpoint.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.DeveloperId", "Developer ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.DeveloperId.Hint", "Specify developer ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.DeveloperVersion", "Developer version");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.DeveloperVersion.Hint", "Specify developer version");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFee", "Additional fee");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFee.Hint", "Enter additional fee to charge your customers.");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFeePercentage", "Additional fee. Use percentage");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFeePercentage.Hint", "Determines whether to apply a percentage additional fee to the order total. If not enabled, a fixed value is used.");

            base.Install();
        }

        public override void Uninstall()
        {
            //settings
            _settingService.DeleteSetting<WorldPayPaymentSettings>();

            //locales
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.UseSandbox");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.UseSandbox.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.PaymentMethodDescription");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.TransactMode");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.TransactMode.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.SecureNetID");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.SecureNetID.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.SecureKey");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.SecureKey.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.EndPoint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.EndPoint.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.DeveloperId");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.DeveloperId.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.DeveloperVersion");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.DeveloperVersion.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFee");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFee.Hint");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFeePercentage");
            this.DeletePluginLocaleResource("Plugins.Payments.WorldPay.AdditionalFeePercentage.Hint");

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
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid
        {
            get
            {
                return true;
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
                return PaymentMethodType.Standard;
            }
        }

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription
        {
            get { return _localizationService.GetResource("Plugins.Payments.AuthorizeNet.PaymentMethodDescription"); }
        }

        #endregion
    }
}

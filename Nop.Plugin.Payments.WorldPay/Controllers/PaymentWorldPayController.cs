using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Nop.Plugin.Payments.WorldPay.Models;
using Nop.Plugin.Payments.WorldPay.Validators;
using Nop.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Payments;
using Nop.Web.Framework.Controllers;

namespace Nop.Plugin.Payments.WorldPay.Controllers
{
    public class PaymentWorldPayController : BasePaymentController
    {
        private readonly ISettingService _settingService;
        private readonly WorldPayPaymentSettings _worldPayPaymentSettings;
        private readonly ILocalizationService _localizationService;

        public PaymentWorldPayController(ISettingService settingService, 
            WorldPayPaymentSettings worldPayPaymentSettings,
            ILocalizationService localizationService)
        {
            this._settingService = settingService;
            this._worldPayPaymentSettings = worldPayPaymentSettings;
            this._localizationService = localizationService;
        }
        
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            var model = new ConfigurationModel
            {
                SecureNetID = _worldPayPaymentSettings.SecureNetID,
                SecureKey = _worldPayPaymentSettings.SecureKey,
                UseSandbox = _worldPayPaymentSettings.UseSandbox,
                TransactModeId = (int) _worldPayPaymentSettings.TransactMode,
                TransactModes = _worldPayPaymentSettings.TransactMode.ToSelectList(),
                AdditionalFee = _worldPayPaymentSettings.AdditionalFee,
                AdditionalFeePercentage = _worldPayPaymentSettings.AdditionalFeePercentage,
                EndPoint = _worldPayPaymentSettings.EndPoint,
                DeveloperId = _worldPayPaymentSettings.DeveloperId,
                DeveloperVersion = _worldPayPaymentSettings.DeveloperVersion
            };

            return View("~/Plugins/Payments.WorldPay/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //save settings
            _worldPayPaymentSettings.SecureNetID = model.SecureNetID;
            _worldPayPaymentSettings.SecureKey = model.SecureKey;
            _worldPayPaymentSettings.UseSandbox = model.UseSandbox;
            _worldPayPaymentSettings.TransactMode = (TransactMode)model.TransactModeId;
            _worldPayPaymentSettings.AdditionalFee = model.AdditionalFee;
            _worldPayPaymentSettings.AdditionalFeePercentage = model.AdditionalFeePercentage;
            _worldPayPaymentSettings.EndPoint = model.EndPoint;
            _worldPayPaymentSettings.DeveloperId = model.DeveloperId;
            _worldPayPaymentSettings.DeveloperVersion = model.DeveloperVersion;

            _settingService.SaveSetting(_worldPayPaymentSettings);

            //clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PaymentInfo()
        {
            var model = new PaymentInfoModel();

            //years
            for (var i = 0; i < 15; i++)
            {
                var year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem
                {
                    Text = year,
                    Value = year
                });
            }

            //months
            for (var i = 1; i <= 12; i++)
            {
                var text = i < 10 ? "0" + i : i.ToString();
                model.ExpireMonths.Add(new SelectListItem
                {
                    Text = text,
                    Value = i.ToString(),
                });
            }

            //set postback values
            var form = this.Request.Form;
            model.CardholderName = form["CardholderName"];
            model.CardNumber = form["CardNumber"];
            model.CardCode = form["CardCode"];
            var selectedMonth = model.ExpireMonths.FirstOrDefault(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase));

            if (selectedMonth != null)
                selectedMonth.Selected = true;

            var selectedYear = model.ExpireYears.FirstOrDefault(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase));

            if (selectedYear != null)
                selectedYear.Selected = true;

            return View("~/Plugins/Payments.WorldPay/Views/PaymentInfo.cshtml", model);
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };

            var validationResult = validator.Validate(model);

            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest
            {
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"]
            };

            return paymentInfo;
        }
    }
}
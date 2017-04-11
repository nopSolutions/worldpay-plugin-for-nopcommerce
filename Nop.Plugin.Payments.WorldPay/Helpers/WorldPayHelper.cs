using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Nop.Plugin.Payments.WorldPay.Domain;
using Nop.Services.Logging;

namespace Nop.Plugin.Payments.WorldPay.Helpers
{
    public static class WorldPayHelper
    {
        #region Utilities

        private static string GetServiceUrl(WorldPayPaymentSettings worldPayPaymentSettings)
        {
            return worldPayPaymentSettings.UseSandbox
                ? "https://gwapi.demo.securenet.com/api/"
                : worldPayPaymentSettings.EndPoint;
        }

        #endregion

        #region Methods

        public static PaymentResponse PostRequest(WorldPayPaymentSettings worldPayPaymentSettings, PaymentRequest paymentRequest, ILogger logger)
        {
            var url = string.Format("{0}{1}", GetServiceUrl(worldPayPaymentSettings), paymentRequest.Command);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            request.Accept = "application/json";

            //set authentication
            var login = string.Format("{0}:{1}", worldPayPaymentSettings.SecureNetID, worldPayPaymentSettings.SecureKey);
            var authorization = Convert.ToBase64String(Encoding.UTF8.GetBytes(login));
            request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Basic {0}", authorization));
            
            paymentRequest.DeveloperApplication = new DeveloperApplication
            {
                DeveloperId = worldPayPaymentSettings.DeveloperId,
                Version = worldPayPaymentSettings.DeveloperVersion
            };

            //set post data
            var postData = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(paymentRequest));
            request.ContentLength = postData.Length;

            //post request
            try
            {
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(postData, 0, postData.Length);
                }

                //get response
                var httpResponse = (HttpWebResponse)request.GetResponse();
               
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    return JsonConvert.DeserializeObject<PaymentResponse>(streamReader.ReadToEnd());
                }
            }
            catch (WebException ex)
            {
                try
                {
                    var httpResponse = (HttpWebResponse)ex.Response;
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        //log errors
                        var response = streamReader.ReadToEnd();
                        logger.Error(string.Format("worldPay error: {0}", response), ex);

                        return null;
                    }
                }
                catch (Exception exc)
                {
                    logger.Error("worldPay error", exc);
                    return null;
                }
            }
            catch (Exception exc)
            {
                logger.Error("worldPay error", exc);
                return null;
            }
        }

        #endregion
    }
}

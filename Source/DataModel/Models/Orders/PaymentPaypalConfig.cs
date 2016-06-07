using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using System.Data;

namespace PhotoBookmart.DataLayer.Models.Products
{
    [Schema("Products")]
    public partial class PayPalStandardPaymentSettings : BasicModelBase
    {
        public bool UseSandbox { get; set; }
        public string BusinessEmail { get; set; }
        public string PdtToken { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
        public bool PassProductNamesAndTotals { get; set; }
        public bool PdtValidateOrderTotal { get; set; }
        public bool EnableIpn { get; set; }
        public string IpnUrl { get; set; }
        /// <summary>
        /// Enable if a customer should be redirected to the order details page
        /// when he clicks "return to store" link on PayPal site
        /// WITHOUT completing a payment
        /// </summary>
        public bool ReturnFromPayPalWithoutPaymentRedirectsToOrderDetailsPage { get; set; }

        /// <summary>
        /// Currency to send to Paypal according to Merchant paypal account
        /// </summary>
        public string InvoiceCurrency { get; set; }

        /// <summary>
        /// IPay 88 merchant code
        /// </summary>
        public string iPay88_MerchantCode { get; set; }

        /// <summary>
        /// IPay 88 merchant key
        /// </summary>
        public string iPay88_MerchantKey { get; set; }

        /// <summary>
        /// Get a settings in DB config
        /// </summary>
        /// <returns></returns>
        public static PayPalStandardPaymentSettings getSetting()
        {
            var db = ModelBase.ServiceAppHost.TryResolve<IDbConnection>();
            if (db.State != ConnectionState.Open)
            {
                db = ModelBase.ServiceAppHost.TryResolve<IDbConnectionFactory>().Open();
            }

            var s = db.Select<PayPalStandardPaymentSettings>().FirstOrDefault();
            if (s == null)
            {
                s = new PayPalStandardPaymentSettings()
                {
                    UseSandbox = true,
                    BusinessEmail = "test@test.com",
                    PdtToken = "Your PDT token here...",
                    PdtValidateOrderTotal = true,
                    EnableIpn = true,
                };
                db.Insert<PayPalStandardPaymentSettings>(s);
            }

            db.Close();
            return s;
        }
    }
}

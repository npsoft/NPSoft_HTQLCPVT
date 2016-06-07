using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;
using PhotoBookmart.DataLayer.Models.Products;
using System.Text;
using System.Net;
using System.IO;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.Lib;
using ServiceStack.OrmLite;

namespace PhotoBookmart.Support.Payment
{
    public class PaymentPaypalLib : BaseLib
    {
        PayPalStandardPaymentSettings _paypalStandardPaymentSettings = null;

        public PaymentPaypalLib()
        {
            if (_paypalStandardPaymentSettings == null)
            {
                _paypalStandardPaymentSettings = PayPalStandardPaymentSettings.getSetting();
            }
        }

        #region Methods

        /// <summary>
        /// Gets Paypal URL
        /// </summary>
        /// <returns></returns>
        public string GetPaypalUrl()
        {
            return PayPalStandardPaymentSettings.getSetting().UseSandbox ? "https://www.sandbox.paypal.com/us/cgi-bin/webscr" :
                "https://www.paypal.com/us/cgi-bin/webscr";
        }

        string CurrentWebsiteDomainURL
        {
            get
            {
                var request = HttpContext.Current.Request;
                var ret = request.Url.AbsoluteUri.Substring(0, request.Url.AbsoluteUri.Length - request.Url.PathAndQuery.Length + 1);
                return ret;
            }
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public string PostProcessPayment(Order order)
        {
            // get order data first
            order.LoadAddress(0);
            order.LoadAddress(1);
            order.LoadProductInfo();

            var country = Db.Select<Country>(x => x.Where(m => m.CurrencyCode == order.Shipping_DisplayPriceSign).Limit(1)).FirstOrDefault();
            var currentcy_sign = "";
            if (country != null)
            {
                currentcy_sign = country.Currency3Letter;
            }

            var builder = new StringBuilder();
            builder.Append(GetPaypalUrl());
            string cmd = string.Empty;
            if (_paypalStandardPaymentSettings.PassProductNamesAndTotals)
            {
                cmd = "_cart";
            }
            else
            {
                cmd = "_xclick";
            }

            double paypal_fee_amount = 0;
            if (_paypalStandardPaymentSettings.AdditionalFee > 0)
            {
                paypal_fee_amount = (double)_paypalStandardPaymentSettings.AdditionalFee;
                if (_paypalStandardPaymentSettings.AdditionalFeePercentage)
                {
                    paypal_fee_amount = (double)_paypalStandardPaymentSettings.AdditionalFee * order.Bill_Total / 100;
                }
            }

            builder.AppendFormat("?cmd={0}&business={1}", cmd, HttpUtility.UrlEncode(_paypalStandardPaymentSettings.BusinessEmail));
            if (_paypalStandardPaymentSettings.PassProductNamesAndTotals)
            {
                builder.AppendFormat("&upload=1");

                //get the items in the cart
                //decimal cartTotal = decimal.Zero;
                // pass the product name first

                int x = 1;


                //round
                builder.AppendFormat("&item_name_" + x + "={0}", HttpUtility.UrlEncode(order.ProductModel.Name));
                builder.AppendFormat("&amount_" + x + "={0}", order.Product_Price.ToString("0.##"));
                builder.AppendFormat("&quantity_" + x + "={0}", 1 * order.Quantity);
                x++;

                foreach (var item in order.Product_OptionsModel)
                {
                    builder.AppendFormat("&item_name_" + x + "={0}", HttpUtility.UrlEncode(item.Option_Name));
                    builder.AppendFormat("&amount_" + x + "={0}", item.Price.ToString("0.##"));
                    builder.AppendFormat("&quantity_" + x + "={0}", item.Option_Quantity * order.Quantity);
                    x++;
                }

                //order totals
                double cardTotal = order.Bill_Total;
                //payment method additional fee
                if (_paypalStandardPaymentSettings.AdditionalFee > 0)
                {
                    builder.AppendFormat("&item_name_" + x + "={0}", "Payment method fee");
                    builder.AppendFormat("&amount_" + x + "={0}", paypal_fee_amount.ToString("0.00"));
                    builder.AppendFormat("&quantity_" + x + "={0}", 1 * order.Quantity);
                    x++;
                }

                // shipping
                builder.AppendFormat("&item_name_" + x + "={0}", "Shipping");
                builder.AppendFormat("&amount_" + x + "={0}", order.Shipping_RealPrice);
                builder.AppendFormat("&quantity_" + x + "={0}", 1 * order.Quantity);
                x++;

                // discount
                if (order.isUseCoupon && order.Coupon_TotalDiscount > 0)
                {
                    builder.AppendFormat("&discount_amount_cart={0}", order.Coupon_TotalDiscount.ToString("0.00"));
                }
            }
            else
            {
                //pass order total
                builder.AppendFormat("&item_name=Order Number {0}", order.Order_Number);
                var orderTotal = order.Bill_Total + paypal_fee_amount;
                builder.AppendFormat("&amount={0}", orderTotal.ToString("0.00"));
            }

            builder.AppendFormat("&custom={0}", order.Order_Number);
            builder.AppendFormat("&charset={0}", "utf-8");
            // we support malaysia ringit only
            //builder.Append(string.Format("&no_note=1&currency_code={0}", HttpUtility.UrlEncode(_paypalStandardPaymentSettings.InvoiceCurrency)));
            builder.Append(string.Format("&no_note=1&currency_code={0}", HttpUtility.UrlEncode(currentcy_sign)));
            builder.AppendFormat("&invoice={0}", order.Order_Number);
            // builder.AppendFormat("&invoice={0}", new Random().Next(100,9999));
            builder.AppendFormat("&rm=2", new object[0]);
            /// temporary set no shipping
            builder.AppendFormat("&no_shipping=1", new object[0]);
            //if (postProcessPaymentRequest.Order.ShippingStatus != ShippingStatus.ShippingNotRequired)
            //    builder.AppendFormat("&no_shipping=2", new object[0]);
            //else
            //    builder.AppendFormat("&no_shipping=1", new object[0]);
            //var website_host = ConfigurationManager.AppSettings.Get("PaypalWebsiteURL");
            var website_host = CurrentWebsiteDomainURL;
            string returnUrl = website_host + "Paypal/PDTHandler";
            string cancelReturnUrl = website_host + "Paypal/CancelOrder";
            builder.AppendFormat("&return={0}&cancel_return={1}", HttpUtility.UrlEncode(returnUrl), HttpUtility.UrlEncode(cancelReturnUrl));

            //Instant Payment Notification (server to server message)
            if (_paypalStandardPaymentSettings.EnableIpn)
            {
                string ipnUrl;
                if (String.IsNullOrWhiteSpace(_paypalStandardPaymentSettings.IpnUrl))
                    ipnUrl = website_host + "Paypal/IPNHandler";
                else
                    ipnUrl = _paypalStandardPaymentSettings.IpnUrl;
                builder.AppendFormat("&notify_url={0}", ipnUrl);
            }

            //address
            builder.AppendFormat("&address_override=1");
            builder.AppendFormat("&first_name={0}", HttpUtility.UrlEncode(order.BillingAddressModel.FirstName));
            builder.AppendFormat("&last_name={0}", HttpUtility.UrlEncode(order.BillingAddressModel.LastName));
            builder.AppendFormat("&address1={0}", HttpUtility.UrlEncode(order.BillingAddressModel.Address));
            builder.AppendFormat("&address2={0}", HttpUtility.UrlEncode(""));
            builder.AppendFormat("&city={0}", HttpUtility.UrlEncode(order.BillingAddressModel.City));
            //if (!String.IsNullOrEmpty(postProcessPaymentRequest.Order.BillingAddress.PhoneNumber))
            //{
            //    //strip out all non-digit characters from phone number;
            //    string billingPhoneNumber = System.Text.RegularExpressions.Regex.Replace(postProcessPaymentRequest.Order.BillingAddress.PhoneNumber, @"\D", string.Empty);
            //    if (billingPhoneNumber.Length >= 10)
            //    {
            //        builder.AppendFormat("&night_phone_a={0}", HttpUtility.UrlEncode(billingPhoneNumber.Substring(0, 3)));
            //        builder.AppendFormat("&night_phone_b={0}", HttpUtility.UrlEncode(billingPhoneNumber.Substring(3, 3)));
            //        builder.AppendFormat("&night_phone_c={0}", HttpUtility.UrlEncode(billingPhoneNumber.Substring(6, 4)));
            //    }
            //}
            builder.AppendFormat("&state={0}", HttpUtility.UrlEncode(order.BillingAddressModel.City));
            //if (postProcessPaymentRequest.Order.BillingAddress.StateProvince != null)
            //    builder.AppendFormat("&state={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.StateProvince.Abbreviation));
            //else
            //    builder.AppendFormat("&state={0}", "");

            builder.AppendFormat("&country={0}", HttpUtility.UrlEncode(order.BillingAddressModel.Country));
            //if (postProcessPaymentRequest.Order.BillingAddress.Country != null)
            //    builder.AppendFormat("&country={0}", HttpUtility.UrlEncode(postProcessPaymentRequest.Order.BillingAddress.Country.TwoLetterIsoCode));
            //else
            //    builder.AppendFormat("&country={0}", "");
            builder.AppendFormat("&zip={0}", HttpUtility.UrlEncode(order.BillingAddressModel.ZipPostalCode));
            builder.AppendFormat("&email={0}", HttpUtility.UrlEncode(order.BillingAddressModel.Email));
            //_httpContext.Response.Redirect(builder.ToString());
            return builder.ToString();
        }

        #endregion

        #region Utilities



        /// <summary>
        /// Gets PDT details
        /// </summary>
        /// <param name="tx">TX</param>
        /// <param name="values">Values</param>
        /// <param name="response">Response</param>
        /// <returns>Result</returns>
        public bool GetPDTDetails(string tx, out Dictionary<string, string> values, out string response)
        {
            var req = (HttpWebRequest)WebRequest.Create(GetPaypalUrl());
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            string formContent = string.Format("cmd=_notify-synch&at={0}&tx={1}", _paypalStandardPaymentSettings.PdtToken, tx);
            req.ContentLength = formContent.Length;

            using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
                sw.Write(formContent);

            response = null;
            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
                response = HttpUtility.UrlDecode(sr.ReadToEnd());

            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool firstLine = true, success = false;
            foreach (string l in response.Split('\n'))
            {
                string line = l.Trim();
                if (firstLine)
                {
                    success = line.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
                    firstLine = false;
                }
                else
                {
                    int equalPox = line.IndexOf('=');
                    if (equalPox >= 0)
                        values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
                }
            }

            return success;
        }

        /// <summary>
        /// Verifies IPN
        /// </summary>
        /// <param name="formString">Form string</param>
        /// <param name="values">Values</param>
        /// <returns>Result</returns>
        public bool VerifyIPN(string formString, out Dictionary<string, string> values)
        {
            var req = (HttpWebRequest)WebRequest.Create(GetPaypalUrl());
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            string formContent = string.Format("{0}&cmd=_notify-validate", formString);
            req.ContentLength = formContent.Length;

            using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
            {
                sw.Write(formContent);
            }

            string response = null;
            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
            {
                response = HttpUtility.UrlDecode(sr.ReadToEnd());
            }
            bool success = response.Trim().Equals("VERIFIED", StringComparison.OrdinalIgnoreCase);

            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string l in formString.Split('&'))
            {
                string line = l.Trim();
                int equalPox = line.IndexOf('=');
                if (equalPox >= 0)
                    values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
            }

            return success;
        }

        /// <summary>
        /// Gets a payment status
        /// </summary>
        /// <param name="paymentStatus">PayPal payment status</param>
        /// <param name="pendingReason">PayPal pending reason</param>
        /// <returns>Payment status</returns>
        public Enum_PaymentStatus GetPaymentStatus(string paymentStatus, string pendingReason)
        {
            var result = Enum_PaymentStatus.Pending;

            if (paymentStatus == null)
                paymentStatus = string.Empty;

            if (pendingReason == null)
                pendingReason = string.Empty;

            switch (paymentStatus.ToLowerInvariant())
            {
                case "pending":
                    switch (pendingReason.ToLowerInvariant())
                    {
                        case "authorization":
                            result = Enum_PaymentStatus.Authorized;
                            break;
                        default:
                            result = Enum_PaymentStatus.Pending;
                            break;
                    }
                    break;
                case "processed":
                case "completed":
                case "canceled_reversal":
                    result = Enum_PaymentStatus.Paid;
                    break;
                case "denied":
                case "expired":
                case "failed":
                case "voided":
                    result = Enum_PaymentStatus.Voided;
                    break;
                case "refunded":
                case "reversed":
                    result = Enum_PaymentStatus.Refunded;
                    break;
                default:
                    break;
            }
            return result;
        }
        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.Lib;
using ServiceStack.OrmLite;
using PhotoBookmart.DataLayer.Models.Users_Management;
using System.Configuration;
using System.Security.Cryptography;
using PhotoBookmart.DataLayer.Models.System;

namespace PhotoBookmart.Support.Payment
{

    /**
     * Integrate Ipay88 (Malaysia) payment gateway system.
     *
     * @author Leow Kah Thong <http://kahthong.com>
     * @copyright Leow Kah Thong 2012
     * @version 2.0
     * @Rewrite into .net by Trung Dang (trungdt@absoft.vn)
     * https://github.com/ktleow/ipay88
    //http://www.woothemes.com/products/ipay88-gateway/
    //http://www.ipay88.com/images/ePayment%20Technical%20Spec.pdf
    //https://www.ipay88.com/integration-faq.asp
     *  public string paymentUrl               = "https://www.mobile88.com/epayment/entry.asp";
        public string requeryUrl               = "https://www.mobile88.com/epayment/enquiry.asp";
        public string refererHost              = "www.mobile88.com";  // Without scheme (http/https).
        public string recurringUrlSubscription = "https://www.ipay88.com/recurringpayment/webservice/RecurringPayment.asmx/Subscription";
        public string recurringUrlTermination  = "https://www.ipay88.com/recurringpayment/webservice/RecurringPayment.asmx/Termination";
     * public static const string TRANSACTION_TYPE_PAYMENT                = "payment";
     */
    public class iPay88Helper : BaseLib
    {
        public static string entryURL = "https://www.mobile88.com/ePayment/entry.asp";
        const string requeryUrl = "https://www.mobile88.com/epayment/enquiry.asp";
        const string refererHost = "www.mobile88.com";  // Without scheme (http/https).
        const string TRANSACTION_TYPE_PAYMENT = "payment";

        /// <summary>
        /// Send transaction to iPay and wait for the response
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public iPay_RequestModel DoTransaction(Order item)
        {
            // get the config
            PayPalStandardPaymentSettings settings = PayPalStandardPaymentSettings.getSetting();

            //
            var p = Db.Select<Product>(x => x.Where(m => m.Id == item.Product_Id).Limit(1)).FirstOrDefault();
            var product_name = item.Product_Name;
            if (p != null)
            {
                product_name += " - " + p.Size;
            }
            p.Dispose();

            var u = Db.Select<ABUserAuth>(x => x.Where(m => m.Id == item.Customer_Id).Limit(1)).FirstOrDefault();
            var user_phone = "";
            if (u != null && !string.IsNullOrEmpty(u.Phone))
            {
                user_phone = u.Phone;
            }

            if (settings.UseSandbox)
            {
                item.Bill_Total = 1;
            }

            // 
            var ret_url = ConfigurationManager.AppSettings.Get("PaypalWebsiteURL");
            //if (ret_url.Contains("http://localhost"))
            //{
            //    ret_url = "http://www.YourBackendURL.com/";
            //}

            if (string.IsNullOrEmpty(item.Customer_Name))
            {
                item.Customer_Name = "Order #" + item.Order_Number;
            }

            var country = Db.Select<Country>(x => x.Where(m => m.CurrencyCode == item.Shipping_DisplayPriceSign).Limit(1)).FirstOrDefault();
            var currentcy_sign= "";
            if (country != null)
            {
                currentcy_sign = country.Currency3Letter;
            }
            string entryUrl = entryURL;
            var pay88Request = new iPay_RequestModel()
            {
                //MerchantKey = settings.iPay88_MerchantKey,
                MerchantCode = settings.iPay88_MerchantCode,
                Amount = item.Bill_Total.ToString("0.00"),
                Currency = currentcy_sign,
                RefNo = item.Order_Number,
                ProdDesc = product_name,
                UserName = item.Customer_Name,
                UserContact = user_phone,
                UserEmail = item.Customer_Email,
                ResponseURL = ret_url + "iPay88/Response",

                BackendURL = ret_url + "iPay88/Response",
                PaymentId = "",
                Remark = "Premier Photo Book Sdn Bhd",
                Signature = generate_SHA1key(item),
            };


            Db.Close();

            return pay88Request;
        }

        string generate_SHA1key(Order item)
        {
            // get the config
            PayPalStandardPaymentSettings settings = PayPalStandardPaymentSettings.getSetting();

            var Key = string.Format("{0}{1}{2}{3}{4}", settings.iPay88_MerchantKey, settings.iPay88_MerchantCode, item.Order_Number, item.Bill_Total.ToString("0.00").Replace(".", ""), "MYR");
            SHA1CryptoServiceProvider objSHA1 = new SHA1CryptoServiceProvider();

            objSHA1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Key.ToCharArray()));

            byte[] buffer = objSHA1.Hash;
            string HashValue = System.Convert.ToBase64String(buffer);

            return HashValue;
        }

        /// <summary>
        /// Generate response signarture to compare
        /// </summary>
        /// <param name="item"></param>
        /// <param name="PaymentId"></param>
        /// <returns></returns>
        public string generate_SHA1keyResponse(Order item, int PaymentId)
        {
            // get the config
            PayPalStandardPaymentSettings settings = PayPalStandardPaymentSettings.getSetting();

            var Key = string.Format("{0}{1}{2}{3}{4}{5}{6}", settings.iPay88_MerchantKey, settings.iPay88_MerchantCode, PaymentId, item.Order_Number, item.Bill_Total.ToString("0.00").Replace(".", ""), "MYR", "1");
            SHA1CryptoServiceProvider objSHA1 = new SHA1CryptoServiceProvider();

            objSHA1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(Key.ToCharArray()));

            byte[] buffer = objSHA1.Hash;
            string HashValue = System.Convert.ToBase64String(buffer);

            return HashValue;
        }
    }
}
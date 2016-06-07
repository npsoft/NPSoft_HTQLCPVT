using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoBookmart.Support.Payment
{
    /// <summary>
    /// iPay RequestModel
    /// </summary>
    public class iPay_RequestModel
    {
        /// <summary>
        /// Merchant code assigned by iPay88. (varchar 20)
        /// </summary>
        public string MerchantCode { get; set; }

        /// <summary>
        /// Merchant Key
        /// </summary>
        public string MerchantKey { get; set; }
        /// <summary>
        /// (Optional) (int)
        /// </summary>
        public string PaymentId { get; set; }
        /// <summary>
        /// Unique merchant transaction number / Order ID (Retry for same RefNo only valid for 30 mins). (varchar 20)
        /// </summary>
        public string RefNo { get; set; }
        /// <summary>
        /// Payment amount with two decimals.
        /// </summary>
        public string Amount { get; set; }
        /// <summary>
        /// (varchar 5)
        /// </summary>
        public string Currency { get; set; }
        /// <summary>
        ///  Product description. (varchar 100)
        /// </summary>
        public string ProdDesc { get; set; }
        /// <summary>
        /// Customer name. (varchar 100)
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Customer email.  (varchar 100)
        /// </summary>
        public string UserEmail { get; set; }
        /// <summary>
        /// Customer contact.  (varchar 20)
        /// </summary>
        public string UserContact { get; set; }
        /// <summary>
        /// (Optional) Merchant remarks. (varchar 100)
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// (Optional) Encoding type:- ISO-8859-1 (English), UTF-8 (Unicode), GB2312 (Chinese Simplified), GD18030 (Chinese Simplified), BIG5 (Chinese Traditional)
        /// </summary>
        public string Lang { get { return "UTF-8"; } }
        /// <summary>
        /// SHA1 signature.
        /// </summary>
        public string Signature { get; set; }
        /// <summary>
        /// (Optional) Payment response page.
        /// </summary>
        public string ResponseURL { get; set; }

        public string BackendURL { get; set; }
    }

    /// <summary>
    /// Response Model
    /// </summary>
    public class iPay_ResponseModel
    {
        /// <summary>
        /// The Merchant Code provided by iPay88 and use  to uniquely identify the Merchant.
        /// </summary>
        public string MerchantCode { get; set; }

        /// <summary>
        /// Refer to Appendix I.pdf file for MYR gateway. Refer to Appendix II.pdf file for Multi-curency gateway.
        /// </summary>
        public int PaymentId { get; set; }

        /// <summary>
        /// Unique merchant transaction number / Order ID
        /// </summary>
        public string RefNo { get; set; }

        /// <summary>
        /// Payment amount with two decimals and  thousand symbols.Example: 1,278.99
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        /// Merchant remarks
        /// </summary>
        public string Remark { get; set; }

        /// <summary>
        /// iPay88 OPSG Transaction ID
        /// </summary>
        public string TransId { get; set; }
        /// <summary>
        /// Bank’s approval code
        /// </summary>
        public string AuthCode { get; set; }

        /// <summary>
        /// Payment status “1” – Success “0” – Fail
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Payment status description  =(Refer to Appendix I.pdf or Appendix II.pdf)
        /// </summary>
        public string ErrDesc { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Signature { get; set; }
    }
}
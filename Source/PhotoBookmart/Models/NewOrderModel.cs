using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using PhotoBookmart.DataLayer.Models.Products;

namespace PhotoBookmart.Models
{
    /// <summary>
    /// Product Option Model, combine from ProductOption and OptionInProduct
    /// </summary>
    public class ProductOptionModel : Product_Option
    {
        /// <summary>
        /// If isRequire then can not remove
        /// </summary>
        public bool isRequire { get; set; }

        /// <summary>
        /// Default quantity the admin set this option for the product
        /// </summary>
        public int DefaultQuantity { get; set; }

        public int MaxQuantity { get; set; }

        public int MinQuantity { get; set; }

        public bool CanApplyCoupon { get; set; }

        public List<Price> Price { get; set; }
    }

    /// <summary>
    /// To response with the Coupon validate request
    /// </summary>
    public class CouponValidateResponseModel
    {
        public bool Valid { get; set; }
        public double Discount { get; set; }
        /// <summary>
        /// VND, RM, ..
        /// </summary>
        //public string CurrencySign { get; set; }
        /// <summary>
        /// Discount in expected currency, get it from coupon
        /// </summary>
        public string Discount_In_Other_Currency { get; set; }

        public bool RequiredSecurityCode { get; set; }

        /// <summary>
        /// =0: Fix Amount
        /// =1: Percentage
        /// </summary>
        public int DiscountType { get; set; }
    }

    /// <summary>
    /// Model class to be submit in json when you submit orders
    /// </summary>
    public class OptionsSubmitModel
    {
        public long Option_Id { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Order Total returned after we calculate 
    /// </summary>
    public class OrderTotalModel
    {
        /// <summary>
        /// Grand total with ship included, no discount included
        /// </summary>
        public double Grand_Total { get; set; }
        /// <summary>
        /// 6% of Grand total
        /// </summary>
        public double GST { get; set; }
        /// <summary>
        /// Grand total + GST
        /// </summary>
        public double Total { get; set; }
        public bool isCalculatingSuccess { get; set; }
        /// <summary>
        /// Real shipping price in RM
        /// </summary>
        public double Shipping_RealPrice { get; set; }
        /// <summary>
        /// Display price in billing / shipping country currency
        /// </summary>
        public double Shipping_DisplayPrice { get; set; }
        /// <summary>
        /// Shipping price currency sign
        /// </summary>
        public string Shipping_DisplayPriceSign { get; set; }
    }

    /// <summary>
    /// Model to submit new Order
    /// </summary>
    public class NewOrderModel
    {
        /// <summary>
        /// This is the request code from the MyPhotoCreation capture function. We want to remove the cache after submit success
        /// </summary>
        public string RequestCode { get; set; }
        /// <summary>
        /// AppCode return from Windows APp
        /// </summary>
        public string PhotobookCode { set; get; }

        public long Product_Id { get; set; }

        /// <summary>
        /// Json string to be parsed. Format: [{option_id, quantity}]
        /// </summary>
        public string Options { get; set; }

        /// <summary>
        /// Options the system set when just open the new order page. For example, if we have 50 pages, then the 10 extra pages will be put into here
        /// </summary>
        public string Preset_Options { get; set; }

        public string CouponCode { get; set; }

        public string CouponSecrect { get; set; }

        public string OrderNote { get; set; }

        public Enum_PaymentMethod PaymentMethod { get; set; }

        public int Quantity { get; set; }

        public int Cover_Marterial { get; set; }

        #region For Billing Address
        public string Billing_Country { get; set; }

        public string Billing_FirstName { get; set; }

        public string Billing_LastName { get; set; }

        public string Billing_Address { get; set; }

        public string Billing_City { get; set; }

        public string Billing_Company { get; set; }

        public string Billing_ZipCode { get; set; }

        public string Billing_Email { get; set; }

        public string Billing_Phone { get; set; }

        public string Billing_State { get; set; }
        #endregion

        #region For Shipping Address
        public bool Shipping_IsDifferencewithBillingAddress { get; set; }

        public string Shipping_Country { get; set; }

        public string Shipping_FirstName { get; set; }

        public string Shipping_LastName { get; set; }

        public string Shipping_Address { get; set; }

        public string Shipping_City { get; set; }

        public string Shipping_Company { get; set; }

        public string Shipping_ZipCode { get; set; }

        public string Shipping_Email { get; set; }

        public string Shipping_Phone { get; set; }

        public Enum_ShippingType ShippingMethod { get; set; }
        #endregion
    }
}
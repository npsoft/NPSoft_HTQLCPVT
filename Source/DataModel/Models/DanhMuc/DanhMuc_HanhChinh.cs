using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace PhotoBookmart.DataLayer.Models.Products
{
    [Alias("DanhMuc_HanhChinh")]
    [Schema("DanhMuc")]
    public partial class DanhMuc_HanhChinh : BasicModelBase
    {
        public string MaHC { get; set; }
        public string TenHC { get; set; }
        public string TenDayDu { get; set; }
        public int? Nho { get; set; }

        public DanhMuc_HanhChinh() { }
    }

    public enum Enum_CouponType
    {
        /// <summary>
        /// This type will only need the CouponCode, no need to check the Secuirty Code
        /// </summary>
        [Display(Name="Promo")]
        Monthly_PromoCode = 0,
        /// <summary>
        /// This time we need to check both Coupon Code and Secuirty code
        /// </summary>
        [Display(Name = "Groupon")]
        Groupon = 1
    }

    [Alias("Coupon_Promo_Code")]
    [Schema("Products")]
    public partial class CouponPromo : BasicModelBase
    {

        /// <summary>
        /// Coupon Code
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// This field can be be add / update
        /// We only update this field incase this coupon has been used on the Front End. 
        /// Backend does not need to update
        /// Only groupon type need this field, for later we export this coupon code to Groupon to get money back
        /// </summary>
        public string SecurityCode { get; set; }

        public bool SecurityCodeRequired { get; set; }

        public DateTime IssueOn { get; set; }

        // To Groupon or to someone else
        public string IssueTo { get; set; }

        public int CouponType { get; set; }
        [Ignore]
        public Enum_CouponType CouponTypeEnum
        {
            get
            {
                return (Enum_CouponType)CouponType;
            }
            set
            {
                CouponType = (int)value;
            }
        }

        /// <summary>
        /// The begin date of the coupon
        /// </summary>
        public DateTime BeginDate { get; set; }

        /// <summary>
        /// the end date of the coupon
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// True: percent, false: fixed amount
        /// </summary>
        public bool isPercentDiscount { get; set; }

        /// <summary>
        /// True : to option
        /// False: to total product price except shipping
        /// </summary>
        public bool isApplyToOption { get; set; }

        /// <summary>
        /// Amount to discount (in RM or in percent)
        /// </summary>
        public double DiscountAmount { get; set; }

        /// <summary>
        /// Store the ID of all the options this coupon apply to
        /// </summary>
        public List<long> AppliedOptions { get; set; }

        /// <summary>
        /// Store the product id this coupon not apply to
        /// </summary>
        public List<long> ExceptProducts { get; set; }

        /// <summary>
        /// How many time maximum this coupon can be use
        /// </summary>
        public int MaxUse { get; set; }

        /// <summary>
        /// How many coupon has been used
        /// </summary>
        public int Used { get; set; }

        /// <summary>
        /// Which country this coupon will be apply to? 
        /// This field can be null, mean apply to all country.
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Date and time last used to redemp
        /// </summary>
        public DateTime LastUsed { get; set; }

        public CouponPromo() {
            CountryCode = "";
        }
    }
}

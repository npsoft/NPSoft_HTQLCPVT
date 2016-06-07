using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using PhotoBookmart.DataLayer.Models;
using ServiceStack.Mvc;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using ServiceStack.Text.Json;
using ServiceStack.Text;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.DataLayer.Models.Users_Management;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Sites;
using ServiceStack.Common.Web;
using PhotoBookmart.Models;
using ServiceStack.ServiceInterface;
using MvcReCaptcha;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.Support.Payment;
using PhotoBookmart.Lib;
using System.Configuration;
using PhotoBookmart.Support;
using System.IO;
using System.Text.RegularExpressions;
using PhotoBookmart.DataLayer.Models.ExtraShipping;

namespace PhotoBookmart.Controllers
{
    public class ProductController : BaseController
    {
        public ActionResult Index(string id)
        {
            return Redirect("/");
        }

        #region Widget
        [ChildActionOnly]
        public ActionResult Widget_BigBanner()
        {
            return View();
        }
        #endregion

        #region Error Report
        /// <summary>
        /// Receive error report from the javascript and insert into the Exception Log
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        [Authenticate]
        [HttpPost]
        public ActionResult ErrorReport(string error)
        {
            Response.AppendHeader("Access-Control-Allow-Origin", ConfigurationManager.AppSettings.Get("PaypalWebsiteURL"));
            if (string.IsNullOrEmpty(error))
            {
                return JsonError("There is no error message in your request");
            }

            var user = CurrentUser;
            Exceptions ex = new Exceptions()
            {
                ContextBrowserAgent = Request.UserAgent,
                ContextHttpMethod = "0",
                ContextUrl = Request.UrlReferrer.AbsoluteUri,
                ServerHost = Request.Url.Host,
                ExceptionOn = DateTime.Now,
                ExMessage = error,
                ExSource = "",
                ExStackTrace = "",
                UserId = user.Id,
                UserIp = InternalService.CurrentUserIP,
                UserName = user.UserName + " / " + user.FullName + " / " + user.Email
            };
            Db.Insert<Exceptions>(ex);
            // insert into email queue
            PhotoBookmart.Common.Helpers.SendEmail.SendMail("", "Photobookmart exception on " + DateTime.Now.ToString() + ": " + ex.EmailTitle, ex.EmailBody);

            return JsonSuccess("");
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Validate the quanitty amount entered by the customer with what we have in db
        /// </summary>
        /// <param name="option"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        int _ValidateSubmitOption(ProductOptionModel opt, int quantity)
        {
            if (quantity < 0)
            {
                quantity = 0;
            }

            if (opt.MaxQuantity != 0)
            {
                if (quantity > opt.MaxQuantity)
                {
                    quantity = opt.MaxQuantity;
                }

                if (quantity < opt.MinQuantity)
                {
                    quantity = opt.MinQuantity;
                }
            }

            if (opt.isRequire && quantity == 0)
            {
                quantity = 1; // can not remove
            }

            return quantity;
        }

        /// <summary>
        /// Get the specific price by the countrycode
        /// </summary>
        /// <param name="prices"></param>
        /// <param name="countrycode"></param>
        /// <returns></returns>
        Price _OptionGetPrice(List<Price> prices, string countrycode)
        {
            var price = prices.Where(x => x.CountryCode == countrycode);
            if (price == null || price.Count() == 0)
            {
                price = prices;
            }
            Price ret = null;
            if (price.Count() > 0)
            {
                if (string.IsNullOrEmpty(countrycode))
                {
                    // find by default currency
                    ret = price.Where(x => x.CurrencyCode == Setting_Defaultcurrency()).FirstOrDefault();
                    if (ret == null)
                    {
                        ret = price.FirstOrDefault();
                    }
                }
                else
                {
                    ret = price.FirstOrDefault();
                }
            }
            return ret;
        }
        /// <summary>
        /// Get the price of the options regarding to the country
        /// </summary>
        /// <param name="opt"></param>
        /// <returns></returns>
        Price _OptionGetPrice(ProductOptionModel opt, string countrycode)
        {
            return _OptionGetPrice(opt.Price, countrycode);
        }

        /// <summary>
        /// We calculate the order total
        /// </summary>
        /// <param name="product"></param>
        /// <param name="Options"></param>
        /// <returns></returns>
        OrderTotalModel _CalculateOrderTotal(Product product, List<OptionsSubmitModel> Options, string CountryCode, string States)
        {
            CountryCode = CurrentUser.Country;
            OrderTotalModel ret = new OrderTotalModel() { isCalculatingSuccess = false };
            try
            {
                var currenuser = CurrentUser;
                // get all options from db
                var pOptions = _GetProductOptions(product.Id);

                // compare with what we have in Options
                var p = product.getPrice(Enum_Price_MasterType.Product, currenuser.Country);
                double total = p.Value;

                ret.Shipping_DisplayPrice = 0;
                ret.Shipping_DisplayPriceSign = Setting_Defaultcurrency();
                ret.Shipping_RealPrice = 0;
                if (!product.isFreeShip)
                {
                    // calculate shipping price
                    //total += product.getPrice(Enum_Price_MasterType.ProductShippingPrice, CountryCode).RealPrice;

                    p = product.getPrice(Enum_Price_MasterType.ProductShippingPrice, currenuser.Country);
                    if (p != null && p.Id > 0)
                    {
                        // we found the price
                        ret.Shipping_RealPrice = p.Value;
                        ret.Shipping_DisplayPriceSign = p.CurrencyCode;
                        //ret.Shipping_DisplayPrice = p.DisplayPrice;
                        ret.Shipping_DisplayPrice = p.Value;

                        // check for extra shipping here
                        var country = Db.Select<Country>(x => x.Where(y => (y.Status && y.Code == CountryCode)).Limit(1)).FirstOrDefault();
                        if (country != null)
                        {
                            var extra_shipping = Db.Select<Country_State_ExtraShipping>(x => x.Where(y => y.CountryId == country.Id && y.State == States).Limit(1)).FirstOrDefault();
                            if (extra_shipping != null)
                            {
                                ret.Shipping_DisplayPrice += extra_shipping.Amount;
                                ret.Shipping_RealPrice += extra_shipping.Amount;
                            }
                        }

                    }
                }

                foreach (var opt in Options)
                {
                    // search it in pOptions to make sure we have it
                    var op_db = pOptions.Where(x => x.Id == opt.Option_Id).FirstOrDefault();
                    if (op_db == null)
                    {
                        continue;
                    }

                    // ok, check the quantity
                    var q = _ValidateSubmitOption(op_db, opt.Quantity);
                    var price = _OptionGetPrice(op_db, CountryCode);
                    if (price != null)
                    {
                        //total += q * price.RealPrice;
                        total += q * price.Value;
                    }
                }

                // ok we have grand total
                ret.Grand_Total = total + ret.Shipping_RealPrice;
                // gst
                ret.GST = ret.Grand_Total * 0.06d;
                // 
                ret.Total = ret.Grand_Total + ret.GST;
                //
                ret.isCalculatingSuccess = true;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while getting the product price. Country code =" + CountryCode + "; product = " + product.ToJson() + "; options = " + Options.ToJson(), ex);
                ret.isCalculatingSuccess = false;
            }
            return ret;
        }

        /// <summary>
        /// Get Product Options combined with OptionINProduct
        /// </summary>
        /// <param name="id">Product Id</param>
        /// <returns></returns>
        List<ProductOptionModel> _GetProductOptions(long id)
        {
            JoinSqlBuilder<Product_Option, Product_Option> jn = new JoinSqlBuilder<Product_Option, Product_Option>();
            jn = jn.Join<Product_Option, OptionInProduct>(m => m.Id, k => k.ProductOptionId);
            jn = jn.Where<OptionInProduct>(m => m.ProductId == id);
            jn = jn.Where<Product_Option>(m => m.Status);
            var sql = jn.ToSql();
            var options = Db.Select<Product_Option>(sql);

            if (options == null)
            {
                options = new List<Product_Option>();
            }

            List<ProductOptionModel> model = new List<ProductOptionModel>();
            if (options.Count > 0)
            {
                /// we get all option in product 
                var oinp = Db.Where<OptionInProduct>(x => x.ProductId == id);
                foreach (var x in options)
                {
                    ProductOptionModel p = x.TranslateTo<ProductOptionModel>();
                    // we combine with option in product inf
                    var opi = oinp.Where(k => k.ProductOptionId == x.Id).FirstOrDefault();
                    if (opi != null)
                    {
                        p.isRequire = opi.isRequire;
                        p.DefaultQuantity = opi.DefaultQuantity;
                        p.MaxQuantity = opi.MaxQuantity;
                        p.MinQuantity = opi.MinQuantity;
                        p.CanApplyCoupon = opi.CanApplyCoupon;

                        p.Price = Db.Where<Price>(k => k.MasterId == x.Id && k.MasterType == Enum_Price_MasterType.ProductOption);

                    }

                    //
                    model.Add(p);
                }
            }
            return model;
        }

        /// <summary>
        /// Calculate the discount for one box only
        /// </summary>
        /// <param name="coupon"></param>
        /// <param name="product"></param>
        /// <param name="options"></param>
        /// <param name="CountryCode"></param>
        /// <returns></returns>
        double _CouponDiscount_Calculation(CouponPromo coupon, Product product, List<OptionsSubmitModel> options, string CountryCode)
        {
            CountryCode = CurrentUser.Country;
            double ret = 0;

            // check country
            if (coupon.CouponTypeEnum == Enum_CouponType.Groupon && coupon.CountryCode != CountryCode)
            {
                // difference country code? then can not be apply
                return 0;
            }

            if (coupon.AppliedOptions == null)
            {
                coupon.AppliedOptions = new List<long>();
            }
            if (coupon.ExceptProducts == null)
            {
                coupon.ExceptProducts = new List<long>();
            }

            // before calculate, we need to find the correct rate for this coupon
            // if this coupon is monthly promotion code, then we need to get the rate based on it country or current user profile country
            // if this couppn is groupon code, then we just get the rate = 1, mean no change because we always check correct country 
            Country rate = new Country { ExchangeRate = 1 };
            // according to Jason on Sept 10, 2014: no exchange rate apply
            //if (coupon.CouponTypeEnum == Enum_CouponType.Monthly_PromoCode)
            //{
            //    // get the current rate
            //    rate = Setting_GetExchangeRate();
            //}

            // if apply to option
            if (coupon.isApplyToOption)
            {
                // get all options from db
                var pOptions = _GetProductOptions(product.Id);

                // tổng khoản có thể discount
                double discounted_amount = coupon.DiscountAmount * rate.ExchangeRate;

                if (!coupon.isPercentDiscount)
                {
                    // exchange to this money
                    coupon.DiscountAmount = coupon.DiscountAmount * rate.ExchangeRate;
                }

                foreach (var opt in options)
                {
                    if (coupon.AppliedOptions.Contains(opt.Option_Id))
                    {
                        // search it in pOptions to make sure we have it
                        var op_db = pOptions.Where(x => x.Id == opt.Option_Id).FirstOrDefault();
                        if (op_db == null)
                        {
                            continue;
                        }

                        if (!op_db.CanApplyCoupon)
                        {
                            continue;
                        }

                        // ok, check the quantity
                        var q = _ValidateSubmitOption(op_db, opt.Quantity);
                        var price = _OptionGetPrice(op_db, CountryCode);
                        var option_total = price.Value * q * rate.ExchangeRate;

                        //
                        if (coupon.isPercentDiscount)
                        {
                            ret += option_total * coupon.DiscountAmount / 100d;
                        }
                        else
                        {
                            if (discounted_amount > 0)
                            {
                                // lượng giảm giá còn có thể giảm tiếp option_total?
                                if (discounted_amount >= option_total)
                                {
                                    ret += option_total;
                                    discounted_amount -= option_total;
                                }
                                else
                                {
                                    ret += discounted_amount;
                                    discounted_amount = 0;
                                }
                            }
                        }
                    }
                }

                // include the base price
                if (coupon.isPercentDiscount || discounted_amount > 0)
                {
                    var p = product.getPrice(Enum_Price_MasterType.Product, CountryCode).Value;
                    if (coupon.isPercentDiscount)
                    {
                        ret += p * coupon.DiscountAmount / 100d;
                    }
                    else
                    {
                        if (discounted_amount > 0)
                        {
                            // lượng giảm giá còn có thể giảm tiếp option_total?
                            if (discounted_amount >= p)
                            {
                                ret += p;
                                discounted_amount -= p;
                            }
                            else
                            {
                                ret += discounted_amount;
                                discounted_amount = 0;
                            }
                        }
                    }
                }
            }
            else
            {
                // if this product is not in the ExceptProducts
                if (!coupon.ExceptProducts.Contains(product.Id))
                {
                    //var total = _CalculateOrderTotal(product, options, CurrentUser.Country);

                    //if (!product.isFreeShip)
                    //{
                    //    // subtract the shipping cost into the discount
                    //    total.Grand_Total -= total.Shipping_RealPrice;
                    //}

                    var p = product.getPrice(Enum_Price_MasterType.Product, CountryCode).Value;
                    //if (coupon.isPercentDiscount)
                    if (coupon.CouponTypeEnum == Enum_CouponType.Monthly_PromoCode) // monthly promotion code always percentage
                    {
                        //ret += total.Grand_Total * coupon.DiscountAmount / 100d;
                        ret += p * coupon.DiscountAmount / 100d;
                    }
                    else
                    { // groupon always fix amount
                        if (p > coupon.DiscountAmount)
                        {
                            //ret += total.Grand_Total - coupon.DiscountAmount;
                            ret += coupon.DiscountAmount;
                        }
                        else
                        {
                            ret = p;
                        }
                    }

                    //// and exchange ret to real money
                    //ret = ret * rate.ExchangeRate;
                }
            }

            return ret;
        }

        bool _PhotoBook_ProcessPhotobookFolder(string photobook_code, bool rename = false, string new_name = "")
        {
            var path = Settings.Get("Enum_Settings_Key.SERVER_DATA_FTP_LOCATION", System.IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }
            try
            {
                if (System.IO.Directory.Exists(path + photobook_code))
                {
                    if (rename)
                    {
                        // in old system we rename, but in new sytem we move it to WEBSITE_ORDERS_FOLDER_NOTYETPAID_PATH
                        var path_copy_to = Settings.Get(Enum_Settings_Key.WEBSITE_ORDERS_FOLDER_NOTYETPAID_PATH, System.IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
                        if (!path_copy_to.EndsWith("\\"))
                        {
                            path_copy_to += "\\";
                        }
                        path_copy_to += new_name;

                        // create new folder first
                        if (!System.IO.Directory.Exists(path_copy_to))
                        {
                            System.IO.Directory.CreateDirectory(path_copy_to);
                        }

                        var dir_info = new System.IO.DirectoryInfo(path + photobook_code);
                        foreach (var x in dir_info.GetFiles())
                        {
                            x.CopyTo(Path.Combine(path_copy_to, x.Name));
                        }

                        // everything ok? then we delete this folder
                        Directory.Delete(path + photobook_code, true);
                    }
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        #endregion

        #region New Order
        /// <summary>
        /// Get product information
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        [Authenticate]
        public ActionResult newOrder_getProductDetail(long id)
        {
            Product p = Db.Select<Product>(x => x.Where(m => m.Id == id && m.Status).Limit(1)).FirstOrDefault();
            if (p == null)
            {
                p = new Product();
            }
            else
            {
                // calculate the shipping
                p.ShippingPrice = Db.Where<Price>(k => k.MasterId == id && k.MasterType == Enum_Price_MasterType.ProductShippingPrice);
                // the product prices
                p.Price = p.getPrice(Enum_Price_MasterType.Product, CurrentUser.Country).Value;
            }
            return Content(p.ToJson());
        }

        /// <summary>
        /// Return all options of product
        /// </summary>
        /// <param name="id">Product Id</param>
        /// <returns></returns>
        [Authenticate]
        public ActionResult newOrder_getProductOptions(long id)
        {
            var model = _GetProductOptions(id);
            return Content(model.ToJson());
        }

        /// <summary>
        /// Validate the coupon
        /// </summary>
        /// <param name="CouponCode"></param>
        /// <param name="CouponSecrect"></param>
        /// <param name="Product_Id"></param>
        /// <param name="Options"></param>
        /// <param name="CountryCode"></param>
        /// <returns></returns>
        [Authenticate]
        [HttpPost]
        public ActionResult newOrder_CouponValidate(string CouponCode, long Product_Id, string Options, string CountryCode)
        {
            CountryCode = CurrentUser.Country;

            CouponValidateResponseModel ret = new CouponValidateResponseModel() { Discount = 0, Valid = false, RequiredSecurityCode = false };
            List<OptionsSubmitModel> Option_parsed = new List<OptionsSubmitModel>();

            // validate input data
            if (string.IsNullOrEmpty(CountryCode))
            {
                return Content(ret.ToJson());
            }
            var country = Db.Select<Country>(x => x.Where(m => m.Code == CountryCode).Limit(1)).FirstOrDefault();
            if (country == null)
            {
                return Content(ret.ToJson());
            }

            Product product = new Product();
            // desriallize the options first
            try
            {
                Option_parsed = Options.FromJson<List<OptionsSubmitModel>>();

                // get the product object to make sure everything is fine
                product = Db.Select<Product>(x => x.Where(m => m.Id == Product_Id && m.Status).Limit(1)).FirstOrDefault();
                if (product == null)
                {
                    // product is not found
                    return Content(ret.ToJson());
                }
            }
            catch
            {
                // we can not validate
                return Content(ret.ToJson());
            }

            var coupons = Db.Select<CouponPromo>(x => x.Where(m => m.BeginDate <= DateTime.Now && m.EndDate >= DateTime.Now &&
                // coupon type: groupon or promotion code
                (
                    (m.CouponType == (int)Enum_CouponType.Groupon && m.CountryCode == CountryCode) ||
                    (m.CouponType == (int)Enum_CouponType.Monthly_PromoCode)
                )
                && m.Code == CouponCode
                && m.Used < m.MaxUse
                ).Limit(1));

            if (coupons.Count > 0)
            {
                // found coupon
                var c = coupons.FirstOrDefault();

                // check to make sure this coupon can be apply to product or option
                if (c.isApplyToOption)
                {
                    ret.Valid = true;
                }
                else
                {
                    // apply to product?
                    if (!c.ExceptProducts.Contains(Product_Id))
                    {
                        ret.Valid = true;
                    }
                }

                ret.RequiredSecurityCode = c.SecurityCodeRequired;
                if (c.CouponTypeEnum == Enum_CouponType.Monthly_PromoCode)
                {
                    // percentage discount
                    ret.DiscountType = 1;
                }
                else
                {
                    // fix amount
                    ret.DiscountType = 0;
                }


                if (ret.Valid)
                {
                    // what next? oh, calculate the bill cost
                    // because when calculating discount we dont need to use Shipping (discount will not affect shipping)
                    // we use empty string for state
                    var total = _CalculateOrderTotal(product, Option_parsed, country.Code, "");
                    if (total.isCalculatingSuccess)
                    {
                        var discount = _CouponDiscount_Calculation(c, product, Option_parsed, country.Code);
                        var rate = Setting_GetExchangeRate();
                        // 
                        ret.Discount = discount;
                        //ret.CurrencySign = c.DiscountAmountDisplayCurrencySign;
                        // qui tắc tam suất
                        //double d = (discount / c.DiscountAmount * c.DiscountAmountDisplay);
                        ret.Discount_In_Other_Currency = discount.ToMoneyFormated(rate.CurrencyCode);
                    }
                }
            }

            // check copuon
            return Content(ret.ToJson());
        }

        static int testv = 0;
        [Authenticate]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SubmitOrder(NewOrderModel model)
        {
            string error_message = "";
            Country country = new Country();
            CouponPromo coupon = null;
            Product product = new Product();
            List<OptionsSubmitModel> options = new List<OptionsSubmitModel>();

            if (string.IsNullOrEmpty(model.Options))
            {
                error_message = "Sorry something went wrong with your order while processing.";
            }
            else
            {
                try
                {
                    options = model.Options.FromJson<List<OptionsSubmitModel>>();
                    if (options == null || options.Count == 0)
                    {
                        error_message = "Sorry something went wrong with your order while processing.";
                    }
                    else
                    {
                        // check to make sure all id and value is ok
                        options.ForEach(x =>
                        {
                            if (!(x.Option_Id > 0))
                            {
                                error_message = "Sorry something went wrong with your order while processing.";
                            }
                        });
                    }
                }
                catch (Exception ex)
                {
                    error_message = "Sorry something went wrong with your order while processing.";
                }
            }

            if (error_message != "")
            {
                // we got error, stop and redirect to newOrder page
                ViewBag.Error = error_message;
                return View("NewOrder", model);
            }

            #region Validate
            //if (!captchaValid)
            //{
            //    ViewBag.Error = "You have entered wrong captcha";

            //    ViewData["Cats"] = Db.Select<Product_Category>(x => x.Where(y => (y.Status)).OrderBy(z => (z.OrderIndex)));

            //    ViewData["Products"] = Db.Select<Product>(x => x.Where(y => (y.Status)).OrderBy(z => (z.Order)));

            //    return View("NewOrder", model);
            //}

            if (string.IsNullOrEmpty(model.PhotobookCode))
            {
                error_message += "PhotobookCode is missing <br/>";
            }
            else
            {
                // TODO: NEED CHANGE
                if (!_PhotoBook_ProcessPhotobookFolder(model.PhotobookCode))
                {
                    error_message += "Photobook folder is missing on our server. Please upload again<br/>";
                }
            }

            if (string.IsNullOrEmpty(model.Billing_FirstName))
            {
                error_message += "Billing First Name is missing <br/>";
            }

            if (string.IsNullOrEmpty(model.Billing_LastName))
            {
                error_message += "Billing Last Name is missing <br/>";
            }

            if (string.IsNullOrEmpty(model.Billing_Address))
            {
                error_message += "Billing Address is missing <br/>";
            }

            if (string.IsNullOrEmpty(model.Billing_City))
            {
                error_message += "Billing City is missing <br/>";
            }

            if (string.IsNullOrEmpty(model.Billing_ZipCode))
            {
                error_message += "Billing ZipCode is missing <br/>";
            }
            if (string.IsNullOrEmpty(model.Billing_Email) || !IsValidEmailAddress(model.Billing_Email))
            {
                error_message += "Billing Email is missing <br/>";
            }
            if (string.IsNullOrEmpty(model.Billing_Phone))
            {
                error_message += "Billing Phone is missing <br/>";
            }

            if (model.Shipping_IsDifferencewithBillingAddress)
            {
                if (string.IsNullOrEmpty(model.PhotobookCode))
                {
                    error_message += "PhotobookCode is missing <br/>";
                }

                if (string.IsNullOrEmpty(model.Shipping_FirstName))
                {
                    error_message += "Shipping First Name is missing <br/>";
                }

                if (string.IsNullOrEmpty(model.Shipping_LastName))
                {
                    error_message += "Shipping Last Name is missing <br/>";
                }

                if (string.IsNullOrEmpty(model.Shipping_Address))
                {
                    error_message += "Shipping Address is missing <br/>";
                }

                if (string.IsNullOrEmpty(model.Shipping_City))
                {
                    error_message += "Shipping City is missing <br/>";
                }

                if (string.IsNullOrEmpty(model.Shipping_ZipCode))
                {
                    error_message += "Shipping ZipCode is missing <br/>";
                }
                if (string.IsNullOrEmpty(model.Shipping_Email) || !IsValidEmailAddress(model.Shipping_Email))
                {
                    error_message += "Shipping Email is missing <br/>";
                }
                if (string.IsNullOrEmpty(model.Shipping_Phone))
                {
                    error_message += "Shipping Phone is missing <br/>";
                }
            }

            #endregion

            if (model.Quantity < 1)
            {
                model.Quantity = 1;
            }

            #region Validation - phase 2
            // continue validation?
            if (error_message != null)
            {
                // get the country by user profile country
                if (!string.IsNullOrEmpty(CurrentUser.Country))
                {
                    country = Db.Select<Country>(x => x.Where(m => m.Code == model.Billing_Country).Limit(1)).FirstOrDefault();
                }
                //if (country == null)
                //{
                //    country = Db.Select<Country>(x => x.Where(m => m.Code == model.Billing_Country).Limit(1)).FirstOrDefault();
                //}
                if (country == null)
                {
                    //error_message += "Sorry, something went wrong with your Billing Country <br />";
                    error_message += "Sorry, Please update your country in your Profile before place order. <br />";
                }
                else
                {
                    // desriallize the options first
                    try
                    {

                        // get the product object to make sure everything is fine
                        product = Db.Select<Product>(x => x.Where(m => m.Id == model.Product_Id && m.Status).Limit(1)).FirstOrDefault();
                        if (product == null)
                        {
                            error_message += "Selected product is not found <br />";
                        }

                        if (string.IsNullOrEmpty(model.CouponCode))
                        {
                            coupon = null;
                        }
                        else
                        {
                            //// search the coupon
                            var coupons = Db.Select<CouponPromo>(x => x.Where(m => m.BeginDate <= DateTime.Now && m.EndDate >= DateTime.Now &&
                m.Code == model.CouponCode
                && (
                (
                    (m.CouponType == (int)Enum_CouponType.Groupon && m.CountryCode == country.Code) ||
                    (m.CouponType == (int)Enum_CouponType.Monthly_PromoCode)
                )
                ) && m.Used < m.MaxUse).Limit(1));

                            if (coupons.Count > 0)
                            {
                                // found coupon
                                coupon = coupons.FirstOrDefault();
                                if (coupon.SecurityCodeRequired && string.IsNullOrEmpty(model.CouponSecrect))
                                {
                                    coupon = null;
                                }
                            }
                            else
                            {
                                coupon = null;
                            }
                        }
                    }
                    catch
                    {
                        error_message += "Sorry, something went wrong with your request. <br />";
                    }
                }
            }
            #endregion

            if (error_message != "")
            {
                // we got error, stop and redirect to newOrder page
                ViewBag.Error = error_message;

                return View("NewOrder", model);
            }


            // ok, we have all things we need, 
            // what next? oh, calculate the bill cost
            var total = _CalculateOrderTotal(product, options, country.Code, model.Billing_State);
            double discount = 0;
            if (total.isCalculatingSuccess)
            {
                if (coupon != null)
                {
                    // discount amount for one box
                    discount = _CouponDiscount_Calculation(coupon, product, options, country.Code);
                    //// No need to recalculate the discount for we will caclulate later with sub total
                    //total.Grand_Total -= discount; // it will affect the grand total because of the discount amoutn because the discount amount has been subtract the shipping cost
                    //total.GST = total.Grand_Total * 6 / 100;
                    //total.Total = total.Grand_Total + total.GST;

                    if (!coupon.SecurityCodeRequired)
                    {
                        model.CouponSecrect = "";
                    }
                }

                // add Address
                AddressModel billingaddress = new AddressModel();
                AddressModel shippingaddress = new AddressModel();
                billingaddress.Address = model.Billing_Address;
                billingaddress.City = model.Billing_City;
                billingaddress.Company = model.Billing_Company;
                billingaddress.Country = country.Code;
                billingaddress.CreatedOn = DateTime.Now;
                billingaddress.Email = model.Billing_Email;
                billingaddress.FaxNumber = "";
                billingaddress.FirstName = model.Billing_FirstName;
                billingaddress.LastName = model.Billing_LastName;
                billingaddress.PhoneNumber = model.Billing_Phone;
                billingaddress.State = model.Billing_State;
                billingaddress.ZipPostalCode = model.Billing_ZipCode;
                Db.Insert<AddressModel>(billingaddress);
                billingaddress.Id = Db.GetLastInsertId();

                // for shipping
                if (model.Shipping_IsDifferencewithBillingAddress)
                {
                    shippingaddress.Address = model.Shipping_Address;
                    shippingaddress.City = model.Shipping_City;
                    shippingaddress.Company = model.Shipping_Company;
                    shippingaddress.Country = model.Shipping_Country;
                    shippingaddress.CreatedOn = DateTime.Now;
                    shippingaddress.Email = model.Shipping_Email;
                    shippingaddress.FaxNumber = "";
                    shippingaddress.FirstName = model.Shipping_FirstName;
                    shippingaddress.LastName = model.Shipping_LastName;
                    shippingaddress.PhoneNumber = model.Shipping_Phone;
                    //shippingaddress.StateProvince = "";
                    shippingaddress.ZipPostalCode = model.Shipping_ZipCode;
                    Db.Insert<AddressModel>(shippingaddress);
                    shippingaddress.Id = Db.GetLastInsertId();
                }
                else
                {
                    // insert same address
                    shippingaddress = billingaddress.TranslateTo<AddressModel>();
                    shippingaddress.Id = 0;
                    Db.Insert<AddressModel>(shippingaddress);
                    shippingaddress.Id = Db.GetLastInsertId();
                }

                // insert order
                var currency = Setting_Defaultcurrency();

                Order o = new Order();
                o.LastUpdate = DateTime.Now;
                o.AppCode = model.PhotobookCode;
                // because of quantity, the total.Grand_Total is just for one product. So we set it to Subtotal
                o.Bill_SubTotal = total.Grand_Total;
                o.Quantity = model.Quantity;
                o.Bill_GrandTotal = o.Bill_SubTotal * model.Quantity;

                if (coupon != null)
                {
                    o.Coupon_Code = model.CouponCode;
                    o.Coupon_SecrectCode = model.CouponSecrect;

                    // the discount
                    if (coupon.CouponTypeEnum == Enum_CouponType.Monthly_PromoCode)
                    {
                        // monthly promotion code alway percentage, so we apply to all box
                        // apply to all box, each box apply the percent discount
                        o.Bill_GrandTotal -= discount * model.Quantity;
                        o.Coupon_TotalDiscount = discount * model.Quantity;
                    }
                    else
                    {
                        // for fix amount or the groupon code
                        // discount only one box
                        o.Bill_GrandTotal -= discount;
                        o.Coupon_TotalDiscount = discount;
                    }
                    o.CouponType = coupon.CouponType;
                }
                else
                {
                    o.Coupon_TotalDiscount = 0;
                    o.isUseCoupon = false;
                    o.CouponType = -1;
                    o.Coupon_Code = "";
                    o.Coupon_SecrectCode = "";
                }

                // use gst or not
                var use_gst = ((int)Settings.Get(Enum_Settings_Key.WEBSITE_GST_ENABLE, null, 0, Enum_Settings_DataType.Int)) == 1;
                if (use_gst)
                {
                    o.Bill_GST = o.Bill_GrandTotal * 0.06d;
                }
                else
                {
                    o.Bill_GST = 0;
                }
                o.Bill_Total = o.Bill_GrandTotal + o.Bill_GST;

                o.CoverMaterial = model.Cover_Marterial;
                o.CreatedOn = DateTime.Now;
                o.Customer_Email = billingaddress.Email;
                o.Customer_Name = billingaddress.FirstName + " " + billingaddress.LastName;
                if (User.Identity.IsAuthenticated)
                {
                    o.Customer_Id = CurrentUser.Id;
                    o.Customer_Username = CurrentUser.UserName;
                }
                else
                {
                    o.Customer_Username = "";
                }
                if (coupon != null)
                {
                    o.isUseCoupon = true;
                }

                var rate = Setting_GetExchangeRate();

                // first we get the product weight first
                o.TotalWeight = product.Weight;
                //var dc = discount * coupon.DiscountAmountDisplay / coupon.DiscountAmount;
                //o.Coupon_DiscountDisplay = dc.ToMoneyFormated(coupon.DiscountAmountDisplayCurrencySign);
                o.Coupon_DiscountDisplay = o.Coupon_TotalDiscount.ToMoneyFormated(rate.CurrencyCode);

                o.Order_Number = ""; // generate later
                o.PaymentMethod = model.PaymentMethod;
                o.PaymentStatus = (int)Enum_PaymentStatus.Pending;
                o.Product_Id = model.Product_Id;
                o.Product_Price = product.getPrice(Enum_Price_MasterType.Product, country.Code).Value;
                o.Product_Name = product.Name;
                o.Shipping_Method = Enum_ShippingType.Aramex;
                o.ShippingNote = model.OrderNote;
                o.StatusEnum = Enum_OrderStatus.Received;
                o.Payment_isPaid = false;
                o.Payment_BillingAddress = billingaddress.Id;
                if (model.Shipping_IsDifferencewithBillingAddress)
                {
                    o.Payment_BillingAddress_SameWith_Shipping = false;
                }
                else
                {
                    o.Payment_BillingAddress_SameWith_Shipping = true;
                }
                o.ShippingAddress = shippingaddress.Id;
                // Update the shipping price
                o.Shipping_DisplayPrice = total.Shipping_DisplayPrice;
                o.Shipping_DisplayPriceSign = total.Shipping_DisplayPriceSign;
                o.Shipping_RealPrice = total.Shipping_RealPrice;

                // insert order
                Db.Insert<Order>(o);
                o.Id = Db.GetLastInsertId();
                // now we calculate the order number
                o.Order_Number = o.Id.ToString("000000");

                Db.Update<Order>(o);

                // now insert into Order_ProductOptionUsing
                JoinSqlBuilder<Product_Option, Product_Option> jn = new JoinSqlBuilder<Product_Option, Product_Option>();
                jn = jn.Join<Product_Option, OptionInProduct>(m => m.Id, k => k.ProductOptionId);
                jn = jn.Where<OptionInProduct>(m => m.ProductId == o.Product_Id);
                jn = jn.Where<Product_Option>(m => m.Status);
                var sql = jn.ToSql();
                var db_opts = Db.Select<Product_Option>(sql);
                foreach (var opt in options)
                {
                    if (opt.Quantity == 0)
                    {
                        continue;
                    }
                    var db_op = db_opts.Where(x => x.Id == opt.Option_Id).FirstOrDefault();
                    if (db_op == null)
                    {
                        continue;
                    }

                    o.TotalWeight += db_op.Weight;

                    var prices = Db.Where<Price>(x => x.MasterId == opt.Option_Id && x.MasterType == Enum_Price_MasterType.ProductOption);
                    var price = _OptionGetPrice(prices, country.Code);

                    Order_ProductOptionUsing p = new Order_ProductOptionUsing();
                    p.Order_Id = o.Id;
                    p.Option_Quantity = opt.Quantity;
                    p.Option_Id = opt.Option_Id;
                    // Put the correct price
                    p.Price = price.Value;
                    p.PriceDisplay = price.Value;
                    p.PriceDisplaySign = price.CurrencyCode;
                    p.Option_Name = db_op.InternalName;
                    Db.Insert<Order_ProductOptionUsing>(p);
                }

                Db.Update<Order>(o);

                // increase coupon use

                if (o.isUseCoupon)
                {
                    var sec_code = "";
                    if (coupon.CouponTypeEnum == Enum_CouponType.Groupon && !string.IsNullOrEmpty(model.CouponSecrect))
                    {
                        sec_code = model.CouponSecrect;
                    }
                    Db.UpdateOnly<CouponPromo>(new CouponPromo() { Used = coupon.Used + 1, SecurityCode = sec_code, LastUsed = DateTime.Now }, ev => ev.Update(p => new { p.Used, p.SecurityCode, p.LastUsed }).Where(m => m.Id == coupon.Id).Limit(1));
                }

                // send email
                new OrderLib(o).Invoice_SendEmail();
                // sendmail to admin
                Invoice_SendEmailAdmin(o);

                // move the photobook code folder to the right location
                _PhotoBook_ProcessPhotobookFolder(o.AppCode, true, o.Order_Number);
                // remove the request code
                try
                {
                    Cache.Remove(model.RequestCode);
                }
                catch
                {
                }

                if (o.PaymentMethod == Enum_PaymentMethod.iPay88)
                {
                    return RedirectToAction("Payment_iPay88", new { id = o.Id });
                }
                else
                {
                    return RedirectToAction("Payment_Paypal", new { id = o.Id });
                }
                //return RedirectToAction("OrderInvoiceDetail", new { id = o.Order_Number });
            }
            else
            {
                ViewBag.Error = "Sorry, something went wrong when we calculate your order information";

                return View("NewOrder", model);
            }

            return View(model);
        }

        /// <summary>
        /// To load New Order page
        /// </summary>
        /// <returns></returns>
        [Authenticate]
        public ActionResult NewOrder(string request)
        {
            Response.AppendHeader("Access-Control-Allow-Origin", ConfigurationManager.AppSettings.Get("PaypalWebsiteURL"));
            // check user country code
            var country = Db.Select<Country>(x => x.Where(m => m.Code == CurrentUser.Country).Limit(1)).FirstOrDefault();
            if (country == null)
            {
                ViewBag.RedirectTo = Url.Action("Index", "Home");
                var contactus_url = Url.Action("Profile", "User");
                ViewBag.Message = string.Format("Sorry, please update your profile with correct country value before place order. Please <a href='{0}'>click here</a> to update your profile.", contactus_url);
                return View("Message");
            }

            NewOrderModel model = new NewOrderModel();

            if (string.IsNullOrEmpty(request))
            {
                return Redirect("/");
            }

            MyPhotoCreationRequest order_info = Cache.Get<MyPhotoCreationRequest>(request);
            // 
            if (order_info != null)
            {
                // now we calculate the options again
                var product = Db.Select<Product>(x => x.Where(m => m.Id == order_info.Product_Id).Limit(1)).FirstOrDefault();
                var extra_pages = order_info.Pages - product.Pages;
                var p_options = _GetProductOptions(product.Id);
                if (extra_pages > 0)
                {
                    var extra_field_name = ((string)Settings.Get(Enum_Settings_Key.WEBSITE_ADDITIONAL_PAGE_NAME, "Additional Page", Enum_Settings_DataType.String)).Trim();
                    foreach (var x in p_options)
                    {
                        if (x.Name.Contains(extra_field_name))
                        {
                            x.DefaultQuantity = extra_pages;
                        }
                    }

                }

                model.Product_Id = order_info.Product_Id;
                model.Preset_Options = p_options.ToJson();
                model.PhotobookCode = order_info.Photobook_Code;

                Cache.Set<MyPhotoCreationRequest>(request, order_info, TimeSpan.FromMinutes(50));

                ViewData["Products"] = Db.Select<Product>(x => x.Where(y => (y.Status)).OrderBy(z => (z.Order)));

                ViewData["RequestCode"] = request;

                return View("NewOrder", model);
            }
            else
            {
                return Redirect("/");
            }
        }


        /// <summary>
        /// Parse the pages and pid from the XML instead of getting it from the request string
        /// Return true if can parse, return false if can not
        /// </summary>
        /// <param name="job_id"></param>
        /// <param name="pages"></param>
        /// <param name="p_id"></param>
        bool MyPhotoCreation_ParsePagesAndPID(string job_id, out int pages, out int p_id)
        {
            var path = Settings.Get("Enum_Settings_Key.SERVER_DATA_FTP_LOCATION", System.IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
            if (!path.EndsWith("\\"))
            {
                path += "\\";
            }

            pages = 0;
            p_id = 0;

            try
            {
                if (System.IO.Directory.Exists(path + job_id) && System.IO.File.Exists(path + job_id + "\\pdf.xml"))
                {
                    var xml_content = System.IO.File.ReadAllText(path + job_id + "\\pdf.xml");
                    var matches = Regex.Split(xml_content, @"(?<=>)|(?=<)").ToList();

                    int found_count = 0;
                    for (var i = 0; i < matches.Count(); i++)
                    {
                        matches[i] = matches[i].Trim();
                        if (matches[i].IndexOf("<PagesInJob") == 0 && i < matches.Count - 1)
                        {
                            found_count++;
                            pages = int.Parse(matches[i + 1].Trim());
                            i++;
                        }
                        if (matches[i].IndexOf("<ProductID") == 0 && i < matches.Count - 1)
                        {
                            found_count++;
                            p_id = int.Parse(matches[i + 1].Trim());
                            i++;
                        }
                        if (found_count == 2)
                        { break; }
                    }
                    if (found_count == 2)
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
            return false;
        }

        /// <summary>
        /// Capture URL from My Photo Creation and pass to new order
        /// </summary>
        /// <param name="form"></param>
        /// <returns></returns>
        public ActionResult MyPhotoCreation(FormCollection form)
        {
            // sample url
            // /cart/getinfo.asp
            //http://www.photobookmart.com/cart/getinfo.asp?ig1=ig&job_id=PhotoBook_3363952057068890&path=PhotoBook_3363952057068890&fname=PDF&no_xml=true&job_channel_id=288&pid=2951&pgx=40&lang=en&p=0
            //http://zin.sendtoprint.net/helpconsole2010/CustomizationGuide/default.aspx?pageid=Tutorial_2
            /*
             * ig1=ig                                       Must exist; ignore
                job_id=Photobook_59045          Job ID, as defined above
                path=Photobook_59045                        Same as job_id
                fname=PDF                              Ignore
                no_xml=true                              Ignore
                job_channel_id=1                      May ignore (this is your numeric id)
                pid=71                                      Product ID
                pgx=32                                     Number of pages in a photobook order
                cor=ls or cor=pt                      Orientation for a cards or calendar order
             */

            var photobook_code = Request.QueryString.Get("job_id");
            //var p_id = Request.QueryString.Get("pid");
            //var spages = Request.QueryString.Get("pgx");

            int p_id = 0;
            int spages = 0;

            // check the photobook code
            // TODO: NEED CHANGE
            if (!MyPhotoCreation_ParsePagesAndPID(photobook_code, out spages, out p_id))
            {
                ViewBag.RedirectTo = Url.Action("Index", "Home");
                var contactus_url = Url.Action("Index", "ContactUs");
                ViewBag.Message = string.Format("Sorry, we can not recognize request from Photobookmart Application. Please <a href='{0}'>contact our Administrator</a>.", contactus_url);
                return View("Message");
            }

            // check additional page
            MyPhotoCreationRequest request = new MyPhotoCreationRequest();
            request.Photobook_Code = photobook_code;


            var product = Db.Select<Product>(x => x.Where(m => m.MyPhotoCreationId == p_id).Limit(1)).FirstOrDefault();
            if (product != null)
            {
                request.Product_Id = product.Id;
                // check the pages
                // find Additional Page
                request.Pages = spages;
                // redirect to new order
                var key = AuthService.GetSessionId() + "_" + request.Photobook_Code + "_" + p_id + "_" + spages;
                Cache.Set<MyPhotoCreationRequest>(key, request, TimeSpan.FromMinutes(15));
                return RedirectToAction("NewOrder", new { request = key });
            }

            return Redirect("/");
        }

        private void Invoice_SendEmailAdmin(Order order)
        {
            string subject = string.Format("New order from {0} - Amount {1}", order.Customer_Name, order.Bill_Total.ToMoneyFormated(order.Shipping_DisplayPriceSign));

            string body = RenderPartialViewToString("InvoiceDetail_Admin", order);

            SendEmail.SendMail(CurrentWebsite.Email_Admin, subject, body);
        }

        /// <summary>
        /// Do the payment process
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [Authenticate]
        public ActionResult Payment_Paypal(long id)
        {
            Response.AppendHeader("Access-Control-Allow-Origin", ConfigurationManager.AppSettings.Get("PaypalWebsiteURL"));
            Order ret = new Order();
            ret = Db.Select<Order>(x => x.Where(m => m.Id == id && m.Customer_Id == CurrentUser.Id).Limit(1)).FirstOrDefault();
            if (ret == null)
            {
                return Redirect("/");
            }
            var url = new PaymentPaypalLib().PostProcessPayment(ret);
            return View("Payment_Paypal", (object)url);
            //return Redirect(new PaymentPaypalLib().PostProcessPayment(ret));
            //return RedirectToAction("PostProcessPayment", "Paypal", new { order = ret });
        }

        /// <summary>
        /// Pay invoice with iPay88
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [Authenticate]
        public ActionResult Payment_iPay88(long id)
        {
            Response.AppendHeader("Access-Control-Allow-Origin", ConfigurationManager.AppSettings.Get("PaypalWebsiteURL"));
            Order ret = new Order();
            ret = Db.Select<Order>(x => x.Where(m => m.Id == id && m.Customer_Id == CurrentUser.Id).Limit(1)).FirstOrDefault();
            if (ret == null)
            {
                return Redirect("/");
            }
            //
            // we dont do anything, we only show the loading page
            return View(ret.Id);
            //return RedirectToAction("PostProcessPayment", "Paypal", new { order = ret });
        }

        /// <summary>
        /// Ajax request from client to receive the iPay88 Request Model
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [Authenticate]
        public ActionResult Payment_iPayRequest(int id)
        {
            Response.AppendHeader("Access-Control-Allow-Origin", ConfigurationManager.AppSettings.Get("PaypalWebsiteURL"));
            Order ret = new Order();
            ret = Db.Select<Order>(x => x.Where(m => m.Id == id && m.Customer_Id == CurrentUser.Id).Limit(1)).FirstOrDefault();
            if (ret == null)
            {
                return JsonError("Order " + id.ToString("00000") + " is not found");
            }
            else
            {
                iPay88AjaxResponseModel model = new iPay88AjaxResponseModel();
                model.Url = iPay88Helper.entryURL;
                model.Model = new iPay88Helper().DoTransaction(ret);

                // before we return, we update the signature key
                Db.UpdateOnly<Order>(new Order() { Payment_CaptureTransactionResult = model.Model.Signature }, ev => ev.Update(p => p.Payment_CaptureTransactionResult).Where(m => m.Id == ret.Id).Limit(1));


                return JsonSuccess("", model.ToJson());
            }
        }
        #endregion

        #region Invoice
        /// <summary>
        /// Show order detail by the order id (5digits)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authenticate]
        public ActionResult OrderInvoiceDetail(string id)
        {
            Order ret = new Order();
            var user = CurrentUser;
            if (user.HasRole(RoleEnum.Administrator) || user.HasRole(RoleEnum.OrderManagement))
            {
                ret = Db.Select<Order>(x => x.Where(m => m.Order_Number == id).Limit(1)).FirstOrDefault();
            }
            else
            {
                ret = Db.Select<Order>(x => x.Where(m => m.Order_Number == id && m.Customer_Id == CurrentUser.Id).Limit(1)).FirstOrDefault();
            }

            if (ret == null)
            {
                ret = new Order();
            }
            return View(ret);
        }

        /// <summary>
        /// Send invoice to email 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authenticate]
        [HttpGet]
        public ActionResult Invoice_SendEmail(string id)
        {
            Order ret = new Order();
            var user = CurrentUser;
            if (user.HasRole(RoleEnum.Administrator) || user.HasRole(RoleEnum.OrderManagement))
            {
                ret = Db.Select<Order>(x => x.Where(m => m.Order_Number == id).Limit(1)).FirstOrDefault();
            }
            else
            {
                ret = Db.Select<Order>(x => x.Where(m => m.Order_Number == id && m.Customer_Id == CurrentUser.Id).Limit(1)).FirstOrDefault();
            }


            if (ret != null && ret.Id > 0)
            {
                //string body = RenderPartialViewToString("InvoiceDetail", ret);
                //string title = string.Format("Invoice {0} - {1} - Photobookmart", ret.Order_Number, ret.CreatedOn.ToString("ddd, MMM d, yyyy"));

                //ret.LoadAddress(0);
                //base.SendMail(title, body, ret.BillingAddressModel.Email, "", "", new Dictionary<string, string>());
                new OrderLib(ret).Invoice_SendEmail();
                return JsonSuccess("", string.Format("Invoice {0} has been sent", ret.Order_Number));
            }
            else
            {
                return JsonError("Can not find your order");
            }
        }

        //[Authorize]
        //public ActionResult Invoice_DownloadPdf(string id)
        //{
        //    Order ret = new Order();
        //    var user = CurrentUser;
        //    if (user.HasRole(RoleEnum.Administrator) || user.HasRole(RoleEnum.OrderManagement))
        //    {
        //        ret = Db.Select<Order>(x => x.Where(m => m.Order_Number == id).Limit(1)).FirstOrDefault();
        //    }
        //    else
        //    {
        //        ret = Db.Select<Order>(x => x.Where(m => m.Order_Number == id && m.Customer_Id == CurrentUser.Id).Limit(1)).FirstOrDefault();
        //    }


        //    if (ret != null && ret.Id > 0)
        //    {
        //        //string body = RenderPartialViewToString("InvoiceDetail", ret);
        //        var orderlib = new OrderLib(ret);
        //        return orderlib.ExportToPdf();
        //    }
        //    else
        //    {
        //        return Redirect("/");
        //    }
        //}
        #endregion
    }
}
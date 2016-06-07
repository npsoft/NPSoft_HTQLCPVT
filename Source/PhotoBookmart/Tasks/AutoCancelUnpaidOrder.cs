using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ServiceStack.OrmLite;
using ServiceStack.Common;
using ServiceStack.ServiceClient.Web;
using PhotoBookmart.Common.Helpers;
using PhotoBookmart.DataLayer.Models.Products;
using PhotoBookmart.DataLayer.Models.Sites;
using PhotoBookmart.DataLayer.Models.System;
using PhotoBookmart.Models;
using PhotoBookmart.Support;
using ServiceStack.CacheAccess;
using System.Configuration;

namespace PhotoBookmart.Tasks
{
    public static class AutoCancelUnpaidOrder
    {
        static JsonServiceClient server_client;

        static string cache_key = "";

        static Thread thread;

        static IDbConnection _connection = null;
        static IDbConnection Db
        {
            get
            {
                if (_connection == null)
                {
                    _connection = AppHost.Resolve<IDbConnection>();
                }

                if (_connection != null && _connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                if (_connection != null && _connection.State != ConnectionState.Open)
                {
                    _connection = AppHost.Resolve<IDbConnectionFactory>().Open();
                }

                return _connection;
            }
        }

        static ICacheClient _cache = null;
        static ICacheClient Cache
        {
            get
            {
                if (_cache == null)
                {
                    _cache = AppHost.Resolve<ICacheClient>();
                }

                return _cache;
            }
        }

        #region Common Functions

        /// <summary>
        /// Based on Model, render template for send mail
        /// </summary>
        /// <param name="st"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        private static string SendMail_RenderBeforeSend_Regex(string st, Dictionary<string, string> model)
        {
            Regex re = new Regex(@"#[a-z_A-Z0-9]+");
            MatchCollection mc = re.Matches(st);
            int mIdx = 0;
            foreach (Match m in mc)
            {
                for (int gIdx = 0; gIdx < m.Groups.Count; gIdx++)
                {
                    var key = m.Groups[gIdx].Value.Substring(1); // remove first #\

                    // now search for the key in model
                    if (model.ContainsKey(key))
                    {
                        var value = model[key];
                        st = st.Replace("#" + key, value);
                    }
                }
                mIdx++;
            }
            return st;
        }

        /// <summary>
        /// Render input string by token replacement before send email
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        private static void SendMail_RenderBeforeSend(ref string body, ref string title, Dictionary<string, string> input = null)
        {
            // prepare the Dictionary
            if (input == null)
            {
                input = new Dictionary<string, string>();
            }
            var parameters = new Dictionary<string, string>();
            parameters.Add(EnumMaillingListTokens.current_date_time.ToString(), DateTime.Now.ToLongDateString());

            // merge 2 dictionary into one
            Dictionary<string, string> final_input = parameters.MergeLeft(input);
            //
            body = SendMail_RenderBeforeSend_Regex(body, final_input);
            title = SendMail_RenderBeforeSend_Regex(title, final_input);
            // for domain host url replace
            var domain_name = ConfigurationManager.AppSettings.Get("PaypalWebsiteURL");
            body = body.Replace("href=\"/", "href=\"" + domain_name);
            body = body.Replace("src=\"/", "src=\"" + domain_name);
            body = body.Replace("href='/", "href='" + domain_name);
            body = body.Replace("src='/", "src='" + domain_name);
        }

        /// <summary>
        /// Return best matched mailling list template 
        /// </summary>
        /// <param name="systemname"></param>
        /// <returns></returns>
        private static Site_MaillingListTemplate Get_MaillingListTemplate(string systemname)
        {
            var data = Db.Select<Site_MaillingListTemplate>(x => x.Where(m => m.Status && m.Systemname == systemname).Limit(1)).FirstOrDefault();
            Db.Close();
            return data;
        }

        static void InsertException(string message, Exception exp = null)
        {
            Exceptions ex = new Exceptions();
            ex.ExMessage = message;
            ex.ContextBrowserAgent = "Auto Cancel Unpaid Order";
            ex.ExceptionOn = DateTime.Now;
            if (exp != null)
            {
                ex.ExSource = exp.Source;
                ex.ExStackTrace = exp.StackTrace;
            }
            // insert into db
            Db.Insert<Exceptions>(ex);
            Db.Close();
        }

        #endregion

        public static void Start()
        {
            Random r = new Random();
            cache_key = r.NextDouble().ToString() + "_" + r.NextDouble().ToString();

            thread = new Thread(new ThreadStart(Execute));
            thread.Start();
        }

        static void Execute()
        {
            while (thread.IsAlive)
            {
                try
                {
                    int interval = (int)Settings.Get(Enum_Settings_Key.TASK_AUTO_CANCEL_ORDER_IF_MORETHAN_MINUTE, null, 5, Enum_Settings_DataType.Int);

                    Site_MaillingListTemplate template = Get_MaillingListTemplate("cancel_order_not_receive_payment");
                    if (template == null)
                    {
                        thread.Abort();
                        break;
                    }

                    var orders = Db.Select<Order>(x => x.Where(y => (
                        y.Status == (int)Enum_OrderStatus.Received &&
                        !y.Payment_isPaid &&
                        y.PaymentStatus != (int)Enum_PaymentStatus.Paid &&
                        DateTime.Now.Subtract(TimeSpan.FromMinutes(interval)) > y.CreatedOn)));

                    foreach (var order in orders)
                    {
                        // set status to cancel
                        order.Status = (int)Enum_OrderStatus.Canceled;

                        Db.UpdateOnly<Order>(order, ev => ev.Update(p => new
                        {
                            p.Status
                        }).Where(m => (m.Id == order.Id)).Limit(1));

                        order.AddHistory("Auto cancel order because of not receive payment", "System", 0, true);

                        #region Return The Coupon

                        if (order.isUseCoupon)
                        {
                            var coupon = Db.Select<CouponPromo>(x => x.Where(y => y.Code == order.Coupon_Code).Limit(1)).FirstOrDefault();

                            if (coupon != null)
                            {
                                Db.UpdateOnly<CouponPromo>(new CouponPromo() { Used = coupon.Used - 1 }, ev => ev.Update(p => new { p.Used }).Where(m => m.Id == coupon.Id).Limit(1));

                                order.AddHistory(string.Format("Update coupon {0}, set used count from {1} to {2} ", coupon.Code, coupon.Used, coupon.Used - 1), "System", 0, true);
                            }
                        }

                        #endregion

                        #region Delete Photobook File

                        if (!order.Order_Photobook_Deleted)
                        {
                            var path = "";

                            if (order.PaymentStatusEnum == Enum_PaymentStatus.Paid)
                            {
                                path = Settings.Get(Enum_Settings_Key.WEBSITE_UPLOAD_PATH_DEFAULT, System.IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
                            }
                            else
                            {
                                path = Settings.Get(Enum_Settings_Key.WEBSITE_ORDERS_FOLDER_NOTYETPAID_PATH, System.IO.Path.GetTempPath(), Enum_Settings_DataType.String).ToString();
                            }

                            path = System.IO.Path.Combine(path, order.Order_Number);

                            if (System.IO.Directory.Exists(path))
                            {
                                try
                                {
                                    System.IO.Directory.Delete(path, true);

                                    order.AddHistory("Deleted photobook folder in " + path, "System", 0, true);

                                    Db.UpdateOnly<Order>(new Order() { Order_Photobook_Deleted = true }, ev => ev.Update(p => new { p.Order_Photobook_Deleted }).Where(m => m.Id == order.Id).Limit(1));
                                }
                                catch (Exception ex)
                                {
                                    order.AddHistory("Error while deleting photobook folder: " + ex.Message, "System", 0, true);
                                }
                            }
                            else
                            {
                                order.AddHistory("Warning: System can not find photobook folder in " + path, "System", 0, true);
                            }
                        }

                        #endregion

                        #region Send Email For Customer

                        Dictionary<string, string> dic = order.GetPropertiesListOrValue(true, "");
                        order.LoadAddress(0);
                        order.LoadAddress(1);
                        order.LoadProductInfo();
                        var billing = order.BillingAddressModel.GetPropertiesListOrValue(true, "Billing_");
                        var shipping = order.BillingAddressModel.GetPropertiesListOrValue(true, "Shipping_");
                        dic = dic.MergeDictionary(billing);
                        dic = dic.MergeDictionary(shipping);

                        if (template != null)
                        {
                            var content = template.Body;
                            var title = template.Title;
                            SendMail_RenderBeforeSend(ref content, ref title, dic);

                            //order.Customer_Email
                            PhotoBookmart.Common.Helpers.SendEmail.SendMail(order.Customer_Email, title, content, "customerservice@photobookmart.com", "Photobookmart Customer Support");

                            order.AddHistory(string.Format("<b>Send message to customer</b>\r\nTitle: {0}\r\nContent:{1}", title, content), "System", 0, true);
                        }

                        #endregion
                    }
                    Db.Close();
                }
                catch (Exception ex)
                {
                    server_client = null;

                    if (ex.Message != "Thread was being aborted")
                    {
                        InsertException(ex.Message, ex);
                    }
                }

                Thread.Sleep(1000 * 60 * 5);
            }
        }
    }
}
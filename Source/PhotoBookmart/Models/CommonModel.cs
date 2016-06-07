using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using PhotoBookmart.Support.Payment;

namespace PhotoBookmart.Models
{
    public class MinuteTimeModel
    {
        public int Hour { get; set; }
        public int Minute { get; set; }

        public static MinuteTimeModel MinuteTimeToTime(int minutetime)
        {
            var x = new MinuteTimeModel();
            x.Hour = minutetime / 60;
            x.Minute = minutetime % 60;
            return x;
        }
    }

    /// <summary>
    ///  Pagination model for automation pagination
    /// </summary>
    public class PaginationModel
    {
        public int pages { get; set; }
        public int page { get; set; }
        public string controller { get; set; }
        public string action { get; set; }
        public RouteValueDictionary route { get; set; }
        /// <summary>
        /// Items per page
        /// </summary>
        public int per_page { get; set; }
        public int total_items { get; set; }
    }

    /// <summary>
    /// Model to response for the iPay88 AJax Model
    /// </summary>
    public class iPay88AjaxResponseModel
    {
        public string Url { get; set; }
        public iPay_RequestModel Model { get; set; }
    }

    /// <summary>
    /// Model to capture request from My Photo Creation and pass to New Order page
    /// </summary>
    public class MyPhotoCreationRequest
    {
        public string Photobook_Code { get; set; }
        // to pass to new order options
        //public string Options { get; set; }
        public int Pages { get; set; }
        public long Product_Id { get; set; }
    }

    public class ReportModelSettings
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string Desc { get; set; }
        public string MaHC { get; set; }

        public ReportModelSettings() { }
    }
}

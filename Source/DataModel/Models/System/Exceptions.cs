using System;
using System.Linq;
using System.Collections.Generic;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using PhotoBookmart.DataLayer.Models.Sites;
using System.Net;

namespace PhotoBookmart.DataLayer.Models.System
{
    [Alias("SystemExceptions")]
    public partial class Exceptions
    {
        [PrimaryKey]
        [AutoIncrement]
        public long Id { get; set; }

        public DateTime ExceptionOn { get; set; }

        public string ServerHost { get; set; }

        public string ExMessage { get; set; }

        public string ExSource { get; set; }

        public string ExStackTrace { get; set; }

        public string ContextBrowserAgent { get; set; }

        public string ContextSessionId { get; set; }

        public int ContextHttpCode { get; set; }

        public string ContextUrl { get; set; }

        public string ContextHeader { get; set; }

        public string ContextForm { get; set; }

        /// <summary>
        /// ==0: GET; =1: POST
        /// </summary>
        public string ContextHttpMethod { get; set; }

        public long UserId { get; set; }

        public string UserName { get; set; }

        public string UserIp { get; set; }

        public Exceptions()
        {
            ContextForm = "";
            ContextHeader = "";
        }

        [Ignore]
        public string EmailTitle
        {
            get
            {
                var x = ExMessage.Split(new string[] { ".", "\r" }, StringSplitOptions.RemoveEmptyEntries);
                if (x.Length > 0)
                    return x.First();
                else
                    return "";
            }
        }

        [Ignore]
        public string EmailBody
        {
            get
            {
                //var st = ExMessage + "\r\n\r\nStackTrace\r\n" + ExStackTrace;
                var st = "";
                st = "<h1 style='color: red;font-family: 'Verdana';font-weight: normal; font-size: 18pt;'>" + EmailTitle + "<hr width='100%' size='1'  color='silver'></h1>";
                st += "<h2> <i>" + ExceptionOn.ToString() + "</i> </h2>";
                st += "<div><b>Message:</b>" + ExMessage + "</div>";
                st += "<div><b>Source:</b>" + ExSource + "</div>";
                st += "<div><b>Browser Agent:</b>" + ContextBrowserAgent + "</div>";
                st += "<div><b>Host:</b>" + ServerHost + "</div>";
                st += "<div><b>HTTP Error Code:</b>" + ContextHttpCode + "</div>";
                st += "<div><b>Request URL:</b>" + ContextUrl + "</div>";
                st += "<div><b>Request Method:</b>" + ContextHttpMethod + "</div>";
                st += "<div><b>Header:</b>" + ContextHeader + "</div>";
                st += "<div><b>User IP:</b>" + UserIp + "</div>";
                st += "<div><b>User ID:</b>" + UserId + "</div>";
                st += "<div><b>Username :</b>" + UserName + "</div>";
                st += "<div><b>Form Data :</b>" + ContextForm + "</div>";
                st += "<br /><br /><h2><i>Stack Trace</i> </h2>";
                st += "<div>" + ExStackTrace + "</div>";

                st = st.Replace("\r\n", "<br />");
                st = "<html><body>" + st + "</body></html>";
                //                return WebUtility.HtmlEncode(st);
                return st;
            }
        }

    }
}

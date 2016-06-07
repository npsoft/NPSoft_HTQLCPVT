using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.Areas.Administration.Models
{
    public class MaillingListSendModel
    {
        public string Body { get; set; }
        public string Title { get; set; }
        /// <summary>
        /// List of users we will send email to
        /// </summary>
        public List<int> TargetEmails { get; set; }
        public int SiteId { get; set; }
        public string SiteName { get; set; }
    }

}
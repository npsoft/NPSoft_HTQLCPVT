using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace PhotoBookmart.Areas.Administration.Models
{
    public class ContactUsListModel
    {
        public int SiteId { get; set; }
        public string term { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.Areas.Administration.Models
{
    public class ReviewFilterModel
    {
        public int MerchantId { get; set; }
        public DateTime BetweenDate { get; set; }
        public DateTime AndDate { get; set; }
        public string Search { get; set; }
        public int ResultType { get; set; }
    }

}
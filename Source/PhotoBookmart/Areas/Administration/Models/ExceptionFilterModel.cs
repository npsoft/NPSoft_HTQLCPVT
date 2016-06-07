using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PhotoBookmart.DataLayer.Models.Sites;

namespace PhotoBookmart.Areas.Administration.Models
{
    public class ExceptionFilterModel
    {
        public DateTime BetweenDate { get; set; }
        public DateTime AndDate { get; set; }
        public string Search { get; set; }
        public string Host { get; set; }
        public string HttpMethod { get; set; }
        public int ResultType { get; set; }
    }

}
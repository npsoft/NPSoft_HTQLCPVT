using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PhotoBookmart.Areas.Administration.Models
{
    public class ListModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class GoogleAnalyticResultModel
    {
        public int View { get; set; }
        public int Click { get; set; }
        public DateTime OnDate { get; set; }
        /// <summary>
        /// OnDate but in string format mm/dd/yyyy h:m:s
        /// </summary>
        public string OnDateRaw { get; set; }
    }

    public class UserUpdatePassword
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string NewPassword2 { get; set; }
    }

    /// <summary>
    /// this model is for cloning data from current language to other language
    /// </summary>
    public class CloneToOtherLangModel
    {
        public int Id { get; set; }
        public string LangCode { get; set; }
        public string NewName { get; set; }
        public bool CloneFile { get; set; }
    }

}
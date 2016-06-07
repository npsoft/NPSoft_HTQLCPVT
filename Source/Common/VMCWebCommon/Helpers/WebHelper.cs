using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.Contracts;
using System.Web.Mvc;

namespace PhotoBookmart.Common.Helpers
{

    public static class WebHelper
    {
        public static SelectList ToSelectList<TEnum>(this TEnum enumObj)
        {
            var values = from TEnum e in Enum.GetValues(typeof(TEnum))
                         select new { Id = Convert.ToInt32(e), Name = e.ToString() };

            return new SelectList(values, "Id", "Name", enumObj);
        }

        public static List<String> ToListString<TEnum>(this TEnum enumObj)
        {
            var values = from TEnum e in Enum.GetValues(typeof(TEnum))
                         select new { Id = Convert.ToInt32(e), Name = e.ToString() };

            List<string> ret = new List<string>();
            foreach (var x in values)
            {
                ret.Add(x.Name);
            }
            return ret;
        }


        public static string TrimStringForTitle(string text)
        {
            return text.Replace("\r", "").Replace("\n", "").Trim().Replace("  ", " ");
        }
    }
}

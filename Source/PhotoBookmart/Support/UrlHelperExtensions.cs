using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PhotoBookmart.ServiceInterface;
using System.Text.RegularExpressions;

namespace PhotoBookmart.Support
{
    public static class UrlHelperExtensions
    {
        public static string ContentArea(this UrlHelper url, string path)
        {
            var area = url.RequestContext.RouteData.DataTokens["area"];

            if (area != null)
            {
                if (!string.IsNullOrEmpty(area.ToString()))
                    area = "Areas/" + area;

                // Simple checks for '~/' and '/' at the
                // beginning of the path.
                if (path.StartsWith("~/"))
                    path = path.Remove(0, 2);

                if (path.StartsWith("/"))
                    path = path.Remove(0, 1);

                path = path.Replace("../", string.Empty);

                return VirtualPathUtility.ToAbsolute("~/" + area + "/" + path);
            }

            return string.Empty;
        }

        public static string ContentTheme(this UrlHelper url, string path)
        {
            var path_splited = path.Split(new string[] { "?" }, StringSplitOptions.RemoveEmptyEntries);
            if (path_splited.Count() > 0)
            {
                path = path_splited[0];
            }
            else
            {
                path = "";
            }

            string theme = "";
            var httpContext = System.Web.HttpContext.Current;
            if (System.Web.HttpContext.Current.Session["theme"] != null)
            {
                theme = httpContext.Session["theme"].ToString();
            }
            else
            {
                // search for theme
                var InternalService = AppHost.ResolveService<LocalAPI>(System.Web.HttpContext.Current);
                theme = InternalService.FrontEnd_GetTheme();
                InternalService.Dispose();
                httpContext.Session.Add("theme", theme);
            }

            string test_url = "/Themes/" + theme + "/" + path;
            var k = httpContext.Server.MapPath("~" + test_url);
            if (System.IO.File.Exists(k))
            {
                return test_url;
            }
            else
            {
                if (path.StartsWith("~/"))
                    path = path.Remove(0, 2);

                if (path.StartsWith("/"))
                    path = path.Remove(0, 1);

                path = path.Replace("../", string.Empty);

                return VirtualPathUtility.ToAbsolute("~/" + path);
            }
        }

        
    }
}
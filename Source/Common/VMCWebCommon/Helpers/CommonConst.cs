using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;

namespace PhotoBookmart.Common.Helpers
{
    public static class CommonConst
    {
        public static string Default_WebServer_URL = "http://54.252.209.181/";
        public static string DefaultSessionPrefix = "RealBonus_";
        public static string LanguageSessionKey { get { return "RealBonus_LANGKEY"; } }
        public static string LanguageSessionObject { get { return "RealBonus_LANGOBJECT"; } }

        public static string SQLExcuteUser { get; set; }

        // For MaxMind GeoDB Location
        public static string PATH_GeoDBFullPath = "";
        public static string PATH_AppData = "";
        static CommonConst()
        {
            PATH_AppData = HostingEnvironment.ApplicationPhysicalPath;
            if (!PATH_AppData.EndsWith("\\"))
                PATH_AppData += "\\";

            PATH_GeoDBFullPath = PATH_AppData+"App_Data\\GeoLiteCity.dat";
        }


        public static string KEY_CACHE_Current_LanguageID
        {
            get
            {
                return "TTG_SESSION_CURRENT_LANGID";
            }
        }

        public static string KEY_CACHE_Current_SiteID
        {
            get
            {
                return "TTG_SESSION_CURRENT_SITEID";
            }
        }

        /// <summary>
        /// Key to store list of all distributors of session users 
        /// </summary>
        public static string KEY_SESSION_USER_DISTRIBUTOR
        {
            get
            {
                return "USER_DISTRIBUTOR";
            }
        }

        /// <summary>
        /// Key to store list of all websites belong to this session user
        /// </summary>
        public static string KEY_SESSION_USER_WEBSITE
        {
            get
            {
                return "USER_WEBSITE";
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PhotoBookmart.Common;
using MaxMind.GeoIP;
using System.Web;

namespace PhotoBookmart.Common.Helpers.Geolocation
{
    /// <summary>
    /// Copyright 2013, Trung Dang (trungdt@absoft.vn)
    /// </summary>
    public static class MaxMind
    {
        static LookupService GeoLookup;

        public static void Init()
        {
            GeoLookup = new LookupService(PhotoBookmart.Common.Helpers.CommonConst.PATH_GeoDBFullPath, LookupService.GEOIP_MEMORY_CACHE);
        }

        public static Location GetLocatioByIP(string IP)
        {
            return GeoLookup.GetLocation(IP);
        }
    }
}

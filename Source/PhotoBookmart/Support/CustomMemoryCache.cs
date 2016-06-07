using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Runtime.Caching;
using System.Web.Caching;

namespace PhotoBookmart.Support
{
    public class CustomMemoryCache : MemoryCache
    {
        public CustomMemoryCache(string name)
            : base(name)
        {

        }
        public override bool Add(string key, object value, DateTimeOffset absoluteExpiration, string regionName = null)
        {
            // Do your custom caching here, in my example I'll use standard Http Caching
            HttpContext.Current.Cache.Add(key, value, null, absoluteExpiration.DateTime,
                System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);

            return true;
        }

        public override object Get(string key, string regionName = null)
        {
            // Do your custom caching here, in my example I'll use standard Http Caching
            return HttpContext.Current.Cache.Get(key);
        }
    }

    public class CustomOutputCacheProvider : OutputCacheProvider
    {
        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            // Do the same custom caching as you did in your 
            // CustomMemoryCache object
            var result = HttpContext.Current.Cache.Get(key);

            if (result != null)
            {
                return result;
            }

            HttpContext.Current.Cache.Add(key, entry, null, utcExpiry,
                System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);

            return entry;
        }

        public override object Get(string key)
        {
            return HttpContext.Current.Cache.Get(key);
        }

        public override void Remove(string key)
        {
            HttpContext.Current.Cache.Remove(key);
        }

        public override void Set(string key, object entry, DateTime utcExpiry)
        {
            HttpContext.Current.Cache.Add(key, entry, null, utcExpiry,
                System.Web.Caching.Cache.NoSlidingExpiration, System.Web.Caching.CacheItemPriority.Normal, null);
        }
    }
}
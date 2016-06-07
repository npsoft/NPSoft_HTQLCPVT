using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.OrmLite;
using ServiceStack.DataAnnotations;
using PhotoBookmart.DataLayer.Models.System;

namespace PhotoBookmart.DataLayer.Models.Sites
{
    [Alias("Site_Blog_Category")]
    [Schema("CMS")]
    public partial class Site_Blog_Category : ModelBase
    {
        //[ForeignKey(typeof(Website), OnDelete = "CASCADE", OnUpdate = "CASCADE")]
        //public int SiteId { get; set; }

        public string LanguageCode { get; set; }

        public bool IsActive { get; set; }

        public string Name { get; set; }

        public string SeoName { get; set; }

        public Site_Blog_Category()
        {
            Id = 0;
            CreatedBy = 0;
            CreatedOn = DateTime.Now;
            IsActive = true;
        }

        [Ignore]
        public string SiteName { get; set; }
    }
}